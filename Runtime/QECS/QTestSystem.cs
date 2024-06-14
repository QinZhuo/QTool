#if ECS
using System.Numerics;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct Comp1 : IComponentData
{
    public Vector2 pos;
}
public struct Comp2 : IComponentData
{
    public float speed;
    public Entity prefab;
}
public partial struct QTestSystem : ISystem
{

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Comp2>();
    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        double elapsedTime = SystemAPI.Time.ElapsedTime;
        foreach (var (transform, speed) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<Comp2>>())
        {
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
    readonly RefRO<Comp2> m_Speed;

    public void Move(double elapsedTime)
    {
        m_Transform.ValueRW.Position.y = (float)math.sin(elapsedTime * m_Speed.ValueRO.speed);
    }
}
#endif
