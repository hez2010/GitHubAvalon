using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;

namespace GitHubAvalon
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new SkiaOptions
                {
                    MaxGpuResourceSizeBytes = 8096000
                })
                .With(new Win32PlatformOptions
                {
                    UseWindowsUIComposition = true,
                    EnableMultitouch = true
                })
                .With(new MacOSPlatformOptions
                {
                    ShowInDock = true
                })
                .With(new X11PlatformOptions
                {
                    EnableIme = true,
                    EnableMultiTouch = true,
                    UseGpu = true
                })
                .With(new AvaloniaNativePlatformOptions
                {
                    UseGpu = true
                })
                .LogToTrace();
    }
}
