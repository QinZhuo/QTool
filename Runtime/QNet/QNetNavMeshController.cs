using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace QTool.Net
{
	[DisallowMultipleComponent,RequireComponent(typeof(CharacterController))]
	public class QNetNavMeshController : QNetBehaviour
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
		public bool Move(Vector3 speed)
		{
			return Controller.SimpleMove(speed);
		}
		public override void OnNetUpdate()
		{
			if (IsGrounded)
			{
				if (NavMesh.SamplePosition(transform.position, out var MeshHit, 1, NavMesh.AllAreas))
				{
					transform.position = MeshHit.position;
				}
			}
		}
	}
}

