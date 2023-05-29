using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace ConsoleApp31.Drawing;
internal class IndexBuffer : Buffer<uint>
{
    public IndexBuffer(ReadOnlySpan<uint> data) : this(data.Length)
    {
        Update(data);
    }

    public IndexBuffer(int length) : base(length, BindFlags.IndexBuffer)
    {
    }
}
