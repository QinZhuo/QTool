using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	[RequireComponent(typeof(Animator))]
	public class QFirstPersonMesh : MonoBehaviour
	{
		[QName("分离手部模型")]
		public void SplitHandMesh()
		{
			var bodyMesh = GetComponentInChildren<SkinnedMeshRenderer>();
			var newMesh = bodyMesh.Split(HumanBodyBones.LeftShoulder);
			newMesh.CombineMeshes(new SkinnedMeshRenderer[] { bodyMesh.Split(HumanBodyBones.RightShoulder) }, true);
			newMesh.name = nameof(QFirstPersonMesh);
#if UNITY_EDITOR
			if (gameObject.IsPrefabInstance(out var prefab))
			{
				newMesh.sharedMesh.CheckSaveAsset(UnityEditor.AssetDatabase.GetAssetPath(prefab).Replace(".prefab","/"+name+".mesh"));
			}
#endif
		}
	}
}
