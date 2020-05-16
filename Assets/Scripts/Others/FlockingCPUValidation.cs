using System;
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


    private void OnDrawGizmos()
    {
        if (boidsList.Count > 0)
        {
            float step = 0;

            for (var b = 0; b < boidsList.Count; b++)
            {
                var boid = boidsList[b];
                Vector3 FRep = Vector3.zero;
                Vector3 FAtt = boid.position;
                Vector3 FAli = Vector3.zero;
                float ForceInteractions = 0;
                int notSelf = 0;
                for (var bn = 0; bn < boidsList.Count; bn++)
                {
                    var neighbour = boidsList[bn];
                    Vector3 sepDir = boid.position - neighbour.position;
                    float sepLength = Mathf.Max(sepDir.magnitude, Mathf.Epsilon);
                    float weight = sepLength / boidModel.OuterRadius;
                    notSelf = (weight > Mathf.Epsilon ? 1 : 0);
                    if (weight > Mathf.Epsilon)
                    {
                        Gizmos.color = Color.white * (1 - Mathf.Min(weight, 1));
                        Debug.Log(1 - Mathf.Min(weight, 1));
                        Gizmos.DrawWireSphere(boid.position, boidModel.OuterRadius);
                        Gizmos.DrawWireSphere(neighbour.position, boidModel.OuterRadius);
                    }

                    weight = 1 - Mathf.Min(weight, 1);
                    ForceInteractions += notSelf;
                    FRep += (sepDir / sepLength) * weight * notSelf;
                    FAtt += neighbour.position * Mathf.Ceil(weight) * notSelf;
                    FAli += neighbour.velocity * weight * notSelf;
                }

                Vector3 FTotal = boid.velocity;
                float avg = 1 / Mathf.Max(ForceInteractions, Mathf.Epsilon);
                FRep *= avg;
                //isnan
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(boid.position, boid.position + FRep);
                FAtt /= Mathf.Max(ForceInteractions + 1, Mathf.Epsilon);
                Gizmos.color = new Color(0, 0, 1, 1f);
                Gizmos.DrawLine(boid.position, FAtt);
                FAtt = FAtt - boid.position;
                Debug.Log(FAtt);
                FAli *= avg;
                FAli = FAli.normalized * boidModel.MaxForce;
                FTotal = Vector3.right; //+ FAli + FRep;


                float currentForce = Mathf.Max(FTotal.magnitude, Mathf.Epsilon);
                FTotal = FTotal / currentForce * boidModel.MaxForce;
                Vector3 Acceleration = FTotal / boidModel.MassPerUnit;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(boid.position, boid.position + Acceleration);
                Vector3 Velocity = boid.velocity + Acceleration * Time.deltaTime;
                float currentSpeed = Mathf.Max(Velocity.magnitude, Mathf.Epsilon);
                Velocity = Velocity / currentSpeed * Mathf.Min(currentSpeed, boidModel.MaxSpeed);
                Vector3 position = boid.position + Velocity * Time.deltaTime;

                Vector3 r = boidBounds.ReflectPointInBounds(position);
                boid.velocity = (r - boid.position).normalized * Mathf.Min(currentSpeed, boidModel.MaxSpeed);
                boid.position = boidBounds.ReflectPointInBounds(position);
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(boid.position, boid.position + Velocity);
                if (Input.GetKey(KeyCode.A))
                {
                    boidsList[b] = boid;
                }

                Gizmos.color = Color.black;

                Gizmos.DrawMesh(boidMesh, boidsList[b].position,
                    Quaternion.LookRotation(boidsList[b].velocity.normalized), Vector3.one * .1f);
                Gizmos.color = new Color(0, 1, 0, 0.1f);
                Gizmos.DrawWireSphere(boid.position, boidModel.OuterRadius);
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