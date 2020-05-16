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

public struct DebugData
{
    public Vector3 FAtt;
    public Vector3 FRep;
    public Vector3 FAli;
}

[Serializable]
public class BoidModel
{
    public int MassPerUnit;
    public float MaxForce;
    public float MaxSpeed;
    [Range(1, 10)] public float OuterRadius;
    [Range(0, 10)] public float InnerRadius;
}

public class FlockingCompute : MonoBehaviour
{
    public bool DEBUG;
    private const int GroupSize = 512;

    [Header("Compute Buffer stuff")] [SerializeField] [Space]
    private int instances;

    [Space] [SerializeField] private ComputeShader flockingShader;
    [SerializeField] private Bounds boidBounds;
    [SerializeField] private BoidModel boidModel;

    private List<BoidData> boidsList = new List<BoidData>();
    private List<DebugData> boidsDebugData = new List<DebugData>();
    private List<Vector3> boidsForcesList = new List<Vector3>();
    private ComputeBuffer boidBuffer;
    private ComputeBuffer boidForcesBuffer;
    private ComputeBuffer IndirectArgsThreadGroup;
    private ComputeBuffer DebugDataBuffer;
    private int[] IndirectComputeArgs;
    private int FlockingKernel;
    private int KinematicKernel;
    private int ThreadGroupSize;

    [Space, Header("Parametrizable behavior"), Range(0.1f, 3.5f), SerializeField,]
    private float SeparationWeight = 1;

    [SerializeField, Range(0.1f, 3.5f)] private float CohesionWeight = 1;
    [SerializeField, Range(0.1f, 3.5f)] private float AlignWeight = 1;


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
        PopulateBoids();
        InitBuffersCompute();
        //  InitBuffersDraw();
        InitMaterial();
        SetBuffers();
    }

    private void InitBuffersCompute()
    {
        var boidByteSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(BoidData));
        boidBuffer = new ComputeBuffer(instances, boidByteSize);
        boidBuffer.SetData(boidsList);

        boidForcesBuffer = new ComputeBuffer(instances, sizeof(float) * 3);
        boidForcesBuffer.SetData(boidsForcesList);

        if (DEBUG)
        {
            DebugDataBuffer =
                new ComputeBuffer(instances, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DebugData)));
            DebugDataBuffer.SetData(boidsDebugData);
        }

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
        if (DEBUG)
        {
            flockingShader.SetBuffer(FlockingKernel, "_BoidsDebug", DebugDataBuffer);
            flockingShader.SetBuffer(KinematicKernel, "_BoidsDebug", DebugDataBuffer);
        }

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
#if UNITY_EDITOR
        InitConstantBuffer();

        if (DEBUG)
        {
            BoidData[] fetchBoids = new BoidData[instances];
            Vector3[] fetchForces = new Vector3[instances];
            DebugData[] fetchDebugForces = new DebugData[instances];
            boidBuffer.GetData(fetchBoids);
            boidForcesBuffer.GetData(fetchForces);
            DebugDataBuffer.GetData(fetchDebugForces);
            for (var index = 0; index < instances; index++)
            {
                var a = fetchBoids[index];
                var f = fetchDebugForces[index];
                Debug.DrawLine(a.position, a.position + a.velocity, Color.black);
                Debug.DrawLine(a.position, a.position + fetchForces[index], Color.white);
                Debug.DrawLine(a.position, a.position + f.FRep, Color.red);
                Debug.DrawLine(a.position, a.position + f.FAtt, Color.green);
                Debug.DrawLine(a.position, a.position + f.FAli, Color.blue);
            }
        }
#endif
    }


    private void Dispatch()
    {
        flockingShader.SetFloat("_DeltaTime", Time.deltaTime);
        flockingShader.SetFloat("_Time", Time.time);
        flockingShader.SetVector("_ForceWeights", new Vector3(SeparationWeight, CohesionWeight, AlignWeight));
        BoidDrawMaterial.SetFloat("_SphereRadius", ParticleSize / 2.0f);
        if (isbillboard)
        {
            BoidDrawMaterial.EnableKeyword("ISBILLBOARD");
        }

        else
        {
            BoidDrawMaterial.DisableKeyword("ISBILLBOARD");
        }

        flockingShader.DispatchIndirect(KinematicKernel, IndirectArgsThreadGroup);
// flockingShader.Dispatch(KinematicKernel, ThreadGroupSize, 1, 1);
        if (Time.frameCount % 2 == 0)
        {
            //flockingShader.DispatchIndirect(FlockingKernel, IndirectArgsThreadGroup);
            flockingShader.Dispatch(FlockingKernel, ThreadGroupSize, 1, 1);
        }
    }


    private void InitConstantBuffer()
    {
        flockingShader.SetInt("_Instances", instances);
        BoidDrawMaterial.SetInt("_Instances", instances);
        flockingShader.SetInt("_MassPerUnit", boidModel.MassPerUnit);
        flockingShader.SetFloat("_OuterRadius", boidModel.OuterRadius);
        flockingShader.SetFloat("_InnerRadius", boidModel.InnerRadius);
        flockingShader.SetFloat("_MaxSpeed", boidModel.MaxSpeed);
        flockingShader.SetFloat("_MaxForce", boidModel.MaxForce);
        flockingShader.SetVector("_MaxBound", boidBounds.max);
        flockingShader.SetVector("_MinBound", boidBounds.min);
    }

    private void PopulateBoids()
    {
        boidsList.Capacity = instances;
        boidsForcesList.Capacity = instances;
        boidsDebugData.Capacity = instances;
//  Vector3 prev = boidBounds.min;
        for (int i = 0;
            i < instances;
            i++)
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
            if (DEBUG)
            {
                boidsDebugData.Add(new DebugData()
                {
                    FAli = Vector3.zero,
                    FRep = Vector3.zero,
                    FAtt = Vector3.zero
                });
            }

            boidsForcesList.Add(Vector3.zero);
        }
    }

    private void Dispose()
    {
        boidBuffer?.Release();
        boidForcesBuffer?.Release();
        DebugDataBuffer?.Release();

// BufferWithArgs.Release();
// args = null;
        IndirectArgsThreadGroup?.Release();
        IndirectComputeArgs = null;
        this.boidsList = null;
        this.boidsForcesList = null;
        this.boidsDebugData = null;
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