using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QTriggerObject : MonoBehaviour
	{
		[QName("交互物体")]
		public bool IsInteract = false;
		public GameObjectEvent OnEvent=new GameObjectEvent();
	}
}

