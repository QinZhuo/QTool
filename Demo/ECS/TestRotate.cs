
using QTool.ECS;
using UnityEngine;
[System.Serializable]
public class TestRotate : QComponent {
	public int a;
}
public class TestRotateSystem : QSystem<QEntity, TestRotate> {
	public override void Init() {
	}
	public override void Query(ref QEntity entity, ref TestRotate rotate) {
		entity.transform.Rotate(Vector3.right * rotate.a * 360 * Time.deltaTime);
	}
}

