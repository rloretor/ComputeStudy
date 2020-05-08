using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public struct BoidData
{
    public Vector3 position;
    public Vector3 velocity;
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
    private const int GroupSize = 64;
    [SerializeField] private int instances;
    [SerializeField] private ComputeShader flockingShader;
    [SerializeField] private Bounds boidBounds;
    [SerializeField] private BoidModel boidModel;
    private List<BoidData> boidsList = new List<BoidData>();
    private int kernel;
    private ComputeBuffer boidBuffer;

    private void Start()
    {
        InitBuffers();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.2f);
        Gizmos.DrawCube(boidBounds.center, boidBounds.size);
    }

    void Update()
    {
        Dispatch();

        BoidData[] fetchBoids = new BoidData[instances];
        boidBuffer.GetData(fetchBoids);
        Debug.Log(fetchBoids[0].position);
        for (var index = 0; index < fetchBoids.Length; index++)
        {
            var a = fetchBoids[index];
            Debug.DrawLine(a.position, a.position + a.velocity.normalized, Color.black);
        }
    }

    private void Dispatch()
    {
        flockingShader.SetFloat("_DeltaTime", Time.deltaTime);
        int TG = Mathf.CeilToInt((float) instances / GroupSize);
        flockingShader.Dispatch(kernel, TG, 1, 1);
    }


    private void InitBuffers()
    {
        PopulateBoids();
        var boidByteSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(BoidData));
        Debug.Log(boidByteSize);
        boidBuffer = new ComputeBuffer(instances, boidByteSize);
        boidBuffer.SetData(boidsList.ToArray());

        kernel = flockingShader.FindKernel("FlockingKernel");
        flockingShader.SetBuffer(kernel, "_BoidsBuffer", boidBuffer);
        InitConstantBuffer();
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
        for (int i = 0; i < instances; i++)
        {
            Vector4 pos = boidBounds.RandomPointInBounds();
            pos.w = 1;
            Vector4 vel = Random.insideUnitSphere.normalized;
            vel.w = Random.Range(1, 10);
            boidsList.Add(new BoidData()
            {
                position = pos,
                velocity = vel
            });
        }
    }

    private void Dispose()
    {
        boidBuffer.Dispose();
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