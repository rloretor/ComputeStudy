using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public struct BoidData
{
    public Vector4 position;
    public Vector4 velocity;
}

[Serializable]
public class BoidModel
{
    public Vector3 MinSpeed;
    public Vector3 MaxSpeed;
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

    void Update()
    {
        Dispatch();

        BoidData[] fetchBoids = new BoidData[instances];
        boidBuffer.GetData(fetchBoids);
        for (var index = 1; index < fetchBoids.Length; index++)
        {
            var a = fetchBoids[index];
            Debug.DrawLine(fetchBoids[index - 1].position, fetchBoids[index].position);
        }
    }

    private void Dispatch()
    {
        flockingShader.SetFloat("_DeltaTime", Time.deltaTime);
        flockingShader.SetInt("_Instances", instances);
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


public static class BoundsExtensions
{
    public static Vector3 RandomPointInBounds(this Bounds bounds)
    {
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }
}