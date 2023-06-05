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
			var (aMesh,bMesh)= skinnedMesh.Split(HumanBodyBones.Neck);
			Instantiate(skinnedMesh.gameObject, skinnedMesh.transform.parent).GetComponent<MeshFilter>().sharedMesh = aMesh;
			Instantiate(skinnedMesh.gameObject, skinnedMesh.transform.parent).GetComponent<MeshFilter>().sharedMesh = bMesh;
		}
	}
}
