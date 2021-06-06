using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using GitHubAvalon.ViewModels;
using Octokit;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHubAvalon.Views
{
    public partial class Notification : UserControl
    {
        private readonly SemaphoreSlim loadSemaphore = new(1, 1);
        private readonly NotificationViewModel viewModel = new();
        private const int PageSize = 20;

        public Notification()
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.FilteredNotifications.Clear();
            viewModel.Notifications.Clear();
            _ = LoadNotificationsAsync();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void FilterNotifications()
        {
            var filtered = viewModel.Notifications.Where(i => true).ToImmutableList();

            viewModel.FilteredNotifications.BeginBulkOperation();

            for (var i = 0; i < filtered.Count; i++)
            {
                if (i < viewModel.FilteredNotifications.Count)
                {
                    if (filtered[i] != viewModel.FilteredNotifications[i])
                    {
                        viewModel.FilteredNotifications.RemoveAt(i);
                        viewModel.FilteredNotifications.Insert(i, filtered[i]);
                    }
                }
                else
                {
                    viewModel.FilteredNotifications.Add(filtered[i]);
                }
            }

            while (viewModel.FilteredNotifications.Count > filtered.Count)
            {
                viewModel.FilteredNotifications.RemoveAt(filtered.Count);
            }

            viewModel.FilteredNotifications.EndBulkOperation();
        }

        private async Task LoadNotificationsAsync()
        {
            if (viewModel.AllLoaded) return;

            await loadSemaphore.WaitAsync();

            try
            {
                var (client, user) = await App.Model.GetModelAsync();
                var notifications = await client.Activity.Notifications.GetAllForCurrent(
                    new NotificationsRequest
                    {
                        All = !viewModel.UnreadOnly,
                        Participating = viewModel.ParticipatedOnly
                    },
                    new ApiOptions
                    {
                        PageCount = 1,
                        PageSize = PageSize,
                        StartPage = ++viewModel.Page
                    });

                viewModel.Notifications.AddRange(notifications.Select(i => new NotificationItem(i)));
                FilterNotifications();

                if (notifications.Count < PageSize)
                {
                    viewModel.AllLoaded = true;
                }
                else
                {
                    var scrollViewer = this.FindControl<ScrollViewer>("NotificationPanel");
                    if (scrollViewer.Content is not ItemsRepeater repeater) return;

                    if (scrollViewer.Offset.Y + scrollViewer.DesiredSize.Height + 50 >= repeater.DesiredSize.Height)
                    {
                        _ = LoadNotificationsAsync();
                    }
                }
            }
            finally
            {
                loadSemaphore.Release();
            }
        }

        private void Notification_ScrollChanged(object sender, ScrollChangedEventArgs args)
        {
            if (sender is not ScrollViewer scrollViewer ||
                scrollViewer.Content is not ItemsRepeater repeater) return;

            if (scrollViewer.Offset.Y + scrollViewer.DesiredSize.Height + 50 >= repeater.DesiredSize.Height && loadSemaphore.CurrentCount > 0)
            {
                _ = LoadNotificationsAsync();
            }
        }
    }
}
