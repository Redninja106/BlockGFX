using ConsoleApp31.Drawing;
using Vortice.Direct3D11;

namespace ConsoleApp31.Drawing;

internal class Sampler : IDisposable
{
    public static Sampler PointWrap { get; private set; }


    public ID3D11SamplerState State { get; private set; }

    public static void InitalizeSharedSamplers()
    {
        PointWrap = new(Filter.MinMagMipPoint, TextureAddressMode.Wrap);
    }

    public Sampler(Filter filter, TextureAddressMode addressMode)
    {
        var desc = new SamplerDescription(filter, addressMode);

        State = Graphics.Device.CreateSamplerState(desc);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        State.Dispose();
    }

    ~Sampler()
    {
        Dispose();
    }
}