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
        [QPopup(nameof(QEntityData))]
        public string data;

		public TransformUsageFlags usage = TransformUsageFlags.Dynamic;
	}
    public class QEntityBaker : Baker<QEntity>
    {
		private List<Type> typeList = new List<Type>();
        public override void Bake(QEntity authoring)
        {
			typeList.Clear();
			GetEntity(authoring.usage);
            if (!QEntityData.ContainsKey(authoring.data)) return;
            foreach (var comp in QEntityData.Get(authoring.data).comps)
            {
				var type = comp.GetType();
				if (typeList.Contains(type))
					continue;
				typeList.Add(type);
				var typeInfo = QSerializeType.Get(type);
				foreach (var memeber in typeInfo.Members) {
					if (memeber.Type == typeof(QPrefabEntity) && memeber.Get(comp) is QPrefabEntity prefabEntity) {
						var prefab = Resources.Load<GameObject>(prefabEntity.path.ToString());
						if (prefab != null) {
							var entity= prefab.GetComponent<QEntity>();
							prefabEntity.entity = GetEntity(prefab, entity == null ? TransformUsageFlags.Dynamic : entity.usage);
							memeber.Set(comp, prefabEntity);
						}
					}
				}
                this.InvokeFunction(nameof(AddComponent), comp);
            }
        }
    }
    public class QEntityData : QDataList<QEntityData>
    {
        public List<IQEntityComponmentData> comps = new List<IQEntityComponmentData>();
    }	
	public struct QPrefabEntity {
		public FixedString64Bytes path;
		[QIgnore]
		public Entity entity;
	}
	public interface IQEntityComponmentData : IComponentData {

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
