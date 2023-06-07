using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	[RequireComponent(typeof(Animator))]
	public class QFirstPersonMesh : MonoBehaviour
	{

		[QName("分离第一人称模型")]
		public void SplitHandMesh()
		{
			var bodyMesh = GetComponentInChildren<SkinnedMeshRenderer>();
			var handMesh = bodyMesh.Split(HumanBodyBones.LeftShoulder);
			handMesh.CombineMeshes(new SkinnedMeshRenderer[] { bodyMesh.Split(HumanBodyBones.RightShoulder) }, true);
			handMesh.name = nameof(QFirstPersonMesh);
			var headMesh=bodyMesh.Split(HumanBodyBones.Head);
			bodyMesh.CombineMeshes(new SkinnedMeshRenderer[] { handMesh, headMesh });
//#if UNITY_EDITOR
//			if (gameObject.IsPrefabInstance(out var prefab))
//			{
//				handMesh.sharedMesh.CheckSaveAsset(UnityEditor.AssetDatabase.GetAssetPath(prefab).Replace(".prefab","/"+name+".mesh"));
//			}
//#endif
		}
	}
}
