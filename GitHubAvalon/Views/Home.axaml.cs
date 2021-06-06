using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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
        private bool allActivitiesLoaded;
        private int page;
        private readonly SemaphoreSlim loadActivitySemaphore = new(1, 1);
        private readonly SemaphoreSlim loadExploreSemaphore = new(1, 1);
        private readonly HomeViewModel viewModel = new();
        private (int UserId, string ActionType, ActivityItem? Item) lastActivityInfo = (default, "", null);
        private const int PageSize = 50;

        public Home()
        {
            InitializeComponent();
            DataContext = viewModel;
            _ = LoadActivityAsync();
            _ = LoadTrendingReposAsync(1);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task LoadTrendingReposAsync(int range)
        {
            await loadExploreSemaphore.WaitAsync();

            viewModel.TrendingRepos.Clear();

            try
            {
                var (client, _) = await App.Model.GetModelAsync();
                var result = await client.Search.SearchRepo(new SearchRepositoriesRequest
                {
                    SortField = RepoSearchSort.Stars,
                    Order = SortDirection.Descending,
                    Created = DateRange.GreaterThan(DateTimeOffset.UtcNow - TimeSpan.FromDays(range)),
                    Archived = false
                });

                viewModel.TrendingRepos.BeginBulkOperation();
                foreach (var i in result.Items) viewModel.TrendingRepos.Add(new(i));
                viewModel.TrendingRepos.EndBulkOperation();
            }
            finally
            {
                loadExploreSemaphore.Release();
            }
        }

        private async Task LoadActivityAsync()
        {
            if (allActivitiesLoaded) return;

            await loadActivitySemaphore.WaitAsync();

            try
            {
                var (client, user) = await App.Model.GetModelAsync();
                var feeds = await client.Activity.Feeds.GetFeeds();
                Console.WriteLine(feeds.CurrentUserUrl);

                var events = await client.Activity.Events.GetAllUserReceived(user.Login, new ApiOptions
                {
                    PageSize = PageSize,
                    PageCount = 1,
                    StartPage = ++page
                });

                viewModel.Activities.BeginBulkOperation();

                var groupedEvents = lastActivityInfo.Item is { GroupedEntries: Activity[] lastActivities } ?
                        lastActivities.ToList() : new List<Activity>();

                foreach (var e in events)
                {
                    var item = ActivityItem.Create(e);
                    if (item is null) continue;
                    if (lastActivityInfo.UserId == e.Actor.Id && lastActivityInfo.ActionType == e.Type)
                    {
                        groupedEvents.Add(e);
                    }
                    else
                    {
                        if (groupedEvents.Count != 0 && lastActivityInfo.Item is not null)
                        {
                            lastActivityInfo.Item.GroupedEntries = groupedEvents.ToArray();
                            groupedEvents.Clear();
                        }

                        viewModel.Activities.Add(lastActivityInfo.Item = item);
                    }
                    lastActivityInfo.UserId = e.Actor.Id;
                    lastActivityInfo.ActionType = e.Type;
                }

                if (groupedEvents.Count != 0 && lastActivityInfo.Item is not null)
                {
                    lastActivityInfo.Item.GroupedEntries = groupedEvents.ToArray();
                    groupedEvents.Clear();
                }

                viewModel.Activities.EndBulkOperation();

                if (events.Count < PageSize)
                {
                    allActivitiesLoaded = true;
                }
                else
                {
                    var scrollViewer = this.FindControl<ScrollViewer>("ActivityPanel");
                    if (scrollViewer.Content is not ItemsRepeater repeater) return;

                    if (scrollViewer.Offset.Y + scrollViewer.DesiredSize.Height + 50 >= repeater.DesiredSize.Height)
                    {
                        _ = LoadActivityAsync();
                    }
                }
            }
            finally
            {
                loadActivitySemaphore.Release();
            }
        }

        private void Activity_ScrollChanged(object sender, ScrollChangedEventArgs args)
        {
            if (sender is not ScrollViewer scrollViewer ||
                scrollViewer.Content is not ItemsRepeater repeater) return;

            if (scrollViewer.Offset.Y + scrollViewer.DesiredSize.Height + 50 >= repeater.DesiredSize.Height && loadActivitySemaphore.CurrentCount > 0)
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

        private async void Star_Clicked(object sender, RoutedEventArgs args)
        {
            if (sender is Button button && button.Tag is ActivityItem item && item.Repository is not null)
            {
                var (client, _) = await App.Model.GetModelAsync();
                try
                {
                    if (item.Starred)
                    {
                        await client.Activity.Starring.RemoveStarFromRepo(item.Repository.Owner.Login, item.Repository.Name);

                        foreach (var i in viewModel.Activities)
                        {
                            if (i.Action is "Unstar" && i.Repository is not null && i.Repository.Id == item.Repository.Id)
                            {
                                i.Action = "Star";
                            }
                        }
                    }
                    else
                    {
                        await client.Activity.Starring.StarRepo(item.Repository.Owner.Login, item.Repository.Name);

                        foreach (var i in viewModel.Activities)
                        {
                            if (i.Action is "Star" && i.Repository is not null && i.Repository.Id == item.Repository.Id)
                            {
                                i.Action = "Unstar";
                            }
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (sender is not ComboBox cb || cb.SelectedItem is not ComboBoxItem si) return;

            _ = LoadTrendingReposAsync(int.Parse(si.Tag?.ToString() ?? "1"));
        }
    }
}
