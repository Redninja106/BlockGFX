using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31.Extensions;
internal static class VectorExtensions
{
    public static Vector3 Normalized(this Vector3 vector)
    {
        if (vector.LengthSquared() is 0)
            return Vector3.Zero;

        return Vector3.Normalize(vector);
    }
}
