using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using GitHubAvalon.ViewModels;
using GitHubAvalon.Views;

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
            viewModel.NavList.Add(new("Issues & Pull requests", () => null));
            viewModel.NavList.Add(new("Repositories", () => null));
            viewModel.NavList.Add(new("Organizations", () => null));
            viewModel.NavList.Add(new("Notifications", () => new Notification()));
            viewModel.NavList.Add(new("Profile", () => null));
            viewModel.NavList.Add(new("Settings", () => null));
            viewModel.NavList.Add(new("About", () => new About()));
        }
    }
}
