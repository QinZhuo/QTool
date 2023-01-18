using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace QTool.Net
{
	[DisallowMultipleComponent]
	public class QNetNavMeshController : QNetBehaviour
	{
		[QSyncVar(true)]
		public Vector3 Position
		{
			get => transform.position;
			set => transform.position = value;
		}
		[QName("半径"),Min(0)]
		public float radius =0.5f;
		[QName("使用重力")]
		public bool useGravity = true;
		[QName("高度偏移")]
		public float heightOffset = -0.08f;
		public bool IsGrounded { get; private set; }
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
			if (useGravity)
			{
				transform.position += NetDeltaTime * Physics.gravity;
				if (NavMesh.SamplePosition(transform.position, out var hitInfo, 2, NavMesh.AllAreas))
				{
					IsGrounded = transform.position.y - heightOffset <= hitInfo.position.y;
					if (IsGrounded)
					{
						transform.position = new Vector3(hitInfo.position.x, IsGrounded ? hitInfo.position.y + heightOffset : transform.position.y, hitInfo.position.z);
					}
				}
				else
				{
					IsGrounded = false;
				}
			}
			else
			{
				if (NavMesh.SamplePosition(transform.position, out var hitInfo, 2, NavMesh.AllAreas))
				{
					transform.position = hitInfo.position;
					IsGrounded = true;
				}
			}
		
		}
		private static List<QNetNavMeshController> AllAgents = new List<QNetNavMeshController>();

	}
}

