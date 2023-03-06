using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace QTool.Net
{
	[DisallowMultipleComponent,RequireComponent(typeof(CharacterController))]
	public class QNetNavMeshAgent : QNetBehaviour
	{
		[QSyncVar(true)]
		public Vector3 Position
		{
			get => transform.position;
			set => transform.position = value;
		}
		private CharacterController _Controller;
		public CharacterController Controller => _Controller ??= GetComponent<CharacterController>();
		public bool IsGrounded => Controller.isGrounded;
		public CollisionFlags Move(Vector3 speed)
		{
			return Controller.Move(speed);
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

