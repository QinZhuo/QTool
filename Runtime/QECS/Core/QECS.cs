using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
namespace QTool.ECS {
	#region Entity
	public struct Entity {
		public int Id;
		public override string ToString() {
			return $"{Id}";
		}
	}
	public sealed class EntityType {
		public World World { get;private set; }
		public override string ToString() => $"{name} {entityMap.Count}";
		public string name { get; private set; }
		public const int INIT_SIZE = 1024;
		private readonly Dictionary<Entity, int> entityMap = new();
		private readonly Dictionary<Type, IComponentArray> componentArrays = new();
		public bool ContainsType(Type type) => componentArrays.ContainsKey(type);
		public IEnumerable<Entity> Entities => entityMap.Keys;
		public int Count { get; private set; } = 0;
		public int Capacity { get; private set; } = INIT_SIZE;
		public EntityType(Type[] types,World world) {
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
				Capacity *= 2;
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
		public EntityType AddComponent<T>(in Entity entity, in T component) where T : IQComponent {
			var trueType = component.GetType();
			var newType =World.GetEntityType(componentArrays.Keys.Append(trueType).ToArray());
			MoveTo(entity, newType);
			if (trueType == typeof(T)) {
				newType.GetComponent<T>(entity) = component;
			}
			else {
				newType.componentArrays[trueType].Set(newType.entityMap[entity], component);
			}
			return newType; ;
		}
		private void MoveTo(in Entity entity, EntityType targetType) {
			if (!entityMap.TryGetValue(entity, out var index))
				throw new ArgumentException("Invalid entity");
			var targetIndex = targetType.Count;
			targetType.AddEntity(entity);
			foreach (var item in componentArrays) {
				item.Value.CopyTo(index, targetType.componentArrays[item.Key], targetIndex);
			}
			RemoveEntity(entity);
		}
		public ref T GetComponent<T>(in Entity entity) where T : IQComponent {
			if (!entityMap.TryGetValue(entity, out var index))
				throw new ArgumentException("Invalid entity");
			return ref (componentArrays[typeof(T)] as ComponentArray<T>).Get(index);
		}
	}

	public sealed class World {
		private int EntityCount;
		private readonly Queue<Entity> entityPool = new();
		private readonly Dictionary<Entity, EntityType> entityMap = new();
		private readonly Dictionary<Types, EntityType> entityTypes = new();
		private readonly Dictionary<Types, List<EntityType>> entityTypesCache = new();
		private readonly List<QSystem> systems = new();
		public void RegisterSystem(QSystem system) {
			system.World = this;
			system.Init();
			systems.Add(system);
			Debug.Log($"{nameof(RegisterSystem)} {system}");

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
				type = new EntityType(types,this);
				entityTypes.Add(key, type);
				Debug.Log($"new {type}");
				foreach (var (typeKey, list) in entityTypesCache) {
					if (key.Contains(typeKey)) {
						list.Add(type);
						Debug.Log($"typeList {typeKey} {list.ToOneString()}");
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
				Debug.Log($"entityTypesCache {types} {typeList.ToOneString()}");
			}
			return typeList;
		}

		public void AddComponent<T>(in Entity entity, in T component) where T : IQComponent {
			if (!entityMap.TryGetValue(entity, out var type))
				throw new ArgumentException("Invalid entity");
			entityMap[entity] = type.AddComponent(entity, component);
		}
		public ref T GetComponent<T>(in Entity entity) where T : struct, IQComponent {
			if (!entityMap.TryGetValue(entity, out var type))
				throw new ArgumentException("Invalid entity");
			return ref type.GetComponent<T>(entity);
		}
		public bool HasComponent<T>(in Entity entity) where T : struct {
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
	#endregion
	#region Component
	public interface IQComponent { }
	internal interface IComponentArray {
		public void Resize(int size);
		public void Remove(int index, int end);
		public void Set(int index, object obj);
		public void CopyTo(int index, IComponentArray targetArrary, int targetIndex);
	}
	internal sealed class ComponentArray<T> : IComponentArray where T : IQComponent {
		public T[] components = new T[EntityType.INIT_SIZE];
		public void Resize(int size) {
			Array.Resize(ref components, size);
		}
		public void Remove(int index, int end) {
			components[index] = components[end];
		}
		public void Set(int index, object obj) {
			components[index] = (T)obj;
		}
		public void Set(int index, in T comp) {
			components[index] = comp;
		}
		public ref T Get(int index) {
			return ref components[index];
		}

		public void CopyTo(int index, IComponentArray targetArrary, int targetIndex) {
			if (targetArrary is ComponentArray<T> target) {
				target.Set(targetIndex, in Get(index));
			}
			else {
				throw new ArgumentException("Invalid targetArrary");
			}
		}
	}
	#endregion
	#region System
	public abstract class QSystem {
		public World World { get; internal set; }
		public abstract void Init();
		public abstract void Update();
	}
	public abstract class QSystem<T1> : QSystem where T1 : IQComponent {
		public Types typeKey = new Type[] { typeof(T1) };
		public sealed override void Update() {
			var types = World.GetEntityTypes(typeKey);
			foreach (var type in types) {
				foreach (var entity in type.Entities) {
					Query(ref type.GetComponent<T1>(entity));
				}
			}
		}
		public abstract void Query(ref T1 comp1);
	}
	public abstract class QSystem<T1, T2> : QSystem where T1 : IQComponent where T2 : IQComponent {
		public Types typeKey = new Type[] { typeof(T1), typeof(T2) };
		public sealed override void Update() {
			var types = World.GetEntityTypes(typeKey);
			foreach (var type in types) {
				foreach (var entity in type.Entities) {
					Query(ref type.GetComponent<T1>(entity), ref type.GetComponent<T2>(entity));
				}
			}
		}
		public abstract void Query(ref T1 comp1, ref T2 comp2);
	}
	#endregion
}

