using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
namespace QTool.Net
{
	public class QNetNavMeshAgent : QNetBehaviour,IQNetSyncCheck
	{
		[Min(0)]
		public float radius =0.5f;
		private static List<QNetNavMeshAgent> AllAgents = new List<QNetNavMeshAgent>();
		public void Move(Vector3 offset)
		{
			transform.position += offset;
			if (radius != 0)
			{
				foreach (var other in AllAgents)
				{
					if (other == this || other.radius == 0) continue;
					var minDis = radius + other.radius;
					var dir = (transform.position - other.transform.position);
					if (Mathf.Abs(dir.x) < minDis || Mathf.Abs(dir.y) < minDis)
					{
						if (dir.sqrMagnitude < (minDis * minDis))
						{
							transform.position += (dir.normalized * (radius + other.radius - dir.magnitude));
						}
					}
				}
			}
		}
		public override void OnNetStart()
		{
			AllAgents.Add(this);
		}
		public override void OnNetDestroy()
		{
			AllAgents.Remove(this);
		}
		public override void OnNetUpdate()
		{
			if (NavMesh.SamplePosition(transform.position, out var hitInfo, 2, NavMesh.AllAreas))
			{
				transform.position = hitInfo.position;
			}
		}
		public void OnSyncCheck(QNetSyncFlag flag)
		{
			flag.Check(transform.position);
		}
		public void OnSyncSave(QBinaryWriter writer)
		{
			writer.WriteObject(transform.position);
		}
		public void OnSyncLoad(QBinaryReader reader)
		{
			transform.position = reader.ReadObject<Vector3>();
		}

	}
}

