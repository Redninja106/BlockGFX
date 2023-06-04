using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace ConsoleApp31.Drawing;
internal class Volume : IDisposable
{
    public ID3D11ShaderResourceView ShaderResourceView { get; private set; }
    public ID3D11Texture3D InternalTexture { get; private set; }

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Depth { get; private set; }
    public Format Format { get; private set; }

    public Volume(int width, int height, int depth, Format format, BindFlags bindFlags = BindFlags.ShaderResource)
    {
        this.Width = width;
        this.Height = height;
        this.Depth = depth;
        this.Format = format;

        InternalTexture = Graphics.Device.CreateTexture3D(format, width, height, depth, bindFlags: bindFlags);

        ShaderResourceView = Graphics.Device.CreateShaderResourceView(InternalTexture, new(InternalTexture, format));
    }

    public void Update<T>(Span<T> data) where T : unmanaged
    {
        int bytes = (Format.GetBitsPerPixel() / 8);
        Graphics.ImmediateContext.UpdateSubresource(data, InternalTexture, 0, Width * bytes, Height * Width * bytes); 
    }

    public virtual void Dispose()
    {
        ShaderResourceView.Dispose();
        InternalTexture.Dispose();
        GC.SuppressFinalize(this);
    }

    ~Volume()
    {
        Dispose();
    }
}
