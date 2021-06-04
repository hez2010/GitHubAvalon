using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using GitHubAvalon.ViewModels;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHubAvalon.Views
{
    public partial class Home : UserControl
    {
        private bool loaded;
        private bool allLoaded;
        private int page;
        private readonly SemaphoreSlim loadSemaphore = new(1, 1);
        private readonly HomeViewModel viewModel = new();
        private ActivityItem? lastItem = null;
        private (int UserId, string ActionType) lastInfo = (default, "");

        public Home()
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (!loaded)
            {
                loaded = true;
                _ = LoadActivityAsync();
            }
        }

        private async Task LoadActivityAsync()
        {
            if (allLoaded) return;

            await loadSemaphore.WaitAsync();

            try
            {
                var (client, user) = await App.Model.GetModelAsync();

                var events = await client.Activity.Events.GetAllUserReceived(user.Login, new ApiOptions
                {
                    PageSize = 20,
                    PageCount = 1,
                    StartPage = ++page
                });

                if (events.Count < 20)
                {
                    allLoaded = true;
                }

                viewModel.Activities.BeginBulkOperation();

                var groupedEvents = lastItem is { GroupedEntries: Activity[] lastActivities } ? 
                        lastActivities.ToList() : new List<Activity>();

                foreach (var e in events)
                {
                    var item = ActivityItem.Create(e);
                    if (item is null) continue;
                    if (lastInfo.UserId == e.Actor.Id && lastInfo.ActionType == e.Type)
                    {
                        groupedEvents.Add(e);
                    }
                    else
                    {
                        if (groupedEvents.Count != 0 && lastItem is not null)
                        {
                            lastItem.GroupedEntries = groupedEvents.ToArray();
                            groupedEvents.Clear();
                        }

                        viewModel.Activities.Add(lastItem = item);
                    }
                    lastInfo.UserId = e.Actor.Id;
                    lastInfo.ActionType = e.Type;
                }

                if (groupedEvents.Count != 0 && lastItem is not null)
                {
                    lastItem.GroupedEntries = groupedEvents.ToArray();
                    groupedEvents.Clear();
                }

                viewModel.Activities.EndBulkOperation();
            }
            finally
            {
                loadSemaphore.Release();
            }
        }

        private void Activity_ScrollChanged(object sender, ScrollChangedEventArgs args)
        {
            if (sender is not ScrollViewer scrollViewer ||
                scrollViewer.Content is not ItemsRepeater repeater) return;

            if (scrollViewer.Offset.Y + scrollViewer.DesiredSize.Height + 50 >= repeater.DesiredSize.Height && loadSemaphore.CurrentCount > 0)
            {
                _ = LoadActivityAsync();
            }
        }

        private void ShowMore_PointerReleased(object sender, PointerReleasedEventArgs args)
        {
            if (sender is not TextBlock textBlock || textBlock.Tag is not ActivityItem activityItem) return;

            if (!activityItem.HasGroupedEntries) return;

            var index = viewModel.Activities.IndexOf(activityItem);
            if (index == -1) return;

            viewModel.Activities.BeginBulkOperation();
            foreach (var e in activityItem.GroupedEntries)
            {
                var item = ActivityItem.Create(e);
                if (item is null) continue;
                viewModel.Activities.Insert(++index, item);
            }

            activityItem.GroupedEntries = Array.Empty<Activity>();
            viewModel.Activities.EndBulkOperation();
        }

        private void Star_Clicked(object sender, RoutedEventArgs args)
        {
            if (sender is Button button && button.Tag is ActivityItem item)
            {
                item.Action = "ñvé ";
            }
        }
    }
}
