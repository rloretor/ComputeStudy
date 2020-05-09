using UnityEngine;

public class TestReflect : MonoBehaviour
{
    public Bounds b;
    public Transform pos;

    private void OnDrawGizmos()
    {
        if (pos != null && b != null)
        {
            Vector3 p = pos.position;
            Gizmos.DrawSphere(b.ReflectPointInBounds(p), 1);
        }

        Gizmos.DrawWireCube(b.center, b.size);

        float max = b.max.x;
        float min = b.min.x;
        float E = max - min;
        float R = (min + BoundsExtensions.aMod((pos.position.x - min), E));
        float t = BoundsExtensions.aMod(Mathf.Ceil((pos.position.x - min) / E), 2);
        float t2 = R * (2 * t - 1) + (max + min) * (1 - t);

        for (float i = -100; i < 100; i += 0.1f)
        {
            max = b.max.x;
            min = b.min.x;
            E = max - min;
            R = (min + BoundsExtensions.aMod((i - min), E));
            t = BoundsExtensions.aMod(Mathf.Ceil((i - min) / E), 2);
            t2 = R * (2 * t - 1) + (max + min) * (1 - t);

            //Gizmos.color = Color.red;
            //Gizmos.DrawSphere(new Vector3(i, 0, t), 0.1f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(new Vector3(i, 0, t2), 0.1f);
            //Gizmos.color = Color.cyan;
            //Gizmos.DrawSphere(new Vector3(i, 0, R), 0.1f);
        }
    }
}