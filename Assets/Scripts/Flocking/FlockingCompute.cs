using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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
    public float MassPerUnit;
    public float MaxForce;
    public float MaxSpeed;
    [Range(1, 10)] public float Radius;
}

public class FlockingCompute : MonoBehaviour
{
    private const int GroupSize = 512;

    [Header("Compute Buffer stuff")] [SerializeField] [Space]
    private int instances;

    [Space] [SerializeField] private ComputeShader flockingShader;
    [SerializeField] private Bounds boidBounds;
    [SerializeField] private BoidModel boidModel;

    private List<BoidData> boidsList = new List<BoidData>();
    private List<Vector3> boidsForcesList = new List<Vector3>();
    private ComputeBuffer boidBuffer;
    private ComputeBuffer boidForcesBuffer;
    private ComputeBuffer IndirectArgsThreadGroup;
    private int[] IndirectComputeArgs;
    private int FlockingKernel;
    private int KinematicKernel;
    private int ThreadGroupSize;

    [Space, Header("Parametrizable behavior"), Range(1, 10), SerializeField,]
    private float SeparationWeight;

    [SerializeField, Range(1, 10)] private float CohesionWeight;
    [SerializeField, Range(1, 10)] private float AlignWeight;


    [Header("Indirect draw stuff")] [SerializeField] [Space]
    private Mesh BoidMesh;

    [SerializeField] private bool isbillboard = true;
    [SerializeField, Range(0, 5.0f)] private float ParticleSize;
    [SerializeField] private Shader BoidInstanceShader;

    private Material BoidDrawMaterial;

    //private ComputeBuffer BufferWithArgs;
    //private uint[] args = new uint[5] {0, 0, 0, 0, 0};
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Gizmos.DrawWireCube(boidBounds.center, boidBounds.size);
    }

    private void Start()
    {
        ThreadGroupSize = Mathf.CeilToInt((float) instances / GroupSize);
        InitBuffersCompute();
        //  InitBuffersDraw();
        InitMaterial();
        SetBuffers();
    }

    private void InitBuffersCompute()
    {
        PopulateBoids();
        var boidByteSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(BoidData));
        //Debug.Log(boidByteSize);
        boidBuffer = new ComputeBuffer(instances, boidByteSize);
        boidBuffer.SetData(boidsList.ToArray());

        boidForcesBuffer = new ComputeBuffer(instances, sizeof(float) * 3);
        boidForcesBuffer.SetData(boidsForcesList.ToArray());
        IndirectComputeArgs = new[] {ThreadGroupSize, 1, 1};
        IndirectArgsThreadGroup = new ComputeBuffer(1, sizeof(int) * 3, ComputeBufferType.IndirectArguments);
        IndirectArgsThreadGroup.SetData(IndirectComputeArgs);

        FlockingKernel = flockingShader.FindKernel("FlockingKernel");
        KinematicKernel = flockingShader.FindKernel("KinematicKernel");
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

    public void LateUpdate()
    {
        Graphics.DrawMeshInstancedProcedural(BoidMesh, 0, BoidDrawMaterial, boidBounds, instances, null,
            ShadowCastingMode.Off, false);
        //Graphics.DrawMeshInstancedIndirect(BoidMesh, 0, BoidDrawMaterial, boidBounds, BufferWithArgs);
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


    void Update()
    {
        Dispatch();

        //BoidData[] fetchBoids = new BoidData[instances];
        //boidBuffer.GetData(fetchBoids);
        //for (var index = 0; index < fetchBoids.Length; index++)
        //{
        //    var a = fetchBoids[index];
        //    Debug.DrawLine(a.position, a.position + a.velocity.normalized * 0.01f, Color.black);
        //}
    }


    private void Dispatch()
    {
        flockingShader.SetFloat("_DeltaTime", Time.deltaTime);
        flockingShader.SetVector("_ForceWeights", new Vector3(SeparationWeight, CohesionWeight, AlignWeight));

        if (isbillboard)
        {
            BoidDrawMaterial.EnableKeyword("ISBILLBOARD");
            BoidDrawMaterial.SetFloat("_SphereRadius", ParticleSize/2.0f);
        }
        else
        {
            BoidDrawMaterial.DisableKeyword("ISBILLBOARD");
        }
        

        if (Time.frameCount % 2 == 0)
        {
            flockingShader.DispatchIndirect(FlockingKernel, IndirectArgsThreadGroup);
            //flockingShader.Dispatch(FlockingKernel, ThreadGroupSize, 1, 1);
        }

        flockingShader.DispatchIndirect(KinematicKernel, IndirectArgsThreadGroup);
        //flockingShader.Dispatch(KinematicKernel, ThreadGroupSize, 1, 1);
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
        //  Vector3 prev = boidBounds.min;
        for (int i = 0; i < instances; i++)
        {
            Vector3 pos = boidBounds.RandomPointInBounds();
            //Debug.DrawLine(prev, pos, Color.black, 3600);
            //prev = pos;
            Vector3 vel = Random.insideUnitSphere.normalized;
            boidsList.Add(new BoidData()
            {
                position = pos,
                velocity = vel,
                scale = (Mathf.Pow(Random.Range(1, 2.0f), 2)),
                dummy = Random.Range(0.0f, 1.0f)
            });

            boidsForcesList.Add(Vector3.zero);
        }
    }

    private void Dispose()
    {
        boidBuffer?.Release();
        boidForcesBuffer?.Release();
        // BufferWithArgs.Release();
        // args = null;
        IndirectArgsThreadGroup?.Release();
        IndirectComputeArgs = null;
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