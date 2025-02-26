using QTool.Inspector;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UIElements;
namespace QTool.ECS {
	public class QEntityObject : MonoBehaviour, IComponent {
		[QReadonly]
		public Entity entity;
		[QObject(typeof(QEntityData))]
		public string entityData;
		private List<IComponent> components = new();
		private void Awake() {
			CreateEntity();
		}
		protected virtual void Start() {
			GetComponents(components);
			foreach (var item in components) {
				QWorld.Active.AddComponent(GetComponent<QEntityObject>().entity, item);
			}
		}
		private void OnDestroy() {
			if (entity.IsNull() || !QWorld.Exists)
				return;
			foreach (var item in components) {
				QWorld.Active.RemoveComponent(GetComponent<QEntityObject>().entity, item);
			}
		}
		public Entity CreateEntity() {
			if (!entity.IsNull())
				return entity;
			var data = entityData.ParseQData<QEntityData>();
			entity = QWorld.Active.CreateEntity();
			if (data.components != null) {
				foreach (var comp in data.components) {
					QWorld.Active.AddComponent(entity, comp);
				}
			}
			return entity;
		}
		private void OnValidate() {
			if (!Application.IsPlaying(this) || entity.IsNull() || QWorld.Active == null)
				return;
			var type = QWorld.Active.GetEntityType(entity);
			var data = entityData.ParseQData<QEntityData>();
			if (data.components != null) {
				foreach (var comp in data.components) {
					type.SetComponent(entity, comp);
				}
			}
		}
	}
	[RequireComponent(typeof(QEntityObject))]
	public abstract class QComponent : MonoBehaviour, IComponent {
	}

	public struct QEntityData {
		[QOldName("comps")]
		public List<IComponent> components;
	}
}
//#if UNITY_EDITOR
//using UnityEditor;
//#endif
//namespace QTool {
//	public sealed class QEntity : MonoBehaviour {
//		public string data;
//		public TransformUsageFlags usage = TransformUsageFlags.Dynamic;
////#if UNITY_EDITOR
////		[QName]
////		public void OpenData() {
////			var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(QDataTable<QEntityData>.Get(name).Row.Table.LoadPath);
////			Debug.LogError(QEntityData.Get(name).Row.Table.LoadPath+":"+asset);
////			AssetDatabase.OpenAsset(asset);
////		}
////#endif
//	}
//#if UNITY_EDITOR
//	[CustomEditor(typeof(QEntity), true, isFallback = true)]
//	public class QEntityEditor:Editor {
//		public override VisualElement CreateInspectorGUI() {
//			var root = new VisualElement();
//			root.Add<QEntityData>(serializedObject.FindProperty(nameof(QEntity.data)));
//			//if (target != null) {
//			//	foreach (var func in QSerializeType.Get(target?.GetType()).Functions) {
//			//		if (func.MethodInfo.GetCustomAttribute<QNameAttribute>() != null) {
//			//			root.AddButton(func.QName, () => func.Invoke(target));
//			//		}
//			//	}
//			//}
//			return root;
//		}
//	}
//#endif
//	public class QEntityBaker : Baker<QEntity> {
//		public override void Bake(QEntity authoring) {
//			var entity = GetEntity(authoring.usage);
//			var typeList = new List<Type>();
//			var bufferCache = new QDictionary<Type, object>();
//			bufferCache.AutoCreate = type => QReflectionType.Get(GetType()).FunctionsCache[nameof(AddBuffer)][1][0].MethodInfo.MakeGenericMethod(type).Invoke(this, new object[] { entity });
//			var data = QDataTable<QEntityData>.Get(authoring.name);
//			if (data == null)
//				return;
//			foreach (var comp in data.comps) {
//				if (comp == null)
//					continue;
//				var type = comp.GetType();
//				if (typeList.Contains(type))
//					continue;
//				typeList.Add(type);
//				var typeInfo = QSerializeType.Get(type);
//				foreach (var memeber in typeInfo.Members) {
//					if (memeber.Type == typeof(QEntityPath) && memeber.Get(comp) is QEntityPath prefabEntity) {
//						var path = prefabEntity.path.ToString();
//						var prefab = Resources.Load<GameObject>(path);
//						var key = path.SplitEndString("/");
//						if (prefab == null && QDataTable<QEntityData>.ContainsKey(key)) {
//							//var newEntity = GetEntityWithoutDependency();
//#if UNITY_EDITOR
//							var obj = new GameObject(key, typeof(QEntity));
//							prefab = PrefabUtility.SaveAsPrefabAsset(obj, Application.dataPath.Combine(nameof(Resources)).Combine(path) + ".prefab");
//							UnityEngine.Object.DestroyImmediate(obj);
//#endif
//						}
//						if (prefab != null) {
//							var tempEntity = prefab.GetComponent<QEntity>();
//							prefabEntity.entity = GetEntity(prefab, tempEntity == null ? TransformUsageFlags.Dynamic : tempEntity.usage);
//							memeber.Set(comp, prefabEntity);
//						}
//					}
//				}
//				this.InvokeFunction(nameof(AddComponent), entity, comp);
//			}
//			foreach (var item in data.elements) {
//				if (item == null)
//					continue;
//				bufferCache[item.GetType()].InvokeFunction("Add", item);
//			}

//			//data._entity = entity;
//		}
//	}

//	public struct QEntityPath {
//		[QIgnore]
//		public Entity entity;
//		[QName, HideInInspector]
//		public FixedString64Bytes path;
//	}
//	public class FixedString64Info : QTypeInfo.CustomTypeInfo<FixedString64Bytes, string> {
//		public override string ChangeType(FixedString64Bytes obj) {
//			return obj.ToString();
//		}

//		public override FixedString64Bytes ChangeType(string obj) {
//			return (FixedString64Bytes)obj;
//		}
//	}
//	public interface IQComponment : IComponentData {

//	}
//	public interface IQBufferElement : IBufferElementData {
//	}
//	public static class QEntityTool
//	{
//		public static bool IsDefault(this in Entity obj) {
//			return obj.Index == 0 && obj.Version == 0;
//		}
//	}
//}


