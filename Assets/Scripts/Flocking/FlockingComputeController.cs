﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class FlockingComputeController
{
    public ComputeBuffer DebugBuffer => debugBuffer;
    public ComputeBuffer ForcesBuffer => boidForcesBuffer;

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

    private List<BoidDebug> boidsForcesDebugList = new List<BoidDebug>();
    private ComputeBuffer debugBuffer;

    private int instances => boidModel.instances;


    public void Init(BoidModel boidData, CommandBuffer command = null)
    {
        boidModel = boidData;
        ThreadGroupSize = Mathf.CeilToInt((float) instances / GroupSize);
        InitForces();
        InitBuffersCompute();
        if (command == null)
        {
            SetComputeShaderBuffers();
        }
        else
        {
            CommandSetComputeShaderBuffers(command);
        }
    }

    private void InitForces()
    {
        boidsForcesList.Capacity = instances;
        for (var i = 0; i < instances; i++)
        {
            boidsForcesList.Add(Vector3.zero);
            boidsForcesDebugList.Add(new BoidDebug()
            {
                FAli = Vector3.zero,
                FRep = Vector3.zero,
                FAtt = Vector3.zero
            });
        }
    }

    private void InitBuffersCompute()
    {
        boidForcesBuffer = new ComputeBuffer(instances, sizeof(float) * 3);
        boidForcesBuffer.SetData(boidsForcesList);

        debugBuffer = new ComputeBuffer(instances, sizeof(float) * 9);
        debugBuffer.SetData(boidsForcesDebugList);


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
        flockingShader.SetBuffer(FlockingKernel, "_BoidsDebug", debugBuffer);

        flockingShader.SetBuffer(KinematicKernel, "_BoidsBuffer", boidModel.BoidBuffer);
        flockingShader.SetBuffer(KinematicKernel, "_BoidsNetForces", boidForcesBuffer);


        InitConstantBuffer();
    }

    private void CommandSetComputeShaderBuffers(CommandBuffer command)
    {
        command.SetComputeBufferParam(flockingShader, FlockingKernel, "_BoidsBuffer", boidModel.BoidBuffer);
        command.SetComputeBufferParam(flockingShader, FlockingKernel, "_BoidsNetForces", boidForcesBuffer);
        command.SetComputeBufferParam(flockingShader, FlockingKernel, "_BoidsDebug", debugBuffer);

        command.SetComputeBufferParam(flockingShader, KinematicKernel, "_BoidsBuffer", boidModel.BoidBuffer);
        command.SetComputeBufferParam(flockingShader, KinematicKernel, "_BoidsNetForces", boidForcesBuffer);


        InitConstantBuffer();
    }


    public void Compute(bool debug)
    {
        Dispatch(debug);
    }

    public void Compute(CommandBuffer command, bool debug)
    {
        CommandDispatch(command, debug);
    }


    private void CommandDispatch(CommandBuffer command, bool debug)
    {
        command.SetComputeFloatParam(flockingShader, "_DeltaTime", Time.deltaTime);
        command.SetComputeFloatParam(flockingShader, "_Time", Time.time);
        command.SetComputeVectorParam(flockingShader, "_ForceWeights", boidModel.GetForceWeights());

        if (debug)
        {
            command.DispatchCompute(flockingShader, KinematicKernel, ThreadGroupSize, 1, 1);
            if (Time.frameCount % 2 == 0)
            {
                command.DispatchCompute(flockingShader, FlockingKernel, ThreadGroupSize, 1, 1);
            }
        }
        else
        {
            command.DispatchCompute(flockingShader, KinematicKernel, IndirectArgsThreadGroup, 0);
            //flockingShader.DispatchIndirect(KinematicKernel, IndirectArgsThreadGroup);
            if (Time.frameCount % 2 == 0)
            {
                command.DispatchCompute(flockingShader, FlockingKernel, IndirectArgsThreadGroup, 0);
            }
        }
    }


    private void Dispatch(bool debug)
    {
        flockingShader.SetFloat("_DeltaTime", Time.deltaTime);
        flockingShader.SetFloat("_Time", Time.time);
        flockingShader.SetVector("_ForceWeights", boidModel.GetForceWeights());

        if (debug)
        {
            flockingShader.Dispatch(KinematicKernel, ThreadGroupSize, 1, 1);
            if (Time.frameCount % 2 == 0)
            {
                flockingShader.Dispatch(FlockingKernel, ThreadGroupSize, 1, 1);
            }
        }
        else
        {
            flockingShader.DispatchIndirect(KinematicKernel, IndirectArgsThreadGroup);
            if (Time.frameCount % 2 == 0)
            {
                flockingShader.DispatchIndirect(FlockingKernel, IndirectArgsThreadGroup);
            }
        }
    }


    public void InitConstantBuffer()
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