using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31.Drawing;
/// <summary>
/// A buffer that supports read/write operations in shaders
/// </summary>
internal class StructuredBuffer<T> : Buffer<T> where T : unmanaged
{
    public StructuredBuffer(Span<T> data) : this(data.Length)
    {
        Update(data);
    }

    public StructuredBuffer(int length) : base(length, BindFlags.UnorderedAccess)
    {
    }
}
