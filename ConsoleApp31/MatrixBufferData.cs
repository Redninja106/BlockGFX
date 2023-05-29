using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31;
internal struct MatrixBufferData
{
    public Matrix4x4 World;
    public Matrix4x4 View;
    public Matrix4x4 Projection;

    public MatrixBufferData(Matrix4x4 world, Matrix4x4 view, Matrix4x4 projection)
    {
        World = Matrix4x4.Transpose(world);
        View = Matrix4x4.Transpose(view);
        Projection = Matrix4x4.Transpose(projection);
    }

    public MatrixBufferData(Matrix4x4 world, Camera camera) : this(world, camera.View.Matrix, camera.Projection.Matrix)
    {
    }
}
