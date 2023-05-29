using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Vortice.D3DCompiler;
using Vortice.Direct3D11;

namespace ConsoleApp31.Drawing;
internal abstract class Shader : IDisposable
{
    public readonly string fileName;
    public readonly string entryPoint;
    public readonly string compilationProfile;

    public string Source { get; private set; }
    public ReadOnlyMemory<byte> ByteCode { get; private set; }

    public ID3D11Buffer[] ConstantBuffers { get; private set; } = new ID3D11Buffer[8];
    public ID3D11ShaderResourceView[] ResourceViews { get; private set; } = new ID3D11ShaderResourceView[16];
    public ID3D11SamplerState[] SamplerStates { get; private set; } = new ID3D11SamplerState[16];

    public Shader(string fileName, string entryPoint, string compilationProfile)
    {
#if DEBUG
        // if debugging load the original file instead

        var originalFile = Path.Combine("../../../", fileName);
        if (File.Exists(originalFile))
        {
            fileName = originalFile;
        }
#endif

        this.fileName = fileName;
        this.entryPoint = entryPoint;
        this.compilationProfile = compilationProfile;

        Reload();
    }

    protected abstract void CreateShader(ReadOnlyMemory<byte> bytecode);

    public abstract void ApplyTo(ID3D11DeviceContext context);

    [MemberNotNull(nameof(Source), nameof(ByteCode))]
    public void Reload()
    {
        Source = File.ReadAllText(fileName);
        ByteCode = Compiler.Compile(Source, entryPoint, fileName, compilationProfile);

        CreateShader(ByteCode);
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    ~Shader()
    {
        Dispose();
    }
}
