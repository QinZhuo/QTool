#if ECS
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using QTool;
using QTool.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
namespace QTool
{
    public class QEntity : MonoBehaviour
    {
		public TransformUsageFlags usage = TransformUsageFlags.Dynamic;
	}
    public class QEntityBaker : Baker<QEntity>
    {
		private List<Type> typeList = new List<Type>();
		private QDictionary<Type, object> bufferCache = new QDictionary<Type, object>();
		public override void Bake(QEntity authoring)
		{
			typeList.Clear();
			bufferCache.Clear();
			var entity = GetEntity(authoring.usage);
			bufferCache.AutoCreate = type => QReflectionType.Get(GetType()).FunctionsCache[nameof(AddBuffer)][1][0].MethodInfo.MakeGenericMethod(type).Invoke(this, new object[] {entity});
            if (!QEntityData.ContainsKey(authoring.name)) return;
			var data = QEntityData.Get(authoring.name);

			foreach (var item in data.comps)
            {
				if (item == null)
					continue;
				var type = item.GetType();
				if (typeList.Contains(type))
					continue;
				typeList.Add(type);
				var typeInfo = QSerializeType.Get(type);
				foreach (var memeber in typeInfo.Members) {
					if (memeber.Type == typeof(QPrefabEntity) && memeber.Get(item) is QPrefabEntity prefabEntity) {
						var prefab = Resources.Load<GameObject>(prefabEntity.path.ToString());
						if (prefab != null) {
							var tempEntity= prefab.GetComponent<QEntity>();
							prefabEntity.entity = GetEntity(prefab, tempEntity == null ? TransformUsageFlags.Dynamic : tempEntity.usage);
							memeber.Set(item, prefabEntity);
						}
					}
				}
                this.InvokeFunction(nameof(AddComponent),entity, item);
            }
			foreach (var item in data.elements) {
				if (item == null)
					continue;
				bufferCache[item.GetType()].InvokeFunction("Add", item); 
			}
		}
    }
    public class QEntityData : QDataList<QEntityData>
    {
        public List<IQComponment> comps = new List<IQComponment>();
		public List<IQBufferElement> elements = new List<IQBufferElement>();
	}	
	public struct QPrefabEntity {
		public FixedString64Bytes path;
		[QIgnore]
		public Entity entity;
	}
	public interface IQComponment : IComponentData {

	}
	public interface IQBufferElement : IBufferElementData {

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
