using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public struct BoidData
{
    public Vector3 position;
    public float scale;
    public Vector3 velocity;
    public float dummy;
}

[Serializable]
public class BoidModel
{
    public float massPerUnit;
    public float MaxForce;
    public float MaxSpeed;
    public float Radius;
}

public class FlockingCompute : MonoBehaviour
{
    private const int GroupSize = 256;

    [Header("Compute Buffer stuff")] [SerializeField]
    private int instances;

    [SerializeField] private ComputeShader flockingShader;
    [SerializeField] private Bounds boidBounds;
    [SerializeField] private BoidModel boidModel;

    private List<BoidData> boidsList = new List<BoidData>();
    private List<Vector3> boidsForcesList = new List<Vector3>();
    private ComputeBuffer boidBuffer;
    private ComputeBuffer boidForcesBuffer;
    private int FlockingKernel;
    private int KinematicKernel;

    [SerializeField, Range(1, 10)] private float SeparationWeight;
    [SerializeField, Range(1, 10)] private float CohesionWeight;
    [SerializeField, Range(1, 10)] private float AlignWeight;


    [Header("Indirect draw stuff")] [SerializeField]
    private Mesh BoidMesh;

    [SerializeField] private Shader BoidInstanceShader;

    private Material BoidDrawMaterial;
    //private ComputeBuffer BufferWithArgs;
    //private uint[] args = new uint[5] {0, 0, 0, 0, 0};

    private void Start()
    {
        InitBuffersCompute();
        //  InitBuffersDraw();
        InitMaterial();
        SetBuffers();
    }

    private void InitMaterial()
    {
        BoidDrawMaterial = new Material(BoidInstanceShader)
        {
            name = "Boids",
            hideFlags = HideFlags.HideAndDontSave,
            enableInstancing = true
        };
    }

    private void SetBuffers()
    {
        flockingShader.SetBuffer(FlockingKernel, "_BoidsBuffer", boidBuffer);
        flockingShader.SetBuffer(FlockingKernel, "_BoidsNetForces", boidForcesBuffer);

        flockingShader.SetBuffer(KinematicKernel, "_BoidsBuffer", boidBuffer);
        flockingShader.SetBuffer(KinematicKernel, "_BoidsNetForces", boidForcesBuffer);

        BoidDrawMaterial.SetBuffer("_BoidsBuffer", boidBuffer);

        InitConstantBuffer();
    }
/*
    private void InitBuffersDraw()
    {
        args[0] = (uint) BoidMesh.GetIndexCount(0);
        args[1] = (uint) instances;
        args[2] = (uint) BoidMesh.GetIndexStart(0);
        args[3] = (uint) BoidMesh.GetBaseVertex(0);
        BufferWithArgs = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        BufferWithArgs.SetData(args);
    }
    */

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.2f);
        Gizmos.DrawCube(boidBounds.center, boidBounds.size);
    }

    void Update()
    {
        Dispatch();

        // BoidData[] fetchBoids = new BoidData[instances];
        // boidBuffer.GetData(fetchBoids);
        // for (var index = 0; index < fetchBoids.Length; index++)
        // {
        //     var a = fetchBoids[index];
        //     Debug.DrawLine(a.position, a.position + a.velocity.normalized, Color.black);
        // }
    }

    public void LateUpdate()
    {
        Graphics.DrawMeshInstancedProcedural(BoidMesh, 0, BoidDrawMaterial, boidBounds, instances);
        //Graphics.DrawMeshInstancedIndirect(BoidMesh, 0, BoidDrawMaterial, boidBounds, BufferWithArgs);
    }

    private void Dispatch()
    {
        flockingShader.SetFloat("_DeltaTime", Time.deltaTime);
        flockingShader.SetVector("_ForceWeights", new Vector3(SeparationWeight,CohesionWeight, AlignWeight));
        int TG = Mathf.CeilToInt((float) instances / GroupSize);

        if (Time.frameCount % 2 == 0)
        {
            flockingShader.Dispatch(FlockingKernel, TG, 1, 1);
        }

        flockingShader.Dispatch(KinematicKernel, TG, 1, 1);
    }


    private void InitBuffersCompute()
    {
        PopulateBoids();
        var boidByteSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(BoidData));
        boidBuffer = new ComputeBuffer(instances, boidByteSize);
        boidBuffer.SetData(boidsList.ToArray());

        boidForcesBuffer = new ComputeBuffer(instances, sizeof(float) * 3);
        boidForcesBuffer.SetData(boidsForcesList.ToArray());

        FlockingKernel = flockingShader.FindKernel("FlockingKernel");
        KinematicKernel = flockingShader.FindKernel("KinematicKernel");
    }

    private void InitConstantBuffer()
    {
        flockingShader.SetInt("_Instances", instances);
        flockingShader.SetFloat("_Radius", boidModel.Radius);
        flockingShader.SetFloat("_MaxSpeed", boidModel.MaxSpeed);
        flockingShader.SetFloat("_MaxForce", boidModel.MaxForce);
        flockingShader.SetInt("_MassPerUnit", 1);
        flockingShader.SetVector("_MaxBound", boidBounds.max);
        flockingShader.SetVector("_MinBound", boidBounds.min);
    }

    private void PopulateBoids()
    {
        boidsList.Capacity = instances;
        boidsForcesList.Capacity = instances;
        for (int i = 0; i < instances; i++)
        {
            Vector4 pos = boidBounds.RandomPointInBounds();
            pos.w = 1;
            Vector4 vel = Random.insideUnitSphere.normalized;
            vel.w = Random.Range(1, 10);
            boidsList.Add(new BoidData()
            {
                position = pos,
                velocity = vel,
                scale = (Mathf.Pow(Random.Range(0.8f, 1.0f), 2)) * 0.5f,
                dummy = 1
            });
            boidsForcesList.Add(Vector3.zero);
        }
    }

    private void Dispose()
    {
        boidBuffer.Release();
        boidForcesBuffer.Release();
        // BufferWithArgs.Release();
        // args = null;
        this.boidsList = null;
        this.boidsForcesList = null;
        Destroy(BoidDrawMaterial);
    }

    private void OnDestroy()
    {
        Dispose();
    }

    private void OnDisable()
    {
        Dispose();
    }
}