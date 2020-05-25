using System;
using UnityEngine;

public class FlockingComputeManager : MonoBehaviour
{
    [SerializeField] private BoidModel boidModel;

    [SerializeField] private FlockingComputeController flockingCompute;
    [SerializeField] private FlockingDrawController flockingDrawer;

    [SerializeField] private bool debug;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(boidModel.boidBounds.center, boidModel.boidBounds.size);
        if (debug)
        {
            DebugForces();
        }
    }

    private void Start()
    {
        boidModel.Init();

        flockingCompute.Init(boidModel);
        flockingDrawer.Init(boidModel);
    }

    private void Update()
    {
        flockingCompute.Compute(debug);
        flockingDrawer.SetShader();
    }

    private void DebugForces()
    {
        BoidDebug[] debug = new BoidDebug[boidModel.instances];
        flockingCompute.DebugBuffer.GetData(debug);
        BoidData[] boidData = new BoidData[boidModel.instances];
        boidModel.BoidBuffer.GetData(boidData);
        Vector3[] boidNetForce = new Vector3[boidModel.instances];
        flockingCompute.ForcesBuffer.GetData(boidNetForce);
        for (var index = 0; index < debug.Length; index++)
        {
            var debugboid = debug[index];
            var boid = boidData[index];
            var force = boidNetForce[index];

            Debug.DrawLine(boid.position, boid.position + boid.velocity, Color.cyan * 0.8f);
            Debug.DrawLine(boid.position, boid.position + force, Color.magenta * 0.8f);
            Debug.DrawLine(boid.position, boid.position + debugboid.FRep, Color.red * 0.5f);
            Debug.DrawLine(boid.position, boid.position + debugboid.FAli, Color.blue * 0.5f);
            Debug.DrawLine(boid.position, boid.position + debugboid.FAtt, Color.green * 0.5f);
            Debug.DrawLine(boid.position, boid.position + ((debugboid.FAtt + debugboid.FRep + debugboid.FAli)),
                Color.yellow);

            Gizmos.color = new Color(1, 0, 0, 0.05f);
            Gizmos.DrawSphere(boid.position, boidModel.OuterRadius + boid.scale / 2);
            Gizmos.color = new Color(1, 1, 1, 0.1f);
            Gizmos.DrawSphere(boid.position,
                Mathf.Min(boidModel.OuterRadius + boid.scale / 2, boidModel.InnerRadius + boid.scale / 2));
        }
    }

    private void LateUpdate()
    {
        flockingDrawer.Draw();
    }

    private void OnDestroy()
    {
        boidModel.Dispose();
        flockingCompute.Dispose();
    }

    private void OnDisable()
    {
        flockingCompute.Dispose();
    }
}