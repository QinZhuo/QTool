#if ECS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using QTool;
using QTool.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor.PackageManager;
using UnityEngine;
namespace QTool {
	public class QEntity : MonoBehaviour {
		public TransformUsageFlags usage = TransformUsageFlags.Dynamic;
	}
	public class QEntityBaker : Baker<QEntity> {
		public override void Bake(QEntity authoring) {
			var entity = GetEntity(authoring.usage);
			var typeList = new List<Type>();
			var bufferCache = new QDictionary<Type, object>();
			bufferCache.AutoCreate = type => QReflectionType.Get(GetType()).FunctionsCache[nameof(AddBuffer)][1][0].MethodInfo.MakeGenericMethod(type).Invoke(this, new object[] { entity });
			var data = QEntityData.Get(authoring.name);
			if (data == null)
				return;
			foreach (var comp in data.comps) {
				if (comp == null)
					continue;
				var type = comp.GetType();
				if (typeList.Contains(type))
					continue;
				typeList.Add(type);
				var typeInfo = QSerializeType.Get(type);
				foreach (var memeber in typeInfo.Members) {
					if (memeber.Type == typeof(QEntityPath) && memeber.Get(comp) is QEntityPath prefabEntity) {
						var path = prefabEntity.path.ToString();
						var prefab = Resources.Load<GameObject>(path);
						var key = path.SplitEndString("/");
						if (prefab == null && QEntityData.ContainsKey(key)) {
#if UNITY_EDITOR
							var obj = new GameObject(key, typeof(QEntity));
							prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(obj, Application.dataPath.MergePath(nameof(Resources)).MergePath(path) + ".prefab");
							UnityEngine.Object.DestroyImmediate(obj);
#endif
						}
						if (prefab != null) {
							var tempEntity = prefab.GetComponent<QEntity>();
							prefabEntity.entity = GetEntity(prefab, tempEntity == null ? TransformUsageFlags.Dynamic : tempEntity.usage);
							memeber.Set(comp, prefabEntity);
						}
					}
				}
				this.InvokeFunction(nameof(AddComponent), entity, comp);
			}
			foreach (var item in data.elements) {
				if (item == null)
					continue;
				bufferCache[item.GetType()].InvokeFunction("Add", item);
			}

			//data._entity = entity;
		}
	}
	public class QEntityData : QDataList<QEntityData> {
		public List<IQComponment> comps = new List<IQComponment>();
		public List<IQBufferElement> elements = new List<IQBufferElement>();
		//[QIgnore]
		//private Entity _entity;
		//public Entity Entity {
		//	get {

		//		if (!_entity.IsDefualt()) {
		//			return _entity;
		//		}
		//		foreach (var world in World.All) {
		//			var entityManager = world.EntityManager;
		//			_entity = entityManager.CreateEntity();
		//			entityManager.SetName(_entity, Key);
		//			foreach (var comp in comps) {
		//				if (comp == null)
		//					continue;
		//				var type = comp.GetType();
		//				var typeInfo = QSerializeType.Get(type);
		//				foreach (var memeber in typeInfo.Members) {
		//					if (memeber.Type == typeof(QEntityPath) && memeber.Get(comp) is QEntityPath prefabEntity) {
		//						var path = prefabEntity.path.ToString();
		//						var prefab = Resources.Load<GameObject>(path);
		//						if (prefab == null) {
		//							prefabEntity.entity = Get(path).Entity;
		//							memeber.Set(comp, prefabEntity);
		//						}
		//					}
		//				}
		//				entityManager.InvokeFunction(nameof(entityManager.AddComponentData), _entity, comp);
		//			}
		//		}
		//		return _entity;
		//	}
		//}
	}
	public struct QEntityPath {
		[QIgnore]
		public Entity entity;
		[HideInInspector]
		public FixedString64Bytes path;
	}
	public interface IQComponment : IComponentData {

	}
	public interface IQBufferElement : IBufferElementData {
	}
	public static class QEntityTool
	{
		public static bool IsDefault(this in Entity obj) {
			return obj.Index == 0 && obj.Version == 0;
		}
	}
}
#else
namespace QTool
{
	public class QEntity : MonoBehaviour
	{
		public string data;
	}
}
#endif
