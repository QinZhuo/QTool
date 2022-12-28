using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace QTool.Net
{
	public class QNetNavMeshAgent : QNetBehaviour
	{
		public override void OnNetStart()
		{
			
		}

		public override void OnNetUpdate()
		{
			if (NavMesh.SamplePosition(transform.position, out var hitInfo, 2, NavMesh.AllAreas))
			{
				transform.position = hitInfo.position;
			}
		}
	}
}

