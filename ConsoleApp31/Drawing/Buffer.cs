using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace ConsoleApp31.Drawing;

internal abstract class Buffer<T> : IDisposable
    where T : unmanaged
{
    public readonly BindFlags bindFlags;

    public ID3D11Buffer InternalBuffer { get; private set; }
    public int Length { get; }

    public Buffer(int length, BindFlags bindFlags)
    {
        this.bindFlags = bindFlags;
        this.Length = length;

        if (length == 0)
            length = 16;

        InternalBuffer = Graphics.Device.CreateBuffer(Ceil16(length * Unsafe.SizeOf<T>()), bindFlags);

        static int Ceil16(int x)
        {
            return (x + 15) / 16 * 16;
        }
    }

    public unsafe void Update(ReadOnlySpan<T> data, ID3D11DeviceContext? context = null)
    {
        if (Length is 0 && data.IsEmpty)
            return;

        context ??= Graphics.ImmediateContext;

        fixed (T* dataPtr = data)
        {
            context.UpdateSubresource(new((nint)dataPtr), InternalBuffer);
        }
    }

    public void Dispose()
    {
        InternalBuffer.Dispose();
        GC.SuppressFinalize(this);
    }

    ~Buffer()
    {
        Dispose();
    }
}
