using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class WaveGenerator : MonoBehaviour {
    #region Serialized Members

    [Header("Grid Dimensions")]
    [SerializeField] private float gridSize = 10f;
    [SerializeField] private int gridDivisions = 200;
    [SerializeField] private int physicsGridDivisions = 20;
    [SerializeField] private Material meshMaterial;

    [Header("Wave Settings")]
    [SerializeField] private float waveScale = 0.3f;
    [SerializeField] private Vector2 waveOffsetSpeed = Vector2.one;
    [SerializeField] private float waveHeight = 0.3f;

    #endregion

    #region Private Members

    private Mesh _mesh;
    private Mesh _physicsMesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;
    private Rigidbody _rigidbody;
    private NativeArray<Vector3> _waterVertices;
    private NativeArray<Vector3> _waterNormals;
    private NativeArray<Vector3> _physicsVertices;
    private NativeArray<Vector3> _physicsNormals;
    private JobHandle _meshModJobHandle;
    private UpdateMeshJob _meshModJob;
    private JobHandle _physicsMeshModJobHandle;
    private UpdateMeshJob _physicsMeshModJob;

    #endregion

    private void Awake() {
        _mesh = new Mesh();
        _physicsMesh = new Mesh();
        _mesh.MarkDynamic();
        _physicsMesh.MarkDynamic();
        GenerateWaveMesh();

        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshFilter.sharedMesh = _mesh;
        
        _meshCollider = gameObject.AddComponent<MeshCollider>();
        _meshCollider.sharedMesh = _physicsMesh;
        
        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        _meshRenderer.material = meshMaterial;

        _rigidbody = gameObject.AddComponent<Rigidbody>();
        _rigidbody.isKinematic = true;
        
        _waterVertices = new NativeArray<Vector3>(_mesh.vertices, Allocator.Persistent);
        _waterNormals = new NativeArray<Vector3>(_mesh.normals, Allocator.Persistent);
        _physicsVertices = new NativeArray<Vector3>(_physicsMesh.vertices, Allocator.Persistent);
        _physicsNormals = new NativeArray<Vector3>(_physicsMesh.normals, Allocator.Persistent);
    }

    private void Update() {
        _meshModJob = new UpdateMeshJob() {
            Vertices = _waterVertices,
            Normals = _waterNormals,
            offsetSpeed = waveOffsetSpeed,
            time = Time.time,
            scale = waveScale,
            height = waveHeight
        };
        
        _physicsMeshModJob = new UpdateMeshJob() {
            Vertices = _physicsVertices,
            Normals = _physicsNormals,
            offsetSpeed = waveOffsetSpeed,
            time = Time.time,
            scale = waveScale,
            height = waveHeight
        };

        _meshModJobHandle = _meshModJob.Schedule(_waterVertices.Length, 64);
        _physicsMeshModJobHandle = _physicsMeshModJob.Schedule(_physicsVertices.Length, 64);
    }

    private void LateUpdate() {
        _meshModJobHandle.Complete();
        _physicsMeshModJobHandle.Complete();
        
        _mesh.SetVertices(_meshModJob.Vertices);
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        
        _physicsMesh.SetVertices(_physicsMeshModJob.Vertices);
        _physicsMesh.RecalculateNormals();
        _physicsMesh.RecalculateBounds();
        
        _meshCollider.sharedMesh = _physicsMesh;
    }

    private void FixedUpdate() {
        Vector3 move = new Vector3(-waveOffsetSpeed.x, 0, -waveOffsetSpeed.y) * Time.fixedDeltaTime / waveScale;
        _rigidbody.position -= move;
        _rigidbody.MovePosition(move);
    }

    private void OnDestroy() {
        _waterVertices.Dispose();
        _waterNormals.Dispose();
        _physicsVertices.Dispose();
        _physicsNormals.Dispose();
    }

    private void GenerateWaveMesh() {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> physicsVertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector3> physicsNormals = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<int> physicsTriangles = new List<int>();

        int pointCount = gridDivisions + 1;
        int physicsPointCount = physicsGridDivisions + 1;

        // create vertices
        for (int x = 0; x < pointCount; x++) {
            float xPos = gridSize * (x / (float) gridDivisions);
            for (int y = 0; y < pointCount; y++) {
                float yPos = gridSize * (y / (float) gridDivisions);
                Vector3 pos = new Vector3(xPos, 0, yPos);
                vertices.Add(pos);
                normals.Add(Vector3.up);
            }
        }
        
        // create physics vertices
        for (int x = 0; x < physicsPointCount; x++) {
            float xPos = gridSize * (x / (float) physicsGridDivisions);
            for (int y = 0; y < physicsPointCount; y++) {
                float yPos = gridSize * (y / (float) physicsGridDivisions);
                Vector3 pos = new Vector3(xPos, 0, yPos);
                physicsVertices.Add(pos);
                physicsNormals.Add(Vector3.up);
            }
        }

        // create triangles
        for (int x = 0; x < gridDivisions; x++) {
            for (int y = 0; y < gridDivisions; y++) {
                triangles.Add((x + 0) * pointCount + (y + 0));
                triangles.Add((x + 0) * pointCount + (y + 1));
                triangles.Add((x + 1) * pointCount + (y + 1));
                triangles.Add((x + 0) * pointCount + (y + 0));
                triangles.Add((x + 1) * pointCount + (y + 1));
                triangles.Add((x + 1) * pointCount + (y + 0));
            }
        }
        
        // create physics triangles
        for (int x = 0; x < physicsGridDivisions; x++) {
            for (int y = 0; y < physicsGridDivisions; y++) {
                physicsTriangles.Add((x + 0) * physicsPointCount + (y + 0));
                physicsTriangles.Add((x + 0) * physicsPointCount + (y + 1));
                physicsTriangles.Add((x + 1) * physicsPointCount + (y + 1));
                physicsTriangles.Add((x + 0) * physicsPointCount + (y + 0));
                physicsTriangles.Add((x + 1) * physicsPointCount + (y + 1));
                physicsTriangles.Add((x + 1) * physicsPointCount + (y + 0));
            }
        }

        // apply mesh data
        _mesh.SetVertices(vertices);
        _mesh.SetTriangles(triangles, 0);
        _mesh.SetNormals(normals);
        _mesh.RecalculateBounds();
        
        // apply physics mesh data
        _physicsMesh.SetVertices(physicsVertices);
        _physicsMesh.SetTriangles(physicsTriangles, 0);
        _physicsMesh.SetNormals(physicsNormals);
        _physicsMesh.RecalculateBounds();
    }
    
    [BurstCompile]
    private struct UpdateMeshJob : IJobParallelFor {
        public NativeArray<Vector3> Vertices;
        public NativeArray<Vector3> Normals;
        
        public float2 offsetSpeed;
        public float scale;
        public float height;
        public float time;

        public void Execute(int index) {
            Vector3 vertex = Vertices[index];
            Vertices[index] = CalculateHeight(vertex);
        }

        private Vector3 CalculateHeight(Vector3 vertex) {
            float noiseValue = Noise(
                vertex.x * scale + offsetSpeed.x * time,
                vertex.z * scale + offsetSpeed.y * time
            );
            return new Vector3(vertex.x, noiseValue * height, vertex.z);
        }

        private float Noise(float x, float y) {
            float2 pos = math.float2(x, y);
            return noise.snoise(pos);
        }
    }
}