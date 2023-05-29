using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace ConsoleApp31.Drawing;
internal class ConstantBuffer<T> : Buffer<T>
    where T : unmanaged
{
    public ConstantBuffer(T data) : this()
    {
        Update(new ReadOnlySpan<T>(in data));
    }

    public ConstantBuffer() : base(1, BindFlags.ConstantBuffer)
    {
    }

    public void Update(T data, ID3D11DeviceContext? context = null)
    {
        Update(new ReadOnlySpan<T>(in data), context);
    }
}
