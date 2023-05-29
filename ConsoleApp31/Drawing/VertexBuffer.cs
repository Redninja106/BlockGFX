using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace ConsoleApp31.Drawing;
internal class VertexBuffer<T> : Buffer<T>
    where T : unmanaged
{
    public VertexBuffer(ReadOnlySpan<T> data) : this(data.Length)
    {
        Update(data);
    }

    public VertexBuffer(int length) : base(length, BindFlags.VertexBuffer)
    {
    }
}
