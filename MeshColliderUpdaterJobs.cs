using System.Collections;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Updates mesh collider with up to date values from the vertex shader
/// </summary>

[SelectionBase]
public class MeshColliderUpdaterJobs : MonoBehaviour
{
    [SerializeField]
    private MeshCollider _meshCollider;

    [SerializeField]
    private MeshRenderer _meshRenderer;

    [SerializeField]
    private MeshFilter _meshFilter;

    private ComputeBuffer m_vertexPositionsBuffer;
    private Mesh m_runtimeMesh;

    private static readonly int s_bufferId = Shader.PropertyToID("_VertexPositionsBuffer");

    private NativeArray<Vector3> m_verticesArray;
    private NativeArray<Vector3> m_offsetsArray;
    private Vector3[] m_incomingDataFromGPU;

    private void Reset()
    {
        _meshFilter = GetComponentInChildren<MeshFilter>();
        _meshCollider = GetComponentInChildren<MeshCollider>();
        _meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    private IEnumerator Start()
    {
        enabled = false;
        InitializeBuffer();
        yield return new WaitForSeconds(.25f);
        enabled = true;
        CreateRuntimeMesh();
    }   

    private void FixedUpdate()
    {
        ReadBuffer();
        UpdateMeshCollider();
    }

    private void InitializeBuffer()
    {
        int vertexCount = _meshFilter.sharedMesh.vertexCount;
        int strideSize = Marshal.SizeOf(typeof(float3));

        m_vertexPositionsBuffer = new ComputeBuffer(vertexCount, strideSize);
        _meshRenderer.sharedMaterial.SetBuffer(s_bufferId, m_vertexPositionsBuffer);
        Graphics.SetRandomWriteTarget(1, m_vertexPositionsBuffer);
        
        m_offsetsArray = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
        m_incomingDataFromGPU = new Vector3[vertexCount];
    }

    private void UpdateMeshCollider()
    {
        ApplyVertexDeformations(m_runtimeMesh);
        _meshCollider.sharedMesh = m_runtimeMesh;
    }

    private void CreateRuntimeMesh()
    {
        Mesh sourceMesh = _meshFilter.sharedMesh;
        
        m_runtimeMesh = new Mesh
        {
            name = "RuntimeMesh",
            vertices = sourceMesh.vertices,
            triangles = sourceMesh.triangles,
            uv = sourceMesh.uv,
            normals = sourceMesh.normals,
            bounds = sourceMesh.bounds,
            colors = sourceMesh.colors,
            tangents = sourceMesh.tangents
        };
        
        m_verticesArray = new NativeArray<Vector3>(m_runtimeMesh.vertexCount, Allocator.Persistent);
    }

    private void ApplyVertexDeformations(Mesh mesh)
    {
        JobHandle jobHandle = new ApplyVertexDeformJob(m_verticesArray, m_offsetsArray).Schedule(mesh.vertexCount, 64);
        jobHandle.Complete();

        mesh.SetVertices(m_verticesArray);
    }

    private void ReadBuffer()
    {
        m_vertexPositionsBuffer.GetData(m_incomingDataFromGPU);
        m_offsetsArray.CopyFrom(m_incomingDataFromGPU);
    }

    private void OnDisable()
    {
        m_vertexPositionsBuffer?.Dispose();
        m_verticesArray.Dispose();
        m_offsetsArray.Dispose();
    }
}