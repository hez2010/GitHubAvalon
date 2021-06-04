using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GitHubAvalon.ViewModels;
using GitHubAvalon.Views;
using Octokit;
using System.Collections.Generic;

namespace GitHubAvalon
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel viewModel = new();
        public MainWindow()
        {
            LoadNavItems();
            InitializeComponent();
            DataContext = viewModel;

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void LoadNavItems()
        {
            viewModel.NavList.Add(new("Home", () => new Home()));
            viewModel.NavList.Add(new("Issues", () => null));
            viewModel.NavList.Add(new("Pull requests", () => null));
            viewModel.NavList.Add(new("Repositories", () => null));
            viewModel.NavList.Add(new("Notifications", () => null));
            viewModel.NavList.Add(new("Profile", () => null));
            viewModel.NavList.Add(new("Settings", () => null));
            viewModel.NavList.Add(new("About", () => null));
        }

        private void Window_PointerPressed(object sender, PointerPressedEventArgs args)
        {
            var point = args.GetCurrentPoint(this);
            if (point.Position.Y <= 20 && point.Pointer.Captured is Border)
            {
                BeginMoveDrag(args);
            }
        }
    }
}
