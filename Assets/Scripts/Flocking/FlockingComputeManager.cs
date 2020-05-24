using UnityEngine;

public class FlockingComputeManager : MonoBehaviour
{
    [SerializeField] private BoidModel boidModel;

    [SerializeField] private FlockingComputeController flockingCompute;
    [SerializeField] private FlockingDrawController flockingDrawer;

    private void Start()
    {
        boidModel.Init();

        flockingCompute.Init(boidModel);
        flockingDrawer.Init(boidModel);
    }

    private void Update()
    {
        flockingCompute.Compute();
        flockingDrawer.SetShader();
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