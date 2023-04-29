using System.Collections;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Updates mesh collider with up to date values from the vertex shader
/// </summary>

[SelectionBase]
public class MeshColliderUpdater : MonoBehaviour
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
        UpdateMeshCollider();
    }

    private void InitializeBuffer()
    {
        int vertexCount = _meshFilter.sharedMesh.vertexCount;
        int strideSize = Marshal.SizeOf(typeof(float3));

        m_vertexPositionsBuffer = new ComputeBuffer(vertexCount, strideSize);
        _meshRenderer.sharedMaterial.SetBuffer(s_bufferId, m_vertexPositionsBuffer);
        Graphics.SetRandomWriteTarget(1, m_vertexPositionsBuffer);
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
    }
    
    private void ApplyVertexDeformations(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] offsets = ReadBuffer();

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].x = offsets[i].x + .01f;
            vertices[i].y = offsets[i].y + .02f;
            vertices[i].z = offsets[i].z;
        }

        mesh.SetVertices(vertices);
    }
    
    private Vector3[] ReadBuffer()
    {
        int vertexCount = _meshFilter.sharedMesh.vertexCount;
        float3[] incomingDataFromGPU = new float3[vertexCount];

        m_vertexPositionsBuffer.GetData(incomingDataFromGPU);
        
        return ConvertToVector3(incomingDataFromGPU);
    }
    
    private Vector3[] ConvertToVector3(float3[] floatArray)
    {
        Vector3[] vectorArray = new Vector3[floatArray.Length];
        for (int i = 0; i < floatArray.Length; i++)
        {
            vectorArray[i] = new Vector3(floatArray[i].x, floatArray[i].y, floatArray[i].z);
        }

        return vectorArray;
    }

    private void OnDisable()
    {
        m_vertexPositionsBuffer?.Dispose();
    }
}
