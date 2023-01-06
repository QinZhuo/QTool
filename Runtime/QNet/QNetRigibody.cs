using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Net
{
	[RequireComponent(typeof(Rigidbody))]
	public class QNetRigibody : QNetBehaviour, IQNetSyncCheck
	{
		public new Rigidbody rigidbody;
		private void Reset()
		{
			rigidbody = GetComponent<Rigidbody>();
		}
		public override void OnNetUpdate()
		{
		}

		public void OnSyncCheck(QNetSyncFlag flag)
		{
			flag.Check(rigidbody.position);
		}

		public void OnSyncLoad(QBinaryReader reader)
		{
			rigidbody.position = reader.ReadObject<Vector3>();
			rigidbody.velocity = reader.ReadObject<Vector3>();
		}

		public void OnSyncSave(QBinaryWriter writer)
		{
			writer.WriteObject(rigidbody.position);
			writer.WriteObject(rigidbody.velocity);
		}
	}
}

