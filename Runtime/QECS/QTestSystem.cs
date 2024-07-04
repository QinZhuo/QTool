#if ECS
using QTool;
using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting.YamlDotNet.Core;
using static UnityEngine.EventSystems.EventTrigger;
[QOldName("Comp1")]
public struct TestRotateComp : IQEntityComponmentData {
}
public struct TestMoveComp : IQEntityComponmentData {
}
[QOldName("Comp2")]
public struct TestSpeedComp : IQEntityComponmentData {
    public float speed;
}
public struct TestShooterComp : IQEntityComponmentData {
	public QPrefabEntity prefab;
}
public partial struct QTestSystem : ISystem
{

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<TestRotateComp>();
    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
		
		#region 使用多个组件旋转
		foreach (var (transform, rotate, speed) in
			 SystemAPI.Query<RefRW<LocalTransform>, RefRO<TestRotateComp>, RefRO<TestSpeedComp>>()) {
			transform.ValueRW = transform.ValueRO.RotateY(
				speed.ValueRO.speed * deltaTime);
		}
		#endregion
		#region 使用IAspect移动
		foreach (var movement in
					SystemAPI.Query<VerticalMovementAspect>()) {
			movement.Move(deltaTime);
		}
		#endregion
		//#region 使用Job多线程旋转
		//var job = new RotateJob {
		//	deltaTime = SystemAPI.Time.DeltaTime,
		//	elapsedTime = (float)SystemAPI.Time.ElapsedTime
		//};
		//job.Schedule();
		//#endregion
		#region 创建预制体
		foreach (var (trans, shooter) in
				SystemAPI.Query<RefRO<LocalTransform>, RefRO<TestShooterComp>>()) {
			if (shooter.ValueRO.prefab.entity.Index == 0)
				continue;
			var entity = state.EntityManager.Instantiate(shooter.ValueRO.prefab.entity);
			var transform = SystemAPI.GetComponentRW<LocalTransform>(entity);
			transform.ValueRW.Position = trans.ValueRO.Position;
			transform.ValueRW.Rotation = trans.ValueRO.Rotation;
		}
		#endregion
	}
}
readonly partial struct VerticalMovementAspect : IAspect
{
    readonly RefRW<LocalTransform> m_Transform;
	readonly RefRO<TestMoveComp> m_Move;
	readonly RefRO<TestSpeedComp> m_Speed;

    public void Move(float deltaTime)
    {
		m_Transform.ValueRW.Position += m_Speed.ValueRO.speed * deltaTime * m_Transform.ValueRW.Forward();
    }
}
[BurstCompile]
partial struct RotateJob : IJobEntity {
	public float deltaTime;
	public float elapsedTime;
	void Execute(ref LocalTransform transform,in TestRotateComp rotate, in TestSpeedComp speed) {
		transform = transform.RotateY(speed.speed * deltaTime);
	}
}
#endif
