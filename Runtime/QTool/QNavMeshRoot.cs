using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace QTool
{
	[ExecuteInEditMode]
	public class QNavMeshRoot : MonoBehaviour
	{
		private void OnEnable()
		{
			QNavMesh.AddRoot(transform);
		}
		private void OnDisable()
		{
			QNavMesh.RemoveRoot(transform);
		}
	}
}

