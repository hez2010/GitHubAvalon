using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using GitHubAvalon.Utils;
using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GitHubAvalon.ViewModels
{
    public class ActivityItem : INotifyPropertyChanged
    {
        private bool loading;
        private IBitmap? avatar;
        private string? description;
        private string? meta;
        private Activity[]? others;
        private string? action;
        private readonly static Bitmap emptyBitmap = new WriteableBitmap(new PixelSize(36, 36), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Premul);

        private ActivityItem(Activity activity, string middleText, string actionName)
        {
            Activity = activity;
            Title = Activity.Actor.Login + " " + middleText + " " + Activity.Repo.Name;
            Description = Activity.Repo.Description;
            Date = activity.CreatedAt.ToString("g");
            Action = actionName;
        }

        public static ActivityItem? Create(Activity activity)
        {
            var action = GetAction(activity.Payload);
            if (string.IsNullOrEmpty(action.MiddleText)) return null;

            return new ActivityItem(activity, action.MiddleText, action.ActionName);
        }

        public Activity Activity { get; private set; }
        public string Title { get; private set; }
        public string Description
        {
            get
            {
                if (description is null && !loading)
                {
                    loading = true;
                    _ = GetDelayedInfoAsync();
                }

                return description ?? "";
            }
            private set
            {
                description = value;
                OnPropertyChanged();
            }
        }
        public string Meta
        {
            get
            {
                if (meta is null && !loading)
                {
                    loading = true;
                    _ = GetDelayedInfoAsync();
                }

                return meta ?? "";
            }
            private set
            {
                meta = value;
                OnPropertyChanged();
            }
        }
        public string Date { get; private set; }
        public IBitmap Avatar
        {
            get
            {
                if (avatar is null && !loading)
                {
                    loading = true;
                    _ = GetDelayedInfoAsync();
                }

                return avatar ?? emptyBitmap;
            }
            private set
            {
                avatar = value;
                OnPropertyChanged();
            }
        }
        public string Action
        {
            get => action ?? "";
            set
            {
                action = value;
                OnPropertyChanged();
            }
        }
        public bool HasAction => !string.IsNullOrEmpty(Action);
        public bool HasGroupedEntries => GroupedEntries.Length != 0;
        public string ShowMoreText => $"Expand {GroupedEntries.Length} more {(GroupedEntries.Length == 1 ? "activity" : "activities")}...";
        public Activity[] GroupedEntries
        {
            get => others ?? Array.Empty<Activity>();
            set
            {
                others = value;
                OnPropertyChanged(nameof(HasGroupedEntries));
                OnPropertyChanged(nameof(ShowMoreText));
            }
        }
        private static (string MiddleText, string ActionName) GetAction(ActivityPayload type) => type switch
        {
            CreateEventPayload create when create.Ref == create.MasterBranch => ("created a repository", "Star"),
            ForkEventPayload fork => ("forked", "Star"),
            ReleaseEventPayload release => ($"released {release.Release.Name} in", "Star"),
            IssueEventPayload issue => ("created an issue in", "Star"),
            PushEventPayload push => ("pushed to", "Star"),
            PullRequestEventPayload pull => ("created a pull request to", "Star"),
            StarredEventPayload star => ("starred", "Star"),
            _ => ("", "")
        };

        private async Task GetDelayedInfoAsync()
        {
            var (client, _) = await App.Model.GetModelAsync();
            var avatarData = await client.Connection.GetRaw(new Uri(Activity.Actor.AvatarUrl), null);
            using var stream = new MemoryStream(avatarData.Body);
            var repo = await client.Repository.Get(Activity.Repo.Id);
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(repo.Language))
            {
                sb.Append(repo.Language);
                sb.Append(". ");
            }
            sb.AppendFormat("Stars: {0}. Updated at {1}.", repo.StargazersCount, repo.UpdatedAt.ToString("g"));

            Avatar = new Bitmap(stream);
            Description = repo.Description;
            Meta = sb.ToString();

            loading = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new(propertyName));
    }
    public class HomeViewModel
    {
        public BulkObservableCollection<ActivityItem> Activities { get; } = new();
    }
}
