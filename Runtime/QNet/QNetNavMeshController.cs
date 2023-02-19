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
		[QName("使用重力")]
		public bool useGravity = true;
		[QName("半径"),Min(0)]
		public float radius =0.5f;
		public bool IsGrounded { get; private set; }
		public float VelocityY { get; set; }
		public Vector3 MoveOffset { get; private set; }
		public void Move(Vector3 offset)
		{
			if (offset != Vector3.zero)
			{
				MoveOffset = offset;
			}
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
		const float CheckOffset = 0.08f;
	
		public override void OnNetUpdate()
		{
			if (useGravity)
			{
				transform.position += Vector3.up* VelocityY * NetDeltaTime;
				var checkPoint = transform.position;
				if(Physics.Raycast(checkPoint+Vector3.up* CheckOffset, Vector3.down,out var hitInfo)){
					checkPoint = hitInfo.point;
				}
				NavMesh.SamplePosition(checkPoint, out MeshHit, 2, NavMesh.AllAreas);
				IsGrounded = transform.position.y <= MeshHit.position.y + CheckOffset;
				VelocityY += Physics.gravity.y * NetDeltaTime;
				if (IsGrounded&& VelocityY <= 0)
				{
					VelocityY = 0;
					transform.position = MeshHit.position;
				}
			}
			else
			{
				if (NavMesh.SamplePosition(transform.position, out MeshHit, 2, NavMesh.AllAreas))
				{
					transform.position = MeshHit.position;
					IsGrounded = true;
				}
			}
		
		}
		private static List<QNetNavMeshController> AllAgents = new List<QNetNavMeshController>();

		protected void OnDrawGizmos()
		{
			if (Application.IsPlaying(this))
			{
				Gizmos.color = IsGrounded ? Color.green:Color.red;
				Gizmos.DrawSphere(MeshHit.position, 0.05f);
				Gizmos.color =Color.Lerp(Color.blue,Color.clear,0.5f);
				Gizmos.DrawLine(transform.position,transform.position+MoveOffset.normalized * radius);
			}
		}

	}
}

