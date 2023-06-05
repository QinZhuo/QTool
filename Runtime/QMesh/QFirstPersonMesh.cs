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
			var skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
			var (headMesh,bodyMesh)= skinnedMesh.Split(HumanBodyBones.Neck);
			skinnedMesh.sharedMesh = bodyMesh.CombineMeshes(headMesh);
			Mesh leftMesh, rightMesh;
			(leftMesh, bodyMesh) = skinnedMesh.Split(HumanBodyBones.LeftUpperArm);
			skinnedMesh.sharedMesh = bodyMesh;
			(rightMesh, bodyMesh) = skinnedMesh.Split(HumanBodyBones.RightUpperArm);
			skinnedMesh.sharedMesh = bodyMesh.CombineMeshes(leftMesh.CombineMeshes(rightMesh,true));
		}
	}
}
