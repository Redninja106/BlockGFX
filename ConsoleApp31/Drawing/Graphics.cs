using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Debug;
using Vortice.DXGI;

namespace ConsoleApp31.Drawing;
internal static class Graphics
{
    public static ID3D11Device5 Device { get; set; } = null!;
    public static ID3D11DeviceContext4 ImmediateContext { get; set; } = null!;
    public static IDXGISwapChain1 SwapChain { get; set; } = null!;
    public static ID3D11RenderTargetView RenderTargetView { get; set; }
    public static ID3D11Debug Debug { get; set; }
    public static Format BackBufferFormat { get; } = Format.R8G8B8A8_UNorm;

    public static int SwapInterval { get; set; }

    public static event Action BeforeResize;
    public static event Action AfterResize;

    public static int RenderTargetWidth { get; set; }
    public static int RenderTargetHeight { get; set; }

    public static void Initialize()
    {
        var featureLevels = new[]
        {
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0,
            FeatureLevel.Level_10_1,
            FeatureLevel.Level_10_0,
            FeatureLevel.Level_9_3,
            FeatureLevel.Level_9_2,
            FeatureLevel.Level_9_1,
        };

        Result hr = D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.Debug, featureLevels, out ID3D11Device? device0);

        if (hr.Failure)
            throw new Exception();

        // Debug = device0!.QueryInterface<ID3D11Debug>();
        Device = device0!.QueryInterface<ID3D11Device5>();

        // device0.Dispose();

        ImmediateContext = Device!.ImmediateContext3.QueryInterface<ID3D11DeviceContext4>();
    }

    public static void CreateSwapChain(nint hwnd, int width, int height)
    {
        using var dxgiDevice = Device.QueryInterface<IDXGIDevice>();

        using var adapter = dxgiDevice.GetAdapter();

        using var factory = adapter.GetParent<IDXGIFactory2>();

        SwapChainDescription1 swapChainDesc = new()
        {
            Format = BackBufferFormat,
            SampleDescription = new(1, 0),
            AlphaMode = AlphaMode.Ignore,
            BufferCount = 2,
            BufferUsage = Usage.RenderTargetOutput,
            Flags = SwapChainFlags.None,
            Height = height,
            Width = width,
            Scaling = Scaling.None,
            Stereo = false,
            SwapEffect = SwapEffect.FlipSequential,
        };

        SwapChain = factory.CreateSwapChainForHwnd(Device, hwnd, swapChainDesc);

        RenderTargetWidth = width;
        RenderTargetHeight = height;

        CreateRenderTargetView();
    }

    public static void Present()
    {
        var hr = SwapChain.Present(SwapInterval, PresentFlags.None, new());
        hr.CheckError();
    }

    public static void Resize(int width, int height)
    {
        if (width == 0 || height == 0)
            return;

        BeforeResize?.Invoke();
        RenderTargetView.Dispose();

        SwapChain.ResizeBuffers(2, width, height);

        RenderTargetWidth = width;
        RenderTargetHeight = height;

        CreateRenderTargetView();
        AfterResize?.Invoke();
    }

    private static void CreateRenderTargetView()
    {
        using var backBuffer = SwapChain.GetBuffer<ID3D11Texture2D>(0);

        var desc = new RenderTargetViewDescription(backBuffer, RenderTargetViewDimension.Texture2D, backBuffer.Description.Format);

        RenderTargetView = Device.CreateRenderTargetView(backBuffer, desc);
    }

    public static void Destroy()
    {
        RenderTargetView?.Dispose();
        SwapChain?.Dispose();
        ImmediateContext?.Dispose();
        Debug?.Dispose();
        Device?.Dispose();
    }

    public static void CheckFeatureSupport()
    {
        Console.WriteLine(Device.CheckFeatureOptions2().TiledResourcesTier);
    }
}
