#if ECS
using System.Collections;
using System.Collections.Generic;
using QTool;
using QTool.Reflection;
using Unity.Burst;
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
            foreach (var item in QEntityData.Get(authoring.data).comps)
            {
                this.InvokeFunction(nameof(AddComponent), item);
            }
        }
    }
    public class QEntityData : QDataList<QEntityData>
    {
        public List<IComponentData> comps = new List<IComponentData>();
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
