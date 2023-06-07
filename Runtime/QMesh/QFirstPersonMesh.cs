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
			var leftMesh = bodyMesh.Split(HumanBodyBones.LeftShoulder);
			var rightMesh = bodyMesh.Split(HumanBodyBones.RightShoulder);
			leftMesh.CombineMeshes(new SkinnedMeshRenderer[] { rightMesh }, true);
		}
	}
}
