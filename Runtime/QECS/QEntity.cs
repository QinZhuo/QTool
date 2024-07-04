#if ECS
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
    }
    public class QEntityBaker : Baker<QEntity>
    {
        public override void Bake(QEntity authoring)
        {
            GetEntity(TransformUsageFlags.Dynamic);
            if (!QEntityData.ContainsKey(authoring.data)) return;
            foreach (var comp in QEntityData.Get(authoring.data).comps)
            {
				var typeInfo = QSerializeType.Get(comp.GetType());
				foreach (var memeber in typeInfo.Members) {
					if (memeber.Type == typeof(QPrefabEntity) && memeber.Get(comp) is QPrefabEntity prefabEntity) {
						var prefab = Resources.Load<GameObject>(prefabEntity.path.ToString());
						if (prefab != null) {
							prefabEntity.entity = GetEntity(prefab, TransformUsageFlags.Dynamic);
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
