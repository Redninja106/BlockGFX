using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace ConsoleApp31.Drawing;
internal class TiledVolume : IDisposable
{
    public ID3D11Texture3D1 InternalTexture { get; private set; }

    public ID3D11ShaderResourceView ShaderResourceView { get; private set; }

    public TiledVolume(int width, int height, int depth, Format format)
    {
    }

    public void Dispose()
    {
    }
}
