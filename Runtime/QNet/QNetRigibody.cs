using UnityEngine;
namespace QTool.Net
{
	[RequireComponent(typeof(Rigidbody))]
	public class QNetRigibody : QNetBehaviour
	{
		public new Rigidbody rigidbody;
		[QSyncVar(true)]
		public Vector3 RigPosition { get => rigidbody.position; set => rigidbody.position = value; }
		[QSyncVar]
		public Vector3 Velocity { get => rigidbody.velocity; set => rigidbody.velocity = value; }
		[QSyncVar]
		public Quaternion RigRotation { get => rigidbody.rotation; set => rigidbody.rotation = value; }
		[QSyncVar]
		public Vector3 AngularVelocity { get => rigidbody.angularVelocity; set => rigidbody.angularVelocity = value; }
		private void Reset()
		{
			rigidbody = GetComponent<Rigidbody>();
		}
		public override void OnNetUpdate()
		{
		}
	}
}

