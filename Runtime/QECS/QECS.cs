using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace QTool.ECS {
	// 实体结构（包含版本号用于验证有效性）
	public struct Entity {
		public int Id;
		public override string ToString() {
			return $"{Id}";
		}
	}
	// 组件存储接口
	public interface IComponentStorage {
		void Remove(Entity entity);
		bool Has(Entity entity);
	}

	// Archetype核心数据结构
	public class Archetype {
		public readonly Type[] ComponentTypes;
		private readonly Dictionary<Type, Array> componentArrays;
		public readonly List<Entity> entities;

		public int Count => entities.Count;

		public Archetype(Type[] types) {
			ComponentTypes = types.OrderBy(t => t.GUID).ToArray();
			componentArrays = new Dictionary<Type, Array>();
			entities = new List<Entity>();
			Debug.LogError("type " + ComponentTypes.ToOneString());
			foreach (var type in ComponentTypes) {
				componentArrays[type] = Array.CreateInstance(type, 64);
			}
		}

		public void AddEntity(Entity entity, IList<IComponent> components=null) {

			// 扩容检查
			//if (componentArrays.Count == 0 || entities.Count >= componentArrays.Values.First().Length) {
			//	foreach (var type in ComponentTypes) {
			//		var oldArray = componentArrays[type];
			//		var newArray = Array.CreateInstance(type, oldArray.Length * 2);
			//		Array.Copy(oldArray, newArray, oldArray.Length);
			//		componentArrays[type] = newArray;
			//	}
			//}

			entities.Add(entity);
			var index = entities.Count - 1;
			if (components != null) {
				foreach (var comp in components) {
					componentArrays[comp.GetType()].SetValue(comp, index);
				}
			}
		}

		public Span<T> GetComponents<T>() where T : struct {
			if (componentArrays.TryGetValue(typeof(T), out var arr)) {
				return new Span<T>((T[])arr, 0, entities.Count);
			}
			return Span<T>.Empty;
		}
		public void RemoveEntity(Entity entity) {
			RemoveEntity(entities.IndexOf(entity));
		}
		public void RemoveEntity(int index) {
			// 交换删除法保持内存连续
			var lastIndex = entities.Count - 1;

			foreach (var array in componentArrays.Values) {
				var lastVal = array.GetValue(lastIndex);
				array.SetValue(lastVal, index);
			}

			entities[index] = entities[lastIndex];
			entities.RemoveAt(lastIndex);
		}
	}


	//// 组件存储实现（密集数组+快速索引）
	//public sealed class ComponentStorage<T> : IComponentStorage where T : struct {
	//	public int Index { get; internal set; }
	//	private T[] _components = new T[World.COMP_CHUNK_SIZE];
	//	private bool[] _existence = new bool[World.COMP_CHUNK_SIZE];
	//	public ref T Add(Entity entity) {
	//		var index = (int)entity.Id;
	//		if (index >= _components.Length) {
	//			Array.Resize(ref _components, index + 1 + World.COMP_CHUNK_SIZE);
	//			Array.Resize(ref _existence, index + 1 + World.COMP_CHUNK_SIZE);
	//		}
	//		_existence[index] = true;
	//		return ref _components[index];
	//	}

	//	public void Remove(Entity entity) {
	//		var index = (int)entity.Id;
	//		if (index < _components.Length) {
	//			_existence[index] = false;
	//		}
	//	}
	//	public bool Has(Entity entity) {
	//		return entity.Id < _components.Length && _existence.Get(entity.Id);
	//	}

	//	public ref T Get(Entity entity) {
	//		if (!Has(entity)) {
	//			throw new KeyNotFoundException("Component not found");
	//		}
	//		return ref _components[entity.Id];
	//	}
	//}
	// 世界（核心容器）
	public sealed class World {
		public const int MAX_COMP_TYPE_COUNT = 128;
		public const int COMP_CHUNK_SIZE = 1024;
		private int EntityIndex;
		private int ComponentIndex;
		private readonly Dictionary<Entity, Archetype> entityArchetypes = new();
		private readonly Dictionary<Type[], Archetype> archetypeCache = new(new TypeArrayComparer());
		//public readonly Dictionary<Entity, BitArray> entitys = new();
		//public readonly Dictionary<Type, IComponentStorage> components = new();
		private readonly List<System> systems = new();
		public void RegisterSystem<T>() where T : System, new() {
			systems.Add(new T() { World = this });
		}
		public void Update() {
			foreach (var system in systems) {
				system.Update();
			}
		}
		public Archetype GetOrCreateArchetype(IEnumerable<Type> types) {
			var orderedTypes = types.OrderBy(t => t.GUID).ToArray();
			if (!archetypeCache.TryGetValue(orderedTypes, out var archetype)) {
				archetype = new Archetype(orderedTypes);
				archetypeCache[orderedTypes] = archetype;
			}
			return archetype;
		}
		public Entity CreateEntity() {
			var entity = new Entity { Id = EntityIndex++ };
			var emptyArchetype = GetOrCreateArchetype(Array.Empty<Type>());
			emptyArchetype.AddEntity(entity);
			entityArchetypes[entity] = emptyArchetype;
			return entity;
		}
		public Entity CreateEntity(params IComponent[] comps) {
			var entity = new Entity { Id = EntityIndex++ };
			var emptyArchetype = GetOrCreateArchetype(comps.Select(c => c.GetType()));
			emptyArchetype.AddEntity(entity, comps);
			entityArchetypes[entity] = emptyArchetype;
			return entity;
		}

		public void DestroyEntity(Entity entity) {
			entityArchetypes[entity].RemoveEntity(entity);
		}
		//private List<IComponent> components = new();
		//public void AddComponent<T>(Entity entity, in T component) where T : struct, IComponent {
		//	if (!entityArchetypes.TryGetValue(entity, out var currentArchetype))
		//		return;
		//	Debug.LogError(entity + "add" + component);
		//	var newTypes = currentArchetype.ComponentTypes.Append(typeof(T)).ToArray();
		//	var newArchetype = GetOrCreateArchetype(newTypes);
		//	components.Clear();
		//	foreach (var type in currentArchetype.ComponentTypes) {
		//		components.Add(GetComponent())
		//	}
		//	components.AddRange(currentArchetype.)
		//	// 迁移实体到新的Archetype
		// currentArchetype.ComponentTypes
		// .ToDictionary(t => t, t => GetComponent(entity,t));

		//	components.Add(component);
		//	currentArchetype.RemoveEntity(GetEntityIndex(entity, currentArchetype));
		//	newArchetype.AddEntity(entity, components);
		//	entityArchetypes[entity] = newArchetype;
		//}
		//public void RemoveComponent<T>(Entity entity) where T : struct {
		//	if (!entityArchetypes.TryGetValue(entity, out var currentArchetype))
		//		return;

		//	var newTypes = currentArchetype.ComponentTypes.ToList<T>().Remove(typeof(T)).ToArray();
		//	var newArchetype = GetOrCreateArchetype(newTypes);

		//	// 迁移实体到新的Archetype
		//	var components = currentArchetype.ComponentTypes
		// .ToDictionary(t => t, t => (IComponent)GetComponent<T>(entity));

		//	components[typeof(T)] = component;
		//	currentArchetype.RemoveEntity(GetEntityIndex(entity, currentArchetype));
		//	newArchetype.AddEntity(entity, components);
		//	entityArchetypes[entity] = newArchetype;
		//}
		private int GetEntityIndex(Entity entity, Archetype archetype) {
			// 实际实现需要维护索引，这里简化为线性搜索
			return archetype.entities.IndexOf(entity);
		}

		public ref T GetComponent<T>(Entity entity) where T : struct, IComponent {
			var archetype = entityArchetypes[entity];
			var index = GetEntityIndex(entity, archetype);
			Debug.LogError("index " + typeof(T) + "  _ " + index);
			return ref archetype.GetComponents<T>()[index];
		}

		public bool HasComponent<T>(Entity entity) where T : struct {
			if (!entityArchetypes.TryGetValue(entity, out var currentArchetype))
				return false;
			return currentArchetype.ComponentTypes.Contains(typeof(T));
		}

		//public ComponentStorage<T> GetStorage<T>() where T : struct {
		//	Type type = typeof(T);
		//	if (!components.TryGetValue(type, out var storage)) {
		//		storage = new ComponentStorage<T> {
		//			Index = ComponentIndex++
		//		};
		//		components[type] = storage;
		//	}
		//	return (ComponentStorage<T>)storage;
		//}
		public class TypeArrayComparer : IEqualityComparer<Type[]> {
			public bool Equals(Type[] x, Type[] y) => x.SequenceEqual(y);
			public int GetHashCode(Type[] obj) {
				unchecked {
					return obj.Aggregate(17, (current, t) => current * 23 + t.GetHashCode());
				}
			}
		}
	}
	// 组件接口标记
	public interface IComponent { }
	// 示例组件
	public struct Position : IComponent { public float X, Y; }
	public struct Velocity : IComponent { public float X, Y; }

	public abstract class System {
		public World World { get; internal set; }
		public abstract void Update();
	}
	public abstract class System<T1, T2> : System where T1 : struct,IComponent where T2 : struct, IComponent {
		protected Archetype storage1;
		protected Archetype storage2;
		public sealed override void Update() {
			storage1 = World.GetOrCreateArchetype(new Type[] {typeof(T1), typeof(T2) });
			//storage2 = World.GetOrCreateArchetype(new Type[] {  });
			foreach (var entity in storage1.entities) {
				Query(ref World.GetComponent<T1>(entity),ref World.GetComponent<T2>(entity));
			}
			//foreach (var item in World.entitys) {
			//	if (item.Value.Get(storage1.Index) && item.Value.Get(storage2.Index)) {
			//		Query(item.Key, ref storage1.Get(item.Key), ref storage2.Get(item.Key));
			//	}
			//}
		}
		public abstract void Query( ref T1 comp1, ref T2 comp2);
	}
	// 示例系统
	public class MovementSystem : System<Position, Velocity> {
		public override void Query(ref Position comp1, ref Velocity comp2) {
			comp1.X += comp2.Y * Time.deltaTime;
		}
	}
	// 使用示例
	public class QECS : MonoBehaviour {
		World world = new World();
		MovementSystem system = new();
		Entity entity;
		private void Awake() {
			entity = world.CreateEntity(new Position(), new Velocity { X = 1, Y = 1 });
			world.RegisterSystem<MovementSystem>();
		}
		private void Update() {
			world.Update();
			//var pos = world.GetComponent<Position>(entity);
			//Debug.LogError($"Position: ({pos.X}, {pos.Y})");
		}
	}
}
