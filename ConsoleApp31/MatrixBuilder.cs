using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31;
internal class MatrixBuilder
{
    public Matrix4x4 Matrix { get; private set; }
    public Matrix4x4 InverseMatrix { get; private set; }

    public MatrixBuilder()
    {
        Reset();
    }

    public MatrixBuilder Reset()
    {
        Matrix = Matrix4x4.Identity;
        InverseMatrix = Matrix4x4.Identity;
        return this;
    }

    public MatrixBuilder Multiply(Matrix4x4 matrix)
    {
        if (!Matrix4x4.Invert(matrix, out Matrix4x4 inverse))
            throw new Exception();

        return Multiply(matrix, inverse);
    }

    public MatrixBuilder Multiply(Matrix4x4 matrix, Matrix4x4 inverse)
    {
        Matrix = matrix * Matrix;
        InverseMatrix = inverse * InverseMatrix;
        return this;
    }

    public MatrixBuilder Scale(Vector3 scale)
    {
        return Multiply(Matrix4x4.CreateScale(scale));
    }

    public MatrixBuilder Translate(Vector3 translation)
    {
        return Multiply(Matrix4x4.CreateTranslation(translation));
    }

    public MatrixBuilder RotateX(float radians)
    {
        return Multiply(Matrix4x4.CreateRotationX(radians));
    }

    public MatrixBuilder RotateY(float radians)
    {
        return Multiply(Matrix4x4.CreateRotationX(radians));
    }

    public MatrixBuilder RotateZ(float radians)
    {
        return Multiply(Matrix4x4.CreateRotationX(radians));
    }

    public MatrixBuilder Rotate(Quaternion quaternion)
    {
        return Multiply(Matrix4x4.CreateFromQuaternion(quaternion));
    }

    public MatrixBuilder PerspectiveFieldOfView(float fov, float aspect, float near, float far)
    {
        return Multiply(Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, near, far));
    }

    public MatrixBuilder LookAt(Vector3 position, Vector3 target, Vector3 up)
    {
        return Multiply(Matrix4x4.CreateLookAt(position, target, up));
    }
}
