using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace ConsoleApp31.Drawing;

internal class Buffer<T> : IDisposable
    where T : unmanaged
{
    public readonly BindFlags bindFlags;

    public ID3D11Buffer InternalBuffer { get; private set; }
    public int Length { get; }

    private protected virtual int RequiredAlignment => 0;

    public Buffer(Span<T> data, BindFlags bindFlags) : this(data.Length, bindFlags)
    {
        Update(data);
    }

    public Buffer(int length, BindFlags bindFlags, ResourceOptionFlags miscFlags = ResourceOptionFlags.None)
    {
        this.bindFlags = bindFlags;
        this.Length = length;

        if (length == 0)
            length = 16;

        InternalBuffer = Graphics.Device.CreateBuffer(Align(length * Unsafe.SizeOf<T>(), RequiredAlignment), bindFlags, miscFlags: miscFlags, structureByteStride: Unsafe.SizeOf<T>());

        static int Align(int x, int alignment)
        {
            if (alignment is 0)
                return x;

            return (x + (alignment-1)) / alignment * alignment;
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

    public virtual void Dispose()
    {
        InternalBuffer.Dispose();
        GC.SuppressFinalize(this);
    }

    ~Buffer()
    {
        Dispose();
    }
}
