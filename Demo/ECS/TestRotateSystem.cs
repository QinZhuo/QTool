#if ECS
using QTool;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;
public partial class TestRotateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var spin = quaternion.RotateY(SystemAPI.Time.DeltaTime * math.PI);
        foreach (var transform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<RotateData>())
        {
            transform.ValueRW.Rotation = math.mul(spin, transform.ValueRO.Rotation);
        }
    }
}
[System.Serializable]
public struct RotateData :IQEntityComponmentData
{
    public int a;
}
#endif
