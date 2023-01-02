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
		public override void OnSyncCheck()
		{
			QNetManager.Instance.SyncCheckFlag ^= (int)transform.position.x;
			QNetManager.Instance.SyncCheckFlag ^= (int)transform.position.y;
			QNetManager.Instance.SyncCheckFlag ^= (int)transform.position.z;
		}
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
					var minDis = radius + other.radius;
					var dir = transform.position-other.transform.position ;
					if (Mathf.Abs(dir.x) < minDis || Mathf.Abs(dir.y) < minDis)
					{
						if (dir.sqrMagnitude < minDis * minDis)
						{
							transform.position += dir.normalized * (radius + other.radius - dir.magnitude);
						}
					}
				}
			}
		}

	}
}

