using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace ConsoleApp31.Drawing;
internal class UnorderedAccessTexture : Texture
{
    public ID3D11UnorderedAccessView UnorderedAccessView { get; private set; }
    public ID3D11ShaderResourceView ShaderResourceView { get; private set; }

    public UnorderedAccessTexture(int width, int height, Format viewFormat, Format? textureFormat = null) : base(width, height, textureFormat ?? viewFormat, BindFlags.UnorderedAccess | BindFlags.ShaderResource)
    {
        UnorderedAccessView = Graphics.Device.CreateUnorderedAccessView(this.InternalTexture, new(this.InternalTexture, UnorderedAccessViewDimension.Texture2D, viewFormat));
        ShaderResourceView = Graphics.Device.CreateShaderResourceView(this.InternalTexture, new(this.InternalTexture, ShaderResourceViewDimension.Texture2D, viewFormat));
    }

    public override void Dispose()
    {
        ShaderResourceView.Dispose();
        UnorderedAccessView.Dispose();
        base.Dispose();
    }
}
