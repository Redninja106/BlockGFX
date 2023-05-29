using ConsoleApp31.Drawing;
using Vortice.Direct3D11;

internal class Sampler : IDisposable
{
    public ID3D11SamplerState State { get; private set; }

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