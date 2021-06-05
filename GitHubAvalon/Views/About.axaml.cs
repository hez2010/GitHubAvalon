using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Reflection;

namespace GitHubAvalon.Views
{
    public partial class About : UserControl
    {
        public string Version { get; }

        public About()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
            InitializeComponent();
            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
