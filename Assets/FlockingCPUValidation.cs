using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class FlockingCPUValidation : MonoBehaviour
{
    [SerializeField] private int instances;
    [SerializeField] private Bounds boidBounds;
    [SerializeField] private BoidModel boidModel;
    [SerializeField] private Mesh boidMesh;
    private List<BoidData> boidsList = new List<BoidData>();

    // Start is called before the first frame update
    void Start()
    {
        PopulateBoids();
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnDrawGizmos()
    {
        if (boidsList.Count > 0)
        {
            float step = 0;

            for (var b = 0; b < boidsList.Count; b++)
            {
                Vector3 FRep = Vector3.zero;
                Vector3 FAtt = Vector3.zero;
                Vector3 FAli = Vector3.zero;
                var boid = boidsList[b];
                float ForceInteractions = instances;
                for (var bn = 0; bn < boidsList.Count; bn++)
                {
                    var neighbour = boidsList[bn];
                    Vector3 sepDir = boid.position - neighbour.position;
                    float sepLength = Mathf.Max(sepDir.magnitude, 0.00000001f);
                    float weight = sepLength / boidModel.Radius;
                    ForceInteractions -= (weight > 1 ? 1 : 0);
                    weight = 1 - Mathf.Min(weight, 1);
                    FRep += (sepDir / sepLength) * weight * boidModel.MaxForce;
                    FAtt += neighbour.position * weight;
                    FAli += neighbour.velocity * weight;
                }

                if (ForceInteractions > 0)
                {
                    FRep /= ForceInteractions;
                    FAtt /= ForceInteractions;
                    FAtt -= boid.position;
                    FAtt = FAtt.normalized * boidModel.MaxForce;
                    FAli = FAli.normalized * boidModel.MaxForce;
                }

                Vector3 FTotal = FAtt + FAli + FRep;
                
                float currentForce = Mathf.Max(FTotal.magnitude, 0.000001f);
                FTotal = FTotal / currentForce * Mathf.Min(currentForce, boidModel.MaxForce);
                Vector3 Acceleration = FTotal / boidModel.massPerUnit;
                Vector3 Velocity = boid.velocity + Acceleration * Time.deltaTime;
                float currentSpeed = Mathf.Max(Velocity.magnitude, 0.00001f);
                Velocity = Velocity / currentSpeed * Mathf.Min(currentSpeed, boidModel.MaxSpeed);
                Vector3 position = boid.position + Velocity * Time.deltaTime;

                Vector3 r = boidBounds.ReflectPointInBounds(position);
                boid.velocity = (r - boid.position).normalized * Mathf.Min(currentSpeed, boidModel.MaxSpeed);
                boid.position = boidBounds.ClampPointToBounds(position);

                if (Input.GetKey(KeyCode.A))
                {
                    boidsList[b] = boid;
                }

                Gizmos.color = Color.black;

                Gizmos.DrawMesh(boidMesh, boidsList[b].position,
                    Quaternion.LookRotation(boidsList[b].velocity.normalized), Vector3.one * .1f);
                Gizmos.color = new Color(0, 1, 0, 0.1f);
                Gizmos.DrawWireSphere(boid.position, boidModel.Radius);
            }

            Gizmos.DrawCube(boidBounds.center, boidBounds.size);
        }
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
}