using ConsoleApp31.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace ConsoleApp31.Extensions;
internal static class DeviceContextExtensions
{
    public static void SetVertexShader(this ID3D11DeviceContext context, VertexShader vertexShader)
    {
        vertexShader.ApplyTo(context);
    }

    public static void SetPixelShader(this ID3D11DeviceContext context, PixelShader? pixelShader)
    {
        if (pixelShader is null)
        {
            context.PSSetShader(null);
        }
        else
        {
            pixelShader.ApplyTo(context);
        }
    }

    public static void SetVertexBuffer<T>(this ID3D11DeviceContext context, VertexBuffer<T> vertexBuffer)
        where T : unmanaged
    {
        context.IASetVertexBuffer(0, vertexBuffer.InternalBuffer, Unsafe.SizeOf<T>());
    }

    public static void SetIndexBuffer(this ID3D11DeviceContext context, IndexBuffer indexBuffer)
    {
        context.IASetIndexBuffer(indexBuffer.InternalBuffer, Format.R32_UInt, 0);
    }

    public static void SetComputeShader(this ID3D11DeviceContext context, ComputeShader computeShader)
    {
        computeShader.ApplyTo(context);
    }
}
