using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31;
internal struct Transform
{
    public Vector3 Position = Vector3.Zero;
    public Vector3 Scale = Vector3.One;
    public Quaternion Rotation = Quaternion.Identity;

    public Transform()
    {

    }

    public Vector3 Forward => Vector3.Transform(Vector3.UnitZ, Rotation);

    public void Translate(Vector3 translation, bool local = true)
    {
        if (!local)
        {
            translation = Vector3.Transform(translation, Rotation);
        }

        Position += translation;
    }

    public void Rotate(Vector3 axis, float angle)
    {
        Rotate(Quaternion.CreateFromAxisAngle(axis, angle));
    }

    public void Rotate(Matrix4x4 rotationMatrix)
    {
        Rotate(Quaternion.CreateFromRotationMatrix(rotationMatrix));
    }

    public void Rotate(Quaternion quaternion)
    {
        Rotation = Quaternion.Concatenate(Rotation, quaternion);
    }

    public Matrix4x4 GetMatrix()
    {
        return
            Matrix4x4.CreateScale(this.Scale) *
            Matrix4x4.CreateFromQuaternion(this.Rotation) *
            Matrix4x4.CreateTranslation(this.Position);
    }
}
