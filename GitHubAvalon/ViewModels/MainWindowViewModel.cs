using Avalonia.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GitHubAvalon.ViewModels
{
    public record NavItem(string Name, Func<IControl?> ContentFactory)
    {
        private IControl? content;
        public IControl? Content => content ??= ContentFactory();
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private NavItem? item;

        public event PropertyChangedEventHandler? PropertyChanged;

        public NavItem? Item
        {
            get => item;
            set
            {
                item = value;
                OnPropertyChanged(nameof(Item));
            }
        }

        public ObservableCollection<NavItem> NavList { get; } = new();

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new(propertyName));
    }
}
