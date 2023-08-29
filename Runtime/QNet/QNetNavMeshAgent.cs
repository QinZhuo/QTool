using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace QTool.Net
{
	[DisallowMultipleComponent]
	public class QNetNavMeshAgent : QNetBehaviour
	{
		[QSyncVar(true)]
		public Vector3 Position
		{
			get => transform.position;
			set => transform.position = value;
		}
		public CharacterController Controller;

		public virtual bool IsGrounded => Controller==null|| Controller.isGrounded;
		public CollisionFlags Move(Vector3 speed)
		{
			if (Controller == null)
			{
				transform.Translate(speed);
				return CollisionFlags.None;
			}
			else
			{
				return Controller.Move(speed);
			}
		}
		public NavMeshPath Path { get; private set; }
		public bool FindPath(Vector3 target)
		{
			if (Path == null)
			{
				NavMeshPath Path = new NavMeshPath();
			}
			else if (Path.status == NavMeshPathStatus.PathComplete && target.Similar(Path.corners.StackPeek()) && transform.position.Similar(Path.corners.QueuePeek()))
			{
				return true;
			}
			return NavMesh.CalculatePath(transform.position, target, NavMesh.AllAreas, Path);
		}
		public override void OnNetUpdate()
		{
			if (IsGrounded)
			{
				if (NavMesh.SamplePosition(transform.position, out var MeshHit, 1, NavMesh.AllAreas))
				{
					var meshPosition = MeshHit.position;
					meshPosition.y = transform.position.y;
					if (!transform.position.Similar(meshPosition))
					{
						transform.position = meshPosition;
					}
				}
			}
		}
	}
}

