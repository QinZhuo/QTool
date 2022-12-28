using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Net;
using QTool;
using UnityEngine.AI;
public class QNetTestEnemy : QNetBehaviour
{
	private QNetTestPlayer Target;
	public QNetTestPlayer GetTarget()
	{
		return QNetTestPlayer.Players.RandomGet(Random);
	}
	public int Level { get; private set; } = 0;
	public void SetLevel(int newLevel)
	{
		Level = newLevel;
		transform.localScale = Vector3.one * QLerp.LerpTo(0.3f,1.5f, Level / 4f);
		if (Level <= 0)
		{
			Destroy(gameObject);
		}
	}
	public override void OnNetStart()
	{
		SetLevel(Random.Range(1, 4));
	}

	public override void OnNetUpdate()
	{
		if (Target == null)
		{
			Target = GetTarget();
		}
		if (Target == null) return;
		transform.LookAt(Target.transform);
		transform.transform.position += transform.forward * NetDeltaTime*1.5f;
	}
}
