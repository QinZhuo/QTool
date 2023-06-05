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
			var animator = GetComponent<Animator>();
			var skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
			skinnedMesh.gameObject.Split(animator.GetBoneTransform(HumanBodyBones.Neck).position, Vector3.up);
		}
	}
}
