using System;
using System.Diagnostics.CodeAnalysis;
using Vortice.Direct3D11;

namespace ConsoleApp31.Drawing;

internal class PixelShader : Shader
{
    public ID3D11PixelShader Shader { get; private set; }

    public PixelShader(string fileName, string entryPoint = "main") : base(fileName, entryPoint, "ps_5_0")
    {

    }

    protected override void CreateShader(ReadOnlyMemory<byte> bytecode)
    {
        Shader?.Dispose();
        Shader = Graphics.Device.CreatePixelShader(bytecode.Span);
    }

    public override void ApplyTo(ID3D11DeviceContext context)
    {
        context.PSSetShader(Shader);

        context.PSSetShaderResources(0, ResourceViews);
        context.PSSetSamplers(0, SamplerStates);
        context.PSSetConstantBuffers(0, ConstantBuffers);
    }

    public override void Dispose()
    {
        Shader?.Dispose();
        base.Dispose();
    }
}