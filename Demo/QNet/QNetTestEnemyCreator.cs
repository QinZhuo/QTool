using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Net;
using QTool;
public class QNetTestEnemyCreator : QNetBehaviour
{
	public GameObject prefab;
	QTimer waitTime = new QTimer(2);
	public override void OnNetStart()
	{
		
	}

	public override void OnNetUpdate()
	{
		if (waitTime.Check(NetDeltaTime* QNetTestPlayer.Players.Count))
		{
			var dir = Random.Direction2D();
			Instantiate(prefab, new Vector3(dir.x,0,dir.y) * 20, Quaternion.identity);
		}	
	}
}
