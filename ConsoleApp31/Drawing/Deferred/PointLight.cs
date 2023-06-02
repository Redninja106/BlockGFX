using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31.Drawing.Deferred;
internal struct PointLight
{
    public Vector3 Position;
    public float Radius;

    public PointLight(Vector3 position, float radius)
    {
        Position = position;
        Radius = radius;
    }
}
