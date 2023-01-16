using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    public class QSceneObjectSetting : InstanceManager<QSceneObjectSetting>
    {
		[QName("初始化列表")]
        public List<QId> qIdInitList = new List<QId>();
        [ExecuteInEditMode]
        protected override void Awake()
        {
			base.Awake();
			qIdInitList.RemoveAll((obj) => obj == null);
            foreach (var id in qIdInitList)
            {
                QId.InstanceIdList[id.Id] = id;
            }
        }
    }
}
