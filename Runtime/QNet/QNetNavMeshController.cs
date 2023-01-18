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
			MeshHit.position = transform.position;
			AllAgents.Add(this);
		}
		public override void OnNetDestroy()
		{
			AllAgents.Remove(this);
		}
		NavMeshHit MeshHit =default;
		NavMeshHit TargetMeshHit = default;
		public override void OnNetUpdate()
		{
			if (useGravity)
			{
				transform.position += NetDeltaTime * Physics.gravity;
				if (NavMesh.SamplePosition(transform.position, out TargetMeshHit, 2, NavMesh.AllAreas))
				{
					TargetMeshHit.position += Vector3.up * heightOffset;
					if (MeshHit.position.y+0.1f >= TargetMeshHit.position.y || transform.position.y+0.1f >= TargetMeshHit.position.y)
					{
						MeshHit = TargetMeshHit;
					}
				}
				IsGrounded = transform.position.y<= MeshHit.position.y;
				if (IsGrounded)
				{
					transform.position = MeshHit.position;
				}
				else
				{
					var dir = transform.position - MeshHit.position;
					dir.y = 0;
					if (!Mathf.Approximately(dir.sqrMagnitude, 0) &&
						NavMesh.SamplePosition(transform.position + dir.normalized*0.1f, out TargetMeshHit, 1, NavMesh.AllAreas)&&
						MeshHit.position.y>TargetMeshHit.position.y+heightOffset&&transform.position.y+0.1> TargetMeshHit.position.y+heightOffset)
					{
						return;
					}
					transform.position = new Vector3(MeshHit.position.x, transform.position.y, MeshHit.position.z);
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

		protected void OnDrawGizmos()
		{
			if (Application.IsPlaying(this))
			{
				Gizmos.color =Color.green;
				Gizmos.DrawSphere(MeshHit.position, 0.05f);
				Gizmos.DrawLine(transform.position, MeshHit.position);
				Gizmos.color = Color.blue;
				Gizmos.DrawSphere(TargetMeshHit.position, 0.05f);
			}

		}

	}
}

