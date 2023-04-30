using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct ApplyVertexDeformJob : IJobParallelFor
{
    private NativeArray<Vector3> m_verts;
    private NativeArray<Vector3> m_offsets;

    public ApplyVertexDeformJob(NativeArray<Vector3> vert, NativeArray<Vector3> offsets)
    {
        m_verts = vert;
        m_offsets = offsets;
    }

    public void Execute(int index)
    {
        m_verts[index] = m_offsets[index];
    }
}