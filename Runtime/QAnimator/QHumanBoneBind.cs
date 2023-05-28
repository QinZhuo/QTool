using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
namespace QTool
{
	public class QHumanBoneBind : MonoBehaviour
	{
		[QName("绑定骨骼"),QOnChange(nameof(Fresh))] public HumanBodyBones rootBone = HumanBodyBones.Hips;
		[QName("IK骨骼")]public List<HumanBodyBones> IkBones = new List<HumanBodyBones>();
		[QName("刷新")]
		public void Fresh()
		{
			transform.GetChild(rootBone.ToString(), true);
			foreach (var bone in IkBones)
			{
				transform.GetChild(bone.ToString(), true);
			}
		}
	}
}

