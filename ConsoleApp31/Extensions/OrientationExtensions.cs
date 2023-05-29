using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31.Extensions;
public static class OrientationExtensions
{
    public static Vector3 GetNormal(this Orientation orientation)
    {
        return orientation switch
        {
            Orientation.Top => Vector3.UnitY,
            Orientation.Bottom => -Vector3.UnitY,
            Orientation.Right => Vector3.UnitX,
            Orientation.Left => -Vector3.UnitX,
            Orientation.Forward => Vector3.UnitZ,
            Orientation.Backward => -Vector3.UnitZ,
            _ => throw new Exception(),
        };
    }
}
