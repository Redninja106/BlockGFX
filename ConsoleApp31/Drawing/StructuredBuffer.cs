using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace ConsoleApp31.Drawing;
internal class StructuredBuffer<T> : Buffer<T> where T : unmanaged
{
    public ID3D11ShaderResourceView ShaderResourceView { get; private set; }

    public StructuredBuffer(Span<T> data) : this(data.Length)
    {
        Update(data);
    }

    public StructuredBuffer(int length) : base(length, BindFlags.ShaderResource, ResourceOptionFlags.BufferStructured)
    {
        ShaderResourceViewDescription uavDesc = new(this.InternalBuffer, Vortice.DXGI.Format.Unknown, 0, length);

        ShaderResourceView = Graphics.Device.CreateShaderResourceView(this.InternalBuffer, uavDesc);
    }

    public override void Dispose()
    {
        ShaderResourceView?.Dispose();
        base.Dispose();
    }
}
