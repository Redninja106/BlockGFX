using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D;

namespace ConsoleApp31.Drawing;
internal static class D3DCompiler
{
    [DllImport("d3dcompiler_47.dll", CallingConvention = CallingConvention.StdCall)]
    private unsafe static extern int D3DCompile(void* _srcData, void* _srcDataSize, void* _sourceName, void* _defines, void* _include, void* _entrypoint, void* _target, int _flags1, int _flags2, void* _code, void* _errorMsgs);

    public unsafe static ReadOnlyMemory<byte> Compile(string src, string sourceFile, string entryPoint, string profile, bool useStandardInclude)
    {
        using NativeString srcNative = new(src, false, Encoding.ASCII);
        using NativeString sourceFileNative = new(sourceFile, true, Encoding.ASCII);
        using NativeString entryPointNative = new(entryPoint, true, Encoding.ASCII);
        using NativeString profileNative = new(profile, true, Encoding.ASCII);

        void* code, error;
        Result result;
        nint include = 0;
        try
        {
            // include = Marshal.GetComInterfaceForObject<Shader.IncludeHandler, ID3DInclude>(Shader.IncludeHandler.Instance);
            fixed (byte* srcPtr = srcNative.AsSpan())
            fixed (byte* sourceFilePtr = sourceFileNative.AsSpan())
            fixed (byte* entryPointPtr = entryPointNative.AsSpan())
            fixed (byte* profilePtr = profileNative.AsSpan())
                result = D3DCompile(srcPtr, (void*)srcNative.Length, sourceFilePtr, null, (void*)include, entryPointPtr, profilePtr, 0, 0, &code, &error);
        }
        finally
        {
            if (include is not 0)
                Marshal.Release(include);
        }

        if (result.Failure)
        {
            using var errorBlob = new Blob((nint)error);
            throw new Exception(errorBlob.AsString());
        }

        using var codeBlob = new Blob((nint)code);
        return codeBlob.AsMemory();
    }
}

readonly struct NativeString : IDisposable
{
    private readonly RentedArray<byte> stringData;
    private readonly int length;

    public int Length => length;

    public NativeString(string value, bool nullTerminated, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        length = encoding.GetByteCount(value);

        if (nullTerminated)
            length++;

        stringData = new(length);

        encoding.GetBytes(value, stringData.AsSpan());
        
        if (nullTerminated)
            stringData[length - 1] = 0;
    }

    public void Dispose()
    {
        stringData.Return();
    }

    public Span<byte> AsSpan()
    {
        return stringData.AsSpan()[..length];
    }
}
