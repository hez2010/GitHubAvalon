using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Octokit;

namespace GitHubAvalon
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void Button_Clicked(object sender, RoutedEventArgs args)
        {
            var client = new GitHubClient(new ProductHeaderValue("GitHubAvalon"));
            var repo = await client.Repository.Content.GetAllContents("hez2010", "TypedocConverter", "/");
            
            this.FindControl<TextBox>("Result").Text = "ok";
        }
    }
}
