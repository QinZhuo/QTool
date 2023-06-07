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
			if ( gameObject.IsPrefabInstance(out var prefab))
			{
				var path = UnityEditor.AssetDatabase.GetAssetPath(prefab).SplitStartString(".prefab"+"/");
				path.CheckDirectoryPath();
				UnityEditor.AssetDatabase.CreateAsset(newMesh.sharedMesh, path + newMesh.name + ".mesh");
				UnityEditor.AssetDatabase.CreateAsset(newMesh.sharedMaterial, path + newMesh.name + ".mat");
			}
		
		}
	}
}
