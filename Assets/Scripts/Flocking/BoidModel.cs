using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class BoidModel
{
    [SerializeField] [Range(0.1f, 3.5f)] private float AlignWeight = 1;

    public Bounds boidBounds;

    private List<BoidData> boidsList = new List<BoidData>();

    [SerializeField] [Range(0.1f, 3.5f)] private float CohesionWeight = 1;
    [Range(0, 10)] public float InnerRadius;

    public int instances;
    public int MassPerUnit;
    public float MaxForce;
    public float MaxSpeed;
    [Range(1, 10)] public float OuterRadius;

    [Space] [Header("Parametrizable behavior")] [Range(0.1f, 3.5f)] [SerializeField]
    private float SeparationWeight = 1;

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
            var pos = boidBounds.RandomPointInBounds();
            var vel = Random.insideUnitSphere.normalized;
            boidsList.Add(new BoidData
            {
                position = pos,
                velocity = vel,
                scale = Mathf.Pow(Random.Range(1, 2.0f), 2),
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