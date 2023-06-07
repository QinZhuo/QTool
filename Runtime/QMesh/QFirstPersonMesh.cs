using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	[RequireComponent(typeof(Animator))]
	public class QFirstPersonMesh : MonoBehaviour
	{
		[QName("生成第一人称模型")]
		public void SplitMesh()
		{
			var bodyMesh = GetComponentInChildren<SkinnedMeshRenderer>();
			var headMesh= bodyMesh.Split(HumanBodyBones.Neck);
			bodyMesh.CombineMeshes(new SkinnedMeshRenderer[] { bodyMesh, headMesh });
			var leftMesh = bodyMesh.Split(HumanBodyBones.LeftUpperArm);
			var rightMesh = bodyMesh.Split(HumanBodyBones.RightUpperArm);
			bodyMesh.CombineMeshes(new SkinnedMeshRenderer[] { bodyMesh,leftMesh, rightMesh });
		}
	}
}
