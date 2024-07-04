#if ECS
using QTool;
using System.Numerics;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
[QOldName("Comp1")]
public struct TestRotateComp : IQEntityComponmentData {
}
public struct TestMoveComp : IQEntityComponmentData {
}
[QOldName("Comp2")]
public struct TestSpeedComp : IQEntityComponmentData {
    public float speed;
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
        double elapsedTime = SystemAPI.Time.ElapsedTime;
		foreach (var (transform, rotate, speed) in
				 SystemAPI.Query<RefRW<LocalTransform>, RefRO<TestRotateComp>, RefRO<TestSpeedComp>>()) {
			transform.ValueRW = transform.ValueRO.RotateY(
				speed.ValueRO.speed * deltaTime);
		}
        foreach (var movement in
                    SystemAPI.Query<VerticalMovementAspect>())
        {
            movement.Move(elapsedTime);
        }
    }
}
readonly partial struct VerticalMovementAspect : IAspect
{
    readonly RefRW<LocalTransform> m_Transform;
	readonly RefRO<TestMoveComp> m_Move;
	readonly RefRO<TestSpeedComp> m_Speed;

    public void Move(double elapsedTime)
    {
        m_Transform.ValueRW.Position.y = (float)math.sin(elapsedTime * m_Speed.ValueRO.speed);
    }
}
#endif
