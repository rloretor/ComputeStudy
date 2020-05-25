using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class BoidModel
{
    public Bounds boidBounds;

    private List<BoidData> boidsList = new List<BoidData>();

    public int instances;
    public int MassPerUnit;
    public float MaxForce;
    public float MaxSpeed;

    [Space] [Header("Parametrizable behavior")] [Range(0.1f, 3.5f)] [SerializeField]
    private float SeparationWeight = 1;

    [SerializeField] [Range(0.1f, 3.5f)] private float CohesionWeight = 1;
    [SerializeField] [Range(0.1f, 3.5f)] private float AlignWeight = 1;
    [Space, Range(0, 10)] public float InnerRadius;
    [Range(1, 10)] public float OuterRadius;

    public ComputeBuffer BoidBuffer { get; private set; }

    public void Init()
    {
        PopulateBoids();
        CreateComputeBuffer();
    }

    private void CreateComputeBuffer()
    {
        var boidByteSize = Marshal.SizeOf(typeof(BoidData));
        BoidBuffer = new ComputeBuffer(instances, boidByteSize);
        BoidBuffer.SetData(boidsList);
    }

    private void PopulateBoids()
    {
        boidsList.Capacity = instances;
        for (var i = 0; i < instances; i++)
        {
            var pos = boidBounds.RandomPointInBounds() / 2;
            var vel = Random.insideUnitSphere.normalized * Random.Range(1, 10);
            boidsList.Add(new BoidData
            {
                position = pos,
                velocity = vel,
                scale = Mathf.Pow(Random.Range(0.5f, 2.0f), 2),
                dummy = Random.Range(0.0f, 1.0f)
            });
        }
    }

    public void Dispose()
    {
        BoidBuffer?.Release();
        boidsList = null;
    }

    public Vector4 GetForceWeights()
    {
        return new Vector3(SeparationWeight, CohesionWeight, AlignWeight);
    }
}