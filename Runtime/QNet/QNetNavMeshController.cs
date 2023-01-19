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
		[QName("网格高度偏移")]
		public float meshOffset = -0.08f;
		[QName("使用重力")]
		public bool useGravity = true;
		[QName("半径"),Min(0)]
		public float radius =0.5f;
		[QName("高度"), Min(0)]
		public float height = 2f;
		public bool IsGrounded { get; private set; }
		public float VelocityY { get; set; }
		public void Move(Vector3 offset)
		{
			transform.position += offset;
			if (radius >0)
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
				transform.position += Vector3.up* VelocityY * NetDeltaTime;
				if (NavMesh.SamplePosition(transform.position, out TargetMeshHit, 2, NavMesh.AllAreas))
				{
					TargetMeshHit.position += Vector3.up * meshOffset;
					if (MeshHit.position.y+0.1f >= TargetMeshHit.position.y || transform.position.y+0.1f >= TargetMeshHit.position.y)
					{
						MeshHit = TargetMeshHit;
					}
				}
				IsGrounded = transform.position.y<= MeshHit.position.y+0.1f;
				VelocityY += Physics.gravity.y * NetDeltaTime;
				if (IsGrounded&& VelocityY <= 0)
				{
					VelocityY = 0;
					transform.position = MeshHit.position;
				}
				else
				{
					var dir = transform.position - MeshHit.position;
					var targetPos = transform.position+Vector3.down*height + dir;
					var hit = NavMesh.SamplePosition(targetPos, out TargetMeshHit, height, NavMesh.AllAreas);
					if (hit)
					{
						if(NavMesh.SamplePosition(targetPos+Vector3.down, out var heightHit, height, NavMesh.AllAreas))
						{
							if (heightHit.position.y != transform.position.y)
							{
								var offset = heightHit.position - transform.position;
								offset.y = 0;
								var targetOffset = TargetMeshHit.position - transform.position;
								targetOffset.y = 0;
								if (offset.sqrMagnitude > targetOffset.sqrMagnitude)
								{
									TargetMeshHit = heightHit;
								}
							}
						}
					}
					if (hit&& transform.position.y>TargetMeshHit.position.y+meshOffset)
					{
						MeshHit = TargetMeshHit;
					}
					else
					{
						transform.position = new Vector3(MeshHit.position.x, transform.position.y, MeshHit.position.z);
					}
				}
			}
			else
			{
				if (NavMesh.SamplePosition(transform.position, out TargetMeshHit, 2, NavMesh.AllAreas))
				{
					transform.position = TargetMeshHit.position;
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
				Gizmos.DrawLine(MeshHit.position, TargetMeshHit.position);
				Gizmos.color =Color.Lerp(Color.blue,Color.clear,0.8f);
				Gizmos.DrawSphere(TargetMeshHit.position, 0.04f);
			}

		}

	}
}

