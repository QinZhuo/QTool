using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QInteractTrigger : MonoBehaviour
	{
		[QName("手动触发"),UnityEngine.Serialization.FormerlySerializedAs("IsInteract")]
		public bool IsManual = false;
		[UnityEngine.Serialization.FormerlySerializedAs("OnEvent")]
		public GameObjectEvent OnTrigger=new GameObjectEvent();
	}
}

