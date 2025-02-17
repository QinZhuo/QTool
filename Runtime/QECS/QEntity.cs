#if ECS
using System;
using System.Collections.Generic;
using QTool.Reflection;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;


#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool {
	public sealed class QEntity : MonoBehaviour {
		public string data;
		public TransformUsageFlags usage = TransformUsageFlags.Dynamic;
//#if UNITY_EDITOR
//		[QName]
//		public void OpenData() {
//			var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(QDataTable<QEntityData>.Get(name).Row.Table.LoadPath);
//			Debug.LogError(QEntityData.Get(name).Row.Table.LoadPath+":"+asset);
//			AssetDatabase.OpenAsset(asset);
//		}
//#endif
	}
#if UNITY_EDITOR
	[CustomEditor(typeof(QEntity), true, isFallback = true)]
	public class QEntityEditor:Editor {
		public override VisualElement CreateInspectorGUI() {
			var root = new VisualElement();
			root.Add<QEntityData>(serializedObject.FindProperty(nameof(QEntity.data)));
			//if (target != null) {
			//	foreach (var func in QSerializeType.Get(target?.GetType()).Functions) {
			//		if (func.MethodInfo.GetCustomAttribute<QNameAttribute>() != null) {
			//			root.AddButton(func.QName, () => func.Invoke(target));
			//		}
			//	}
			//}
			return root;
		}
	}
#endif
	public class QEntityBaker : Baker<QEntity> {
		public override void Bake(QEntity authoring) {
			var entity = GetEntity(authoring.usage);
			var typeList = new List<Type>();
			var bufferCache = new QDictionary<Type, object>();
			bufferCache.AutoCreate = type => QReflectionType.Get(GetType()).FunctionsCache[nameof(AddBuffer)][1][0].MethodInfo.MakeGenericMethod(type).Invoke(this, new object[] { entity });
			var data = QDataTable<QEntityData>.Get(authoring.name);
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
						if (prefab == null && QDataTable<QEntityData>.ContainsKey(key)) {
							//var newEntity = GetEntityWithoutDependency();
#if UNITY_EDITOR
							var obj = new GameObject(key, typeof(QEntity));
							prefab = PrefabUtility.SaveAsPrefabAsset(obj, Application.dataPath.Combine(nameof(Resources)).Combine(path) + ".prefab");
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
	public class QEntityData : IKey<string>{
		public string Key { get; set; }
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
		[QName, HideInInspector]
		public FixedString64Bytes path;
	}
	public class FixedString64Info : QTypeInfo.CustomTypeInfo<FixedString64Bytes, string> {
		public override string ChangeType(FixedString64Bytes obj) {
			return obj.ToString();
		}

		public override FixedString64Bytes ChangeType(string obj) {
			return (FixedString64Bytes)obj;
		}
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
using UnityEngine;
namespace QTool
{
	public class QEntity : MonoBehaviour
	{
	}
}
#endif
