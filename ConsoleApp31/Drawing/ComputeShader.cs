using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace ConsoleApp31.Drawing;
internal class ComputeShader : Shader
{
    public ID3D11ComputeShader Shader { get; private set; }
    public ID3D11UnorderedAccessView[] UnorderedAccessViews { get; private set; } = new ID3D11UnorderedAccessView[8];

    public ComputeShader(string fileName, string entryPoint = "main") : base(fileName, entryPoint, "cs_5_0")
    {
    }

    public override void ApplyTo(ID3D11DeviceContext context)
    {
        context.CSSetShader(Shader);

        context.CSSetShaderResources(0, ResourceViews);
        context.CSSetSamplers(0, SamplerStates);
        context.CSSetConstantBuffers(0, ConstantBuffers);
        context.CSSetUnorderedAccessViews(0, UnorderedAccessViews);
    }

    protected override void CreateShader(ReadOnlyMemory<byte> bytecode)
    {
        Shader?.Dispose();
        Shader = Graphics.Device.CreateComputeShader(bytecode.Span);
    }

    public override void Dispose()
    {
        Shader?.Dispose();
        base.Dispose();
    }
}
