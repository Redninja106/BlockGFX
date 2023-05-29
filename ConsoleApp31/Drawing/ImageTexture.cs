using StbImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.Win32;

namespace ConsoleApp31.Drawing;
internal class ImageTexture : Texture
{
    public ID3D11ShaderResourceView ShaderResourceView { get; private set; }

    static ImageTexture()
    {
        StbImage.stbi_set_flip_vertically_on_load(0);
    }

    public static ImageTexture FromFile(string file)
    {
        using var fs = new FileStream(file, FileMode.Open);
        var image = ImageResult.FromStream(fs, ColorComponents.RedGreenBlueAlpha);

        var result = new ImageTexture(image.Width, image.Height);
        result.Update(image.Data);
        return result;
    }

    public ImageTexture(int width, int height) : base(width, height, Format.R8G8B8A8_UNorm, BindFlags.ShaderResource)
    {
        ShaderResourceView = Graphics.Device.CreateShaderResourceView(InternalTexture, new()
        {
            ViewDimension = ShaderResourceViewDimension.Texture2D,
            Format = InternalTexture.Description.Format,
            Texture2D = new()
            {
                MipLevels = 1,
                MostDetailedMip = 0,
            }
        });
    }

    public void Update(Span<Color> colors, ID3D11DeviceContext? context = null)
    {
        var bytes = MemoryMarshal.AsBytes(colors);
        Update(bytes, context);
    }

    public unsafe void UpdateRegion(Span<Color> colors, int x, int y, int width, int height)
    {
        fixed (Color* colorsPtr = &colors[0])
        {
            Graphics.ImmediateContext.UpdateSubresource(new MappedSubresource((nint)colorsPtr, width * sizeof(Color), 0), this.InternalTexture, 0, new(x, y, 0, x + width, y + height, 1));
        }
    }

    public override void Dispose()
    {
        ShaderResourceView.Dispose();
        base.Dispose();
    }

    public static ImageTexture FromColors(int width, int height, Color[] colors)
    {
        var result = new ImageTexture(width, height);
        result.Update(colors);
        return result;
    }
}
