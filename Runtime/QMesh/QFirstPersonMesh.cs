using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	[RequireComponent(typeof(SkinnedMeshRenderer))]
	public class QFirstPersonMesh : MonoBehaviour
	{
		[QName("分割第一人称模型")]
		public void SplitMesh()
		{
			var bodyMesh = GetComponentInChildren<SkinnedMeshRenderer>();
			var handMesh = bodyMesh.Split(HumanBodyBones.LeftShoulder);
			handMesh.CombineMeshes(new SkinnedMeshRenderer[] { bodyMesh.Split(HumanBodyBones.RightShoulder) }, true);
			var headMesh = bodyMesh.Split(HumanBodyBones.Head);
			headMesh.name = "头部";
			bodyMesh.name = "身体";
			handMesh.name = "手部";
#if UNITY_EDITOR
			if (gameObject.IsPrefabInstance(out var prefab))
			{
				var path = UnityEditor.AssetDatabase.GetAssetPath(prefab);
				headMesh.sharedMesh.CheckSaveAsset(path.Replace(".prefab", "/头部.mesh"));
				bodyMesh.sharedMesh.CheckSaveAsset(path.Replace(".prefab", "/身体.mesh"));
				handMesh.sharedMesh.CheckSaveAsset(path.Replace(".prefab", "/手部.mesh"));
			}
#endif
		}
	}
}
