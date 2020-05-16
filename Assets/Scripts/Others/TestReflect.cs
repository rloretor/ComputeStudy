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
            Vector3 d = (Vector3.right + Vector3.forward) / 2;
            Gizmos.DrawSphere(p, 0.1f);
            Gizmos.DrawLine(p, p + d * 0.1f);

            for (int i = 2; i < 4; i++)
            {
                Vector3 rp = b.ReflectPointInBounds(p);
                Vector3 pp = b.ReflectPointInBounds(p + d * i * 0.1f);
                Vector3 np = p + d * i * 0.1f;

                int pinBounds = b.isPointInBounds(p) ? 1 : 0;
                float a = (0.5f + (i / 10.0f) * 0.5f);
                Vector3 nd = (-b.ClampPointToBounds(p) + pp).normalized;
                d = Vector3.Lerp(nd, d, pinBounds);

                Gizmos.color = a * (pinBounds > 0 ? Color.cyan : Color.magenta);
                p = rp + d * 0.2f;
                Gizmos.DrawSphere(p, 0.1f);
                Gizmos.DrawLine(p, p + d * i * 0.1f);
            }
        }

        Gizmos.DrawWireCube(b.center, b.size);

        // float max = b.max.x;
        // float min = b.min.x;
        // float E = max - min;
        // float R = (min + BoundsExtensions.aMod((pos.position.x - min), E));
        // float t = BoundsExtensions.aMod(Mathf.Ceil((pos.position.x - min) / E), 2);
        // float t2 = R * (2 * t - 1) + (max + min) * (1 - t);
//
        // for (float i = -100; i < 100; i += 0.1f)
        // {
        //     max = b.max.x;
        //     min = b.min.x;
        //     E = max - min;
        //     R = (min + BoundsExtensions.aMod((i - min), E));
        //     t = BoundsExtensions.aMod(Mathf.Ceil((i - min) / E), 2);
        //     t2 = R * (2 * t - 1) + (max + min) * (1 - t);
//
        //     //Gizmos.color = Color.red;
        //     //Gizmos.DrawSphere(new Vector3(i, 0, t), 0.1f);
        //     Gizmos.color = Color.green;
        //     Gizmos.DrawSphere(new Vector3(i, 0, t2), 0.1f);
        //     //Gizmos.color = Color.cyan;
        //     //Gizmos.DrawSphere(new Vector3(i, 0, R), 0.1f);
        // }
    }
}