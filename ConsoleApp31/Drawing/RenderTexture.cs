using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace ConsoleApp31.Drawing;
internal class RenderTexture : Texture
{
    public ID3D11RenderTargetView RenderTargetView { get; private set; }
    public ID3D11ShaderResourceView ShaderResourceView { get; private set; }

    public RenderTexture(int width, int height, Format format, BindFlags? bindFlags = null) : base(width, height, format, bindFlags ?? BindFlags.RenderTarget | BindFlags.ShaderResource)
    {
        RenderTargetViewDescription rtvDesc = new(this.InternalTexture, RenderTargetViewDimension.Texture2D, format);
        RenderTargetView = Graphics.Device.CreateRenderTargetView(this.InternalTexture, rtvDesc);

        ShaderResourceViewDescription srvDesc = new(this.InternalTexture, ShaderResourceViewDimension.Texture2D, format);
        ShaderResourceView = Graphics.Device.CreateShaderResourceView(this.InternalTexture, srvDesc);
    }

    public override void Dispose()
    {
        RenderTargetView.Dispose();
        ShaderResourceView.Dispose();
        base.Dispose();
    }
}