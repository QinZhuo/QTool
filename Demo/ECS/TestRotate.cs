
using QTool.ECS;
using UnityEngine;
[System.Serializable]
public class TestRotate : QComponent<TestRotate> {
	public int a;
}
public class TestRotateSystem : QuerySystem<QEntityObject, TestRotate> {
	public override void Query(ref QEntityObject entity, ref TestRotate rotate) {
		entity.transform.Rotate(Vector3.right * rotate.a * 360 * Time.deltaTime);
	}
}

