using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace ConsoleApp31.Drawing;
internal class DepthStencilTexture : Texture
{
    public ID3D11DepthStencilView DepthStencilView { get; private set; }

    public DepthStencilTexture(int width, int height, Format depthFormat = Format.D24_UNorm_S8_UInt) : base(width, height, depthFormat, BindFlags.DepthStencil)
    {
        DepthStencilViewDescription viewDesc = new(DepthStencilViewDimension.Texture2D, depthFormat);

        DepthStencilView = Graphics.Device.CreateDepthStencilView(this.InternalTexture, viewDesc);
    }

    public override void Dispose()
    {
        DepthStencilView.Dispose();
        base.Dispose();
    }
}
