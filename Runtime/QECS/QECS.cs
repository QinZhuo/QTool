using QTool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.LightTransport;
using static UnityEditor.PlayerSettings;
namespace QTool.ECS {


	// 实体结构（包含版本号用于验证有效性）
	public struct Entity : IEquatable<Entity> {
		public int Id;
		public int Version;

		public bool Equals(Entity other) => Id == other.Id && Version == other.Version;
		public override int GetHashCode() => HashCode.Combine(Id, Version);
	}

	

	// 组件存储接口
	public interface IComponentStorage {
		void Remove(Entity entity);
		bool Has(Entity entity);
	}

	// 组件存储实现（密集数组+快速索引）
	public sealed class ComponentStorage<T> : IComponentStorage where T : struct {
		public int Index { get; internal set; }
		private T[] _components = new T[World.COMP_CHUNK_SIZE];
		private bool[] _existence = new bool[World.COMP_CHUNK_SIZE];
		public ref T Add(Entity entity) {
			var index = (int)entity.Id;
			if (index >= _components.Length) {
				Array.Resize(ref _components, index + 1 + World.COMP_CHUNK_SIZE);
				Array.Resize(ref _existence, index + 1 + World.COMP_CHUNK_SIZE);
			}
			_existence[index] = true;
			return ref _components[index];
		}

		public void Remove(Entity entity) {
			var index = (int)entity.Id;
			if (index < _components.Length) {
				_existence[index] = false;
			}
		}
		public bool Has(Entity entity) {
			return entity.Id < _components.Length && _existence.Get(entity.Id);
		}

		public ref T Get(Entity entity) {
			if (!Has(entity)) {
				throw new KeyNotFoundException("Component not found");
			}
			return ref _components[entity.Id];
		}
	}
	// 世界（核心容器）
	public sealed class World {
		public const int MAX_COMP_TYPE_COUNT = 128;
		public const int COMP_CHUNK_SIZE = 1024;
		private int EntityIndex = 1;
		private int ComponentIndex;
		public readonly Dictionary<Entity, BitArray> entitys = new();
		public readonly Dictionary<Type, IComponentStorage> components = new();
		private readonly List<System> systems = new();
		public void RegisterSystem<T>() where T : System, new() {
			systems.Add(new T() { World = this });
		}
		public void Update() {
			foreach (var system in systems) {
				system.Update();
			}
		}
		public Entity CreateEntity() {
			var entity = new Entity { Id = EntityIndex++, Version = 0 };
			entitys[entity] = new BitArray(MAX_COMP_TYPE_COUNT);
			return entity;
		}

		public void DestroyEntity(Entity entity) {
			foreach (var storage in components.Values)
				storage.Remove(entity);
			entitys.Remove(entity);
		}

		public void AddComponent<T>(Entity entity, in T component) where T : struct {
			var storage = GetStorage<T>();
			entitys[entity].Set(storage.Index, true);
			ref var comp = ref storage.Add(entity);
			comp = component;
		}
		public void RemoveComponent<T>(Entity entity) where T : struct {
			if (GetStorage<T>() is ComponentStorage<T> storage) {
				storage.Remove(entity);
				entitys[entity].Set(storage.Index, false);
			}
		}

		public ref T GetComponent<T>(Entity entity) where T : struct =>
			 ref GetStorage<T>().Get(entity);

		public bool HasComponent<T>(Entity entity) where T : struct =>
			GetStorage<T>().Has(entity);

		public ComponentStorage<T> GetStorage<T>() where T : struct {
			Type type = typeof(T);
			if (!components.TryGetValue(type, out var storage)) {
				storage = new ComponentStorage<T> {
					Index = ComponentIndex++
				};
				components[type] = storage;
			}
			return (ComponentStorage<T>)storage;
		}
	}

	// 示例组件
	public struct Position { public float X, Y; }
	public struct Velocity { public float X, Y; }

	public abstract class System {
		public World World { get; internal set; }
		public abstract void Update();
	}
	public abstract class System<T1, T2> : System where T1 : struct where T2 : struct {
		protected ComponentStorage<T1> storage1;
		protected ComponentStorage<T2> storage2;
		public sealed override void Update() {
			storage1= World.GetStorage<T1>();
			storage2= World.GetStorage<T2>();
			foreach (var item in World.entitys) {
				if (item.Value.Get(storage1.Index) && item.Value.Get(storage2.Index)) {
					Query(item.Key,ref storage1.Get(item.Key),ref storage2.Get(item.Key));
				}
			}
		}
		public abstract void Query(Entity entity, ref T1 comp1, ref T2 comp2);
	}
	// 示例系统
	public class MovementSystem:System<Position,Velocity> {
		public override void Query(Entity entity, ref Position comp1, ref Velocity comp2) {
			comp1.X += comp2.Y * Time.deltaTime;
		}
	}
	// 使用示例
	public class QECS:MonoBehaviour {
		World world = new World();
		MovementSystem system = new();
		Entity entity;
		private void Awake() {
			entity = world.CreateEntity();
			world.RegisterSystem<MovementSystem>();
			world.AddComponent(entity, new Position());
			world.AddComponent(entity,new Velocity { X = 1, Y = 1 });
		}
		private void Update() {
			world.Update();
			var pos = world.GetComponent<Position>(entity);
			Debug.LogError($"Position: ({pos.X}, {pos.Y})");
		}
	}
}
