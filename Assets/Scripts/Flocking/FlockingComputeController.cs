using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FlockingComputeController
{
    private const int GroupSize = 512;
    private ComputeBuffer boidForcesBuffer;

    private BoidModel boidModel;

    private List<Vector3> boidsForcesList = new List<Vector3>();
    private int FlockingKernel;

    [Header("Flocking Compute shader")] [Space] [SerializeField]
    private ComputeShader flockingShader;

    private ComputeBuffer IndirectArgsThreadGroup;
    private int[] IndirectComputeArgs;
    private int KinematicKernel;
    private int ThreadGroupSize;


    private int instances => boidModel.instances;


    public void Init(BoidModel boidData)
    {
        boidModel = boidData;
        ThreadGroupSize = Mathf.CeilToInt((float) instances / GroupSize);
        InitForces();
        InitBuffersCompute();
        SetComputeShaderBuffers();
    }

    private void InitForces()
    {
        boidsForcesList.Capacity = instances;
        for (var i = 0; i < instances; i++) boidsForcesList.Add(Vector3.zero);
    }

    private void InitBuffersCompute()
    {
        boidForcesBuffer = new ComputeBuffer(instances, sizeof(float) * 3);
        boidForcesBuffer.SetData(boidsForcesList);


        IndirectComputeArgs = new[] {ThreadGroupSize, 1, 1};
        IndirectArgsThreadGroup = new ComputeBuffer(1, sizeof(int) * 3, ComputeBufferType.IndirectArguments);
        IndirectArgsThreadGroup.SetData(IndirectComputeArgs);

        FlockingKernel = flockingShader.FindKernel("FlockingKernel");
        KinematicKernel = flockingShader.FindKernel("KinematicKernel");
    }


    private void SetComputeShaderBuffers()
    {
        flockingShader.SetBuffer(FlockingKernel, "_BoidsBuffer", boidModel.BoidBuffer);
        flockingShader.SetBuffer(FlockingKernel, "_BoidsNetForces", boidForcesBuffer);

        flockingShader.SetBuffer(KinematicKernel, "_BoidsBuffer", boidModel.BoidBuffer);
        flockingShader.SetBuffer(KinematicKernel, "_BoidsNetForces", boidForcesBuffer);


        InitConstantBuffer();
    }


    public void Compute()
    {
        Dispatch();
#if UNITY_EDITOR
        InitConstantBuffer();

#endif
    }


    private void Dispatch()
    {
        flockingShader.SetFloat("_DeltaTime", Time.deltaTime);
        flockingShader.SetFloat("_Time", Time.time);
        flockingShader.SetVector("_ForceWeights", boidModel.GetForceWeights());

        flockingShader.DispatchIndirect(KinematicKernel, IndirectArgsThreadGroup);
        // flockingShader.Dispatch(KinematicKernel, ThreadGroupSize, 1, 1);
        if (Time.frameCount % 2 == 0)
            flockingShader.DispatchIndirect(FlockingKernel, IndirectArgsThreadGroup);
        //flockingShader.Dispatch(FlockingKernel, ThreadGroupSize, 1, 1);
    }


    private void InitConstantBuffer()
    {
        flockingShader.SetInt("_Instances", instances);
        flockingShader.SetInt("_MassPerUnit", boidModel.MassPerUnit);
        flockingShader.SetFloat("_OuterRadius", boidModel.OuterRadius);
        flockingShader.SetFloat("_InnerRadius", boidModel.InnerRadius);
        flockingShader.SetFloat("_MaxSpeed", boidModel.MaxSpeed);
        flockingShader.SetFloat("_MaxForce", boidModel.MaxForce);
        flockingShader.SetVector("_MaxBound", boidModel.boidBounds.max);
        flockingShader.SetVector("_MinBound", boidModel.boidBounds.min);
    }


    public void Dispose()
    {
        boidForcesBuffer?.Release();
        IndirectArgsThreadGroup?.Release();

        IndirectComputeArgs = null;
        boidsForcesList = null;
    }
}