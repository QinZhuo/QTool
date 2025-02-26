using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System;
using UnityEngine;

namespace QTool.ECS {
	[Serializable]
	public struct Entity {
		public int Id;
		public override string ToString() {
			return $"{Id}";
		}
		public bool IsNull() { return Id == 0; }
	}
	public sealed class EntityType {
		public World World { get; private set; }
		public override string ToString() => $"{name} {entityMap.Count}";
		[QName,QReadonly]
		public string name { get; private set; }
		public const int INIT_SIZE = 4;
		[QName]
		private readonly Dictionary<Entity, int> entityMap = new();
		private readonly Dictionary<Type, IComponentArray> componentArrays = new();
		public bool ContainsType(Type type) => componentArrays.ContainsKey(type);
		public IEnumerable<Entity> GetEntities(List<Entity> entities) {
			entities.Clear();
			entities.AddRange(entityMap.Keys);
			return entities;
		}
		public IEnumerable<Entity> GetEntities() {
			return entityMap.Keys;
		}
		[QName,QReadonly]
		public int Count { get; private set; } = 0;
		[QName,QReadonly]
		public int Capacity { get; private set; } = INIT_SIZE;
		public EntityType(Type[] types, World world) {
			World = world;
			name = nameof(EntityType) + " ( ";
			foreach (var type in types) {
				name += type.Name + " ";
				componentArrays.Add(type, Activator.CreateInstance(typeof(ComponentArray<>).MakeGenericType(type)) as IComponentArray);
			}
			name += ")";
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddEntity(in Entity entity) {
			if (Count >= Capacity) {
				Capacity *= INIT_SIZE;
				foreach (var item in componentArrays) {
					item.Value.Resize(Capacity);
				}
			}
			foreach (var item in componentArrays) {
				item.Value.Resize(Capacity);
			}
			entityMap.Add(entity, Count);
			Count++;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RemoveEntity(in Entity entity) {
			if (!entityMap.TryGetValue(entity, out var index))
				throw new ArgumentException("Invalid entity");
			foreach (var component in componentArrays) {
				component.Value.Remove(index, Count);
			}
			entityMap.Remove(entity);
			Count--;
		}
		public void SetComponent<T>(in Entity entity, in T component) where T : IComponent {
			var trueType = component.GetType();
			if (trueType == typeof(T)) {
				GetComponent<T>(entity) = component;
			}
			else {
				componentArrays[trueType].SetObject(entityMap[entity], component);
			}
		}
		public EntityType AddComponent<T>(in Entity entity, in T component) where T : IComponent {
			var trueType = component.GetType();
			var newType = World.GetEntityType(componentArrays.Keys.Append(trueType).ToArray());
			MoveTo(entity, newType);
			if (trueType == typeof(T)) {
				newType.GetComponent<T>(entity) = component;
			}
			else {
				newType.componentArrays[trueType].SetObject(newType.entityMap[entity], component);
			}
			return newType;
		}
		public EntityType RemoveComponent<T>(in Entity entity, T comp) where T : class, IComponent {
			var compType=comp.GetType();
			var newType = World.GetEntityType(componentArrays.Keys.Where(type => type != compType).ToArray());
			MoveTo(entity, newType);
			return newType;
		}
		public EntityType RemoveComponent<T>(in Entity entity) where T : IComponent {
			var compType = typeof(T);
			var newType = World.GetEntityType(componentArrays.Keys.Where(type=>type!= compType).ToArray());
			MoveTo(entity, newType);
			return newType;
		}
		private void MoveTo(in Entity entity, EntityType targetType) {
			if (!entityMap.TryGetValue(entity, out var index))
				throw new ArgumentException("Invalid entity");
			var targetIndex = targetType.Count;
			targetType.AddEntity(entity);
			foreach (var item in componentArrays) {
				if (targetType.componentArrays.ContainsKey(item.Key)) {
					item.Value.CopyTo(index, targetType.componentArrays[item.Key], targetIndex);
				}
			}
			RemoveEntity(entity);
		}
		public ref T GetComponent<T>(in Entity entity) where T : IComponent {
			if (!entityMap.TryGetValue(entity, out var index))
				throw new ArgumentException("Invalid entity");
			return ref (componentArrays[typeof(T)] as ComponentArray<T>).Get(index);
		}
		private Dictionary<Type, Type[]> TupleCache = new();
		public T GetComponents<T>(Entity entity) where T : struct, ITuple {
			if (!entityMap.TryGetValue(entity, out var index))
				throw new ArgumentException("Invalid entity");
			var type = typeof(T);
			if (!TupleCache.TryGetValue(type, out var types)) {
				types = type.GetGenericArguments();
				TupleCache.Add(type, types);
			}
			return (T)Activator.CreateInstance(type, types.Select(type => type.Equals(typeof(T))
		? entity : (componentArrays[type].GetObject(index))).ToArray());
		}
		public void SetComponents<T>(in Entity entity,T comps) where T : struct, ITuple {
			if (!entityMap.TryGetValue(entity, out var index))
				throw new ArgumentException($"Invalid entity {entity}");
			var type = typeof(T);
			if (!TupleCache.TryGetValue(type, out var types)) {
				types = type.GetGenericArguments();
				TupleCache.Add(type, types);
			}
			for (int i = 0; i < comps.Length; i++) {
				if (types[i] == typeof(Entity)) continue;
				componentArrays[types[i]].SetObject(index, comps[i]);
			}
		}
	}

	public sealed class World {
		private int EntityCount = 1;
		private readonly Queue<Entity> entityPool = new();
		private readonly Dictionary<Entity, EntityType> entityMap = new();
		private readonly Dictionary<Types, EntityType> entityTypes = new();
		private readonly Dictionary<Types, List<EntityType>> entityTypesCache = new();
		private readonly List<ISystem> systems = new();
		public IEnumerable<ISystem> Systems => systems;
		public IEnumerable<EntityType> EntityTypes => entityTypes.Values;
		public void RegisterSystem(ISystem system) {
			system.World = this;
			system.Start();
			systems.Add(system);

		}
		public void Update() {
			foreach (var system in systems) {
				system.Update();
			}
		}
		public Entity CreateEntity() {
			var entity = entityPool.Count > 0 ? entityPool.Dequeue() : new Entity { Id = EntityCount++ };
			var emptyType = GetEntityType();
			emptyType.AddEntity(entity);
			entityMap.Add(entity, emptyType);
			return entity;
		}
		public void DestroyEntity(in Entity entity) {
			if (!entityMap.TryGetValue(entity, out var type))
				throw new ArgumentException("Invalid entity");
			entityPool.Enqueue(entity);
			type.RemoveEntity(entity);
		}
		internal EntityType GetEntityType(params Type[] types) {
			var key = (Types)types;
			if (!entityTypes.TryGetValue(key, out var type)) {
				type = new EntityType(types, this);
				entityTypes.Add(key, type);
				foreach (var (typeKey, list) in entityTypesCache) {
					if (key.Contains(typeKey)) {
						list.Add(type);
					}
				}
			}
			return type;
		}
		public List<EntityType> GetEntityTypes(Types types) {
			if (!entityTypesCache.TryGetValue(types, out var typeList)) {
				typeList = new();
				foreach (var (key, entityType) in entityTypes) {
					if (key.Contains(types)) {
						typeList.Add(entityType);
					}
				}
				entityTypesCache.Add(types, typeList);
			}
			return typeList;
		}
		public List<EntityType> GetEntityTypes<T>()where T:struct,ITuple {
			return GetEntityTypes(typeof(T).GetGenericArguments());
		}
		public EntityType GetEntityType(in Entity entity) {
			if (!entityMap.TryGetValue(entity, out var type))
				throw new ArgumentException($"Invalid entity {entity}");
			return type;
		}
		public void AddComponent<T>(in Entity entity, in T component) where T : IComponent {
			var type = GetEntityType(entity);
			entityMap[entity] = type.AddComponent(entity, component);
		}
		public void RemoveComponent<T>(in Entity entity, T compType) where T : class, IComponent {
			var type = GetEntityType(entity);
			entityMap[entity] = type.RemoveComponent(entity, compType);
		}
		public void RemoveComponent<T>(in Entity entity) where T : IComponent {
			var type = GetEntityType(entity);
			entityMap[entity] = type.RemoveComponent<T>(entity);
		}
		public void SetComponent<T>(in Entity entity, in T component) where T : IComponent {
			var type = GetEntityType(entity);
			type.SetComponent(entity, component);
		}
		public ref T GetComponent<T>(in Entity entity) where T : IComponent {
			var type = GetEntityType(entity);
			return ref type.GetComponent<T>(entity);
		}
		public bool HasComponent<T>(in Entity entity) where T : IComponent {
			if (!entityMap.TryGetValue(entity, out var type))
				return false;
			return type.ContainsType(typeof(T));
		}
	}

	public struct Types {
		private static int typeCount = 0;
		private static Dictionary<Type, int> HashCache = new();
		public static int GetHash(Type type) {
			if (!HashCache.TryGetValue(type, out var hash)) {
				hash = 1 << typeCount++;
				HashCache.Add(type, hash);
			}
			return hash;
		}
		public int hash;
		public bool Contains(Types other) {
			return Equals(other.hash & hash, other.hash);
		}
		public static implicit operator Types(Type[] types) {
			var hash = new Types { hash = types.Select(type => GetHash(type)).Sum() };
			return hash;
		}
		public override string ToString() {
			return $"{hash}";
		}
	}
}
