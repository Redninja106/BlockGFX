using StbImageSharp;
using System.IO;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace ConsoleApp31.Drawing;

abstract class Texture : IDisposable
{
    public ID3D11Texture2D InternalTexture { get; private set; }

    public int Width { get; private set; }
    public int Height { get; private set; }
    public Format Format { get; private set; }

    public Texture(int width, int height, Format format, BindFlags bindFlags)
    {
        Width = width;
        Height = height;
        Format = format;

        Texture2DDescription desc = new(format, width, height, bindFlags: bindFlags);

        InternalTexture = Graphics.Device.CreateTexture2D(in desc);
    }

    public void Update(Span<byte> data, ID3D11DeviceContext? context = null)
    {
        if (data.Length != Width * Height * (Format.GetBitsPerPixel() / 8))
            throw new InvalidOperationException();

        context ??= Graphics.ImmediateContext;

        context.UpdateSubresource(data, InternalTexture, rowPitch: Width * (Format.GetBitsPerPixel() / 8));
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);

        InternalTexture.Dispose();
    }

    ~Texture()
    {
        Dispose();
    }
}