using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Net;
using QTool;
public class QNetTestPlayer : QNetBehaviour
{
	public static List<QNetTestPlayer> Players = new List<QNetTestPlayer>();
	public GameObject bulletPrefab;
	public int Score = 0;
	public QNetNavMeshAgent agent;
	QTimer shootTimer = new QTimer(0.4f,true);
	public override void OnNetStart()
	{
		Players.AddCheckExist(this);
	}

	[QSyncAction]
	public void QNetActionTest()
	{
		Debug.LogError("shoot");
	}
	public override void OnNetUpdate()
	{
		if (agent != null)
		{  
			agent.Move(PlayerValue("位置", new Vector3(QInput.MoveDirection.x, 0, QInput.MoveDirection.y)) * NetDeltaTime * 3);
		}
		else
		{
			transform.position += PlayerValue("位置", new Vector3(QInput.MoveDirection.x, 0, QInput.MoveDirection.y)) * NetDeltaTime * 3;
		}
		var pos = PlayerValue("目标", QTool.QTool.RayCastPlane(Camera.main.ScreenPointToRay(QInput.PointerPosition), Vector3.up, transform.position));
		transform.LookAt(pos);
		shootTimer.Check(NetDeltaTime, false);
		if (PlayerValue("射击", QInput.PointerPress) && shootTimer.Check())
		{
			QNetActionTest();
			var bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);
			bullet.GetComponent<QNetTestBullet>().Player = this;
		}
	}
	public override string ToString()
	{
		return name + ":" + Score;
	}
	private void OnGUI()
	{
		if (Players.IndexOf(this) == 0) 
		{
			GUILayout.BeginArea(new Rect(Screen.width / 2-100, 0, Screen.width / 2, 100));
			GUILayout.Label(Players.ToOneString());
			GUILayout.EndArea();
		}
	
	}
}
