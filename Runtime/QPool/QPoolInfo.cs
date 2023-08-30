using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QPoolInfo : MonoBehaviour
	{
		[QName("对象池"), QReadonly, SerializeField]
		internal string poolName = "";
	}

}
