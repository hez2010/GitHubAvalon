using GitHubAvalon.Utils;
using Octokit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GitHubAvalon.ViewModels
{
    public class NotificationItem : INotifyPropertyChanged
    {
        public NotificationItem(Notification notification)
        {
            Notification = notification;
        }

        public Notification Notification { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new(propertyName));
    }
    public class NotificationViewModel : INotifyPropertyChanged
    {
        private bool unreadOnly;
        private bool participatedOnly;

        public bool UnreadOnly
        {
            get => unreadOnly;
            set
            {
                unreadOnly = value;
                Page = 0;
                AllLoaded = false;
                OnPropertyChanged();
            }
        }

        public bool ParticipatedOnly
        {
            get => participatedOnly;
            set
            {
                participatedOnly = value;
                Page = 0;
                AllLoaded = false;
                OnPropertyChanged();
            }
        }

        public int Page { get; set; }
        public bool AllLoaded { get; set; }

        public List<NotificationItem> Notifications { get; } = new();
        public BulkObservableCollection<NotificationItem> FilteredNotifications { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new(propertyName));
    }
}
