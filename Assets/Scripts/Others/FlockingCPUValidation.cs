using System.Collections.Generic;
using UnityEngine;

public class FlockingCPUValidation : MonoBehaviour
{
    [SerializeField] private Bounds boidBounds;
    [SerializeField] private Mesh boidMesh;
    [SerializeField] private BoidModel boidModel;
    private readonly List<BoidData> boidsList = new List<BoidData>();
    [SerializeField] private int instances;

    // Start is called before the first frame update
    private void Start()
    {
        PopulateBoids();
    }


    private void OnDrawGizmos()
    {
        if (boidsList.Count > 0)
        {
            for (var b = 0; b < boidsList.Count; b++)
            {
                var boid = boidsList[b];
                var FRep = Vector3.zero;
                var FAtt = boid.position;
                var FAli = Vector3.zero;
                float ForceInteractions = 0;
                var notSelf = 0;
                for (var bn = 0; bn < boidsList.Count; bn++)
                {
                    var neighbour = boidsList[bn];
                    var sepDir = boid.position - neighbour.position;
                    var sepLength = Mathf.Max(sepDir.magnitude, Mathf.Epsilon);
                    var weight = sepLength / boidModel.OuterRadius;
                    notSelf = weight > Mathf.Epsilon ? 1 : 0;
                    if (weight > Mathf.Epsilon)
                    {
                        Gizmos.color = Color.white * (1 - Mathf.Min(weight, 1));
                        Debug.Log(1 - Mathf.Min(weight, 1));
                        Gizmos.DrawWireSphere(boid.position, boidModel.OuterRadius);
                        Gizmos.DrawWireSphere(neighbour.position, boidModel.OuterRadius);
                    }

                    weight = 1 - Mathf.Min(weight, 1);
                    ForceInteractions += notSelf;
                    FRep += sepDir / sepLength * weight * notSelf;
                    FAtt += neighbour.position * Mathf.Ceil(weight) * notSelf;
                    FAli += neighbour.velocity * weight * notSelf;
                }

                var FTotal = boid.velocity;
                var avg = 1 / Mathf.Max(ForceInteractions, Mathf.Epsilon);
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


                var currentForce = Mathf.Max(FTotal.magnitude, Mathf.Epsilon);
                FTotal = FTotal / currentForce * boidModel.MaxForce;
                var Acceleration = FTotal / boidModel.MassPerUnit;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(boid.position, boid.position + Acceleration);
                var Velocity = boid.velocity + Acceleration * Time.deltaTime;
                var currentSpeed = Mathf.Max(Velocity.magnitude, Mathf.Epsilon);
                Velocity = Velocity / currentSpeed * Mathf.Min(currentSpeed, boidModel.MaxSpeed);
                var position = boid.position + Velocity * Time.deltaTime;

                var r = boidBounds.ReflectPointInBounds(position);
                boid.velocity = (r - boid.position).normalized * Mathf.Min(currentSpeed, boidModel.MaxSpeed);
                boid.position = boidBounds.ReflectPointInBounds(position);
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(boid.position, boid.position + Velocity);
                if (Input.GetKey(KeyCode.A)) boidsList[b] = boid;

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
        for (var i = 0; i < instances; i++)
        {
            Vector4 pos = boidBounds.RandomPointInBounds();
            pos.w = 1;
            Vector4 vel = Random.insideUnitSphere.normalized;
            vel.w = Random.Range(1, 10);
            boidsList.Add(new BoidData
            {
                position = pos,
                velocity = vel
            });
        }
    }
}