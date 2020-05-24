using UnityEngine;

public static class BoundsExtensions
{
    public static Vector3 RandomPointInBounds(this Bounds bounds)
    {
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z));
    }

    public static Vector3 ClampPointToBounds(this Bounds bounds, Vector3 p)
    {
        var x = Mathf.Clamp(p.x, bounds.min.x, bounds.max.x);
        var y = Mathf.Clamp(p.y, bounds.min.y, bounds.max.y);
        var z = Mathf.Clamp(p.z, bounds.min.z, bounds.max.z);
        return new Vector3(x, y, z);
    }

    public static bool isPointInBounds(this Bounds bounds, Vector3 p)
    {
        var xInside = p.x >= bounds.min.x && p.x <= bounds.max.x;
        var yInside = p.y >= bounds.min.y && p.y <= bounds.max.y;
        var zInside = p.z >= bounds.min.z && p.z <= bounds.max.z;
        return xInside && yInside && zInside;
    }

    public static Vector3 ReflectPointInBounds(this Bounds bounds, Vector3 p)
    {
        var x = Reflect(p.x, bounds.min.x, bounds.max.x);
        var y = Reflect(p.y, bounds.min.y, bounds.max.y);
        var z = Reflect(p.z, bounds.min.z, bounds.max.z);


        return new Vector3(x, y, z);
    }

    private static float Reflect(float val, float min, float max)
    {
        var E = max - min;
        var R = min + aMod(val - min, E);
        var t = aMod(Mathf.Ceil((val - min) / E), 2);
        return R * (2 * t - 1) + (max + min) * (1 - t);
    }

    public static float aMod(float a, float n)
    {
        return a - n * Mathf.Floor(a / n);
    }
}