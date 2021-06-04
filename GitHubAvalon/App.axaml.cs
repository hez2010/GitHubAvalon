using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using GitHubAvalon.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Node;
using System.Threading.Tasks;

namespace GitHubAvalon
{
    public class App : Application
    {
        public static readonly Styles FluentDark = new()
        {
            new StyleInclude(new Uri("avares://ControlCatalog/Styles"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/FluentDark.xaml")
            }
        };

        public static readonly Styles FluentLight = new()
        {
            new StyleInclude(new Uri("avares://ControlCatalog/Styles"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/FluentLight.xaml")
            }
        };

        public static readonly AppModel Model = new();

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            CreateModelAsync();
        }

        private async void CreateModelAsync()
        {
            await using var fs = File.OpenRead("config.json");
            var json = JsonNode.Parse(fs);

            _ = Model.SetModelAsync(new Octokit.GitHubClient(new Octokit.ProductHeaderValue("GitHubAvalon"))
            {
                Credentials = new Octokit.Credentials(json?["token"]?.ToString() ?? throw new Exception("config"))
            });
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
