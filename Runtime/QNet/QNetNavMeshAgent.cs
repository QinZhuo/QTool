using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace QTool.Net
{
	public class QNetNavMeshAgent : QNetBehaviour
	{
		[Min(0)]
		public float radius =0.5f;
		private static List<QNetNavMeshAgent> AllAgents = new List<QNetNavMeshAgent>();
		private void OnEnable()
		{
			AllAgents.Add(this);
		}
		private void OnDisable()
		{
			AllAgents.Remove(this);
		}
		public override void OnNetStart()
		{
		}

		public override void OnNetUpdate()
		{
			if (NavMesh.SamplePosition(transform.position, out var hitInfo, 2, NavMesh.AllAreas))
			{
				transform.position = hitInfo.position;
			}
			if (radius != 0)
			{
				foreach (var other in AllAgents)
				{
					if (other == this || other.radius == 0) continue;
					var dir = transform.position-other.transform.position ;
					var minDis = (radius + other.radius) * (radius + other.radius);
					if (dir.sqrMagnitude < minDis)
					{
						transform.position += dir.normalized * (radius + other.radius - dir.magnitude);
					}
				}
			}
		}
	}
}

