using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Net;
using QTool;
public class QNetTestBullet : QNetBehaviour
{
	public float damage=10;
	public float speed=20;
	float time = 0;
	public QNetTestPlayer Player;
	public override void OnNetStart()
	{
		
	}

	public override void OnNetUpdate()
	{
		transform.position+=transform.forward*Time.fixedDeltaTime*speed;
		time += Time.fixedDeltaTime;
		if (time > 3)
		{
			Destroy(gameObject);
		}
	}
	private void OnTriggerEnter(Collider other)
	{
		var enemy = other.GetComponentInChildren<QNetTestEnemy>();
		if (enemy != null&&enemy.Level>0)
		{
			Player.Score++;
			enemy.SetLevel(enemy.Level - 1);
		}
		if (other.GetComponent<QNetTestPlayer>() == null&& other.name!="Plane")
		{
			Destroy(gameObject);
		}
	}
}
