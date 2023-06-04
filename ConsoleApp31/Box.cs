using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31;
internal struct Box : ICollidable
{
    public Vector3 min;
    public Vector3 max;

    public Vector3 Size => max - min;

    public Box(Vector3 min, Vector3 max)
    {
        this.min = new(MathF.Min(min.X, max.X), MathF.Min(min.Y, max.Y), MathF.Min(min.Z, max.Z));
        this.max = new(MathF.Max(min.X, max.X), MathF.Max(min.Y, max.Y), MathF.Max(min.Z, max.Z));
    }

    public bool Raycast(Ray ray, out RaycastHit hit)
    {
        float t1 = (min.X - ray.origin.X) * ray.inverseDirection.X;
        float t2 = (max.X - ray.origin.X) * ray.inverseDirection.X;
        float t3 = (min.Y - ray.origin.Y) * ray.inverseDirection.Y;
        float t4 = (max.Y - ray.origin.Y) * ray.inverseDirection.Y;
        float t5 = (min.Z - ray.origin.Z) * ray.inverseDirection.Z;
        float t6 = (max.Z - ray.origin.Z) * ray.inverseDirection.Z;

        float tNear = MathF.Max(MathF.Max(MathF.Min(t1, t2), MathF.Min(t3, t4)), MathF.Min(t5, t6));
        float tFar = MathF.Min(MathF.Min(MathF.Max(t1, t2), MathF.Max(t3, t4)), MathF.Max(t5, t6));

        if (tNear <= tFar && tFar > 0 && tNear < ray.length)
        {
            float t;
            Vector3 normal;

            t = tNear < 0 ? tFar : tNear;

            if (t == t1) normal = -Vector3.UnitX;
            else if (t == t2) normal = Vector3.UnitX;
            else if (t == t3) normal = -Vector3.UnitY;
            else if (t == t4) normal = Vector3.UnitY;
            else if (t == t5) normal = -Vector3.UnitZ;
            else if (t == t6) normal = Vector3.UnitZ;
            else normal = Vector3.Zero; // huh?

            hit = new(t, normal, this);
            return true;
        }

        hit = default;
        return false;
    }

    public bool PartialRaycast(Ray ray, out float tNear, out float tFar)
    {
        float t1 = (min.X - ray.origin.X) * ray.inverseDirection.X;
        float t2 = (max.X - ray.origin.X) * ray.inverseDirection.X;
        float t3 = (min.Y - ray.origin.Y) * ray.inverseDirection.Y;
        float t4 = (max.Y - ray.origin.Y) * ray.inverseDirection.Y;
        float t5 = (min.Z - ray.origin.Z) * ray.inverseDirection.Z;
        float t6 = (max.Z - ray.origin.Z) * ray.inverseDirection.Z;

        tNear = MathF.Max(MathF.Max(MathF.Min(t1, t2), MathF.Min(t3, t4)), MathF.Min(t5, t6));
        tFar = MathF.Min(MathF.Min(MathF.Max(t1, t2), MathF.Max(t3, t4)), MathF.Max(t5, t6));

        if (tNear <= tFar && tFar > 0 && tNear < ray.length)
        {
            return true;
        }

        return false;
    }

    public bool Intersect(Box other, out Box overlap)
    {
        if (max.X < other.min.X || min.X > other.max.X ||
            max.Y < other.min.Y || min.Y > other.max.Y ||
            max.Z < other.min.Z || min.Z > other.max.Z)
        {
            overlap = default;
            return false;
        }

        overlap.min.X = MathF.Max(min.X, other.min.X);
        overlap.max.X = MathF.Min(max.X, other.max.X);
        overlap.min.Y = MathF.Max(min.Y, other.min.Y);
        overlap.max.Y = MathF.Min(max.Y, other.max.Y);
        overlap.min.Z = MathF.Max(min.Z, other.min.Z);
        overlap.max.Z = MathF.Min(max.Z, other.max.Z);

        return true;
    }

    public Box Translated(Vector3 translation)
    {
        return new Box { min = this.min + translation, max = this.max + translation };
    }
}
