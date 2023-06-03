using SharpGen.Runtime;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        // prepend "Shaders/" if it's missing
        if (!fileName.StartsWith("Shaders"))
        {
            fileName = Path.Combine("Shaders", fileName);
        }

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
        Source = LoadSourceFile(fileName);

        ByteCode = Compiler.Compile(Source, entryPoint, fileName, compilationProfile, ShaderFlags.Debug);

        CreateShader(ByteCode);
    }

    private string LoadSourceFile(string file)
    {
        var source = File.ReadAllText(file);

        source = ReplaceIncludes(source, file);

        return source;
    }

    private string ReplaceIncludes(string source, string fileName)
    {
        fileName = Path.GetFullPath(fileName);
        var dir = Path.GetDirectoryName(fileName)!;

        int index = source.IndexOf("#include");

        while (index != -1)
        {
            var firstQuote = index + source[index..].IndexOf('"');
            var includedFileBegin = firstQuote + 1;
            var secondQuote = includedFileBegin + source[includedFileBegin..].IndexOf('"');

            var included = source[includedFileBegin..secondQuote];
            
            var fullIncluded = Path.Combine(dir, included);

            if (fileName == fullIncluded)
                throw new("Recursive Include Alert!");

            if (!File.Exists(fullIncluded))
                throw new($"Included file {included} doesnt exist!");

            var includedSource = LoadSourceFile(fullIncluded);

            source = string.Concat(source[..index], includedSource, source[(secondQuote+1)..]);

            index = source.IndexOf("#include");
        }

        return source;
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    ~Shader()
    {
        Dispose();
    }

    public class IncludeHandler : Include
    {
        public static readonly IncludeHandler Instance = new();

        private IncludeHandler()
        {
        }

        public unsafe void Close(Stream stream)
        {
            stream.Dispose();
        }

        public void Dispose()
        {

        }

        public Stream Open(IncludeType type, string fileName, Stream? parentStream)
        {
            return new FileStream(fileName, FileMode.Open);
        }
    }
}