using ConsoleApp31.Drawing;
using System.Numerics;
using Vortice.Direct3D11;

internal class Sampler : IDisposable
{
    public ID3D11SamplerState State { get; private set; }

    public Sampler(Filter filter, TextureAddressMode addressMode, Vector4 borderColor = default)
    {
        var desc = new SamplerDescription(filter, addressMode);
        desc.BorderColor = new(borderColor);

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