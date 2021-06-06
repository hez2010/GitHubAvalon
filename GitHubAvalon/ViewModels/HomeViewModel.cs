using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GitHubAvalon.Utils;
using Octokit;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GitHubAvalon.ViewModels
{
    public class RepositoryItem : INotifyPropertyChanged
    {
        public RepositoryItem(Repository repo)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(repo.Language))
            {
                sb.Append(repo.Language);
                sb.Append(". ");
            }
            sb.AppendFormat("Stars: {0}. Forks: {1}.", repo.StargazersCount, repo.ForksCount);

            Meta = sb.ToString();
            Repo = repo;
        }

        public Repository Repo { get; }
        public string Meta { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new(propertyName));
    }

    public enum ActionType { None, Star, Follow }
    public class ActivityItem : INotifyPropertyChanged, IDisposable
    {
        private bool loading;
        private bool disposed;
        private IBitmap? avatar;
        private string? description;
        private string? meta;
        private Activity[]? others;
        private string? action;
        private readonly static Bitmap emptyBitmap = new WriteableBitmap(new PixelSize(36, 36), Vector.One, PixelFormat.Bgra8888, AlphaFormat.Premul);

        private ActivityItem(Activity activity, string eventAction)
        {
            Activity = activity;
            EventAction = eventAction;
            Description = Activity.Repo.Description;
            Date = activity.CreatedAt.ToString("g");
        }

        public static ActivityItem? Create(Activity activity)
        {
            var (middle, _) = GetAction(activity.Payload);
            if (string.IsNullOrEmpty(middle)) return null;

            return new ActivityItem(activity, middle);
        }

        public Activity Activity { get; }
        public Repository? Repository { get; private set; }
        public string EventAction { get; }
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
                OnPropertyChanged(nameof(IsStarAction));
                OnPropertyChanged(nameof(Starred));
                OnPropertyChanged(nameof(HasAction));
            }
        }
        public bool HasAction => !string.IsNullOrEmpty(Action);
        public bool IsStarAction => Action is "Star" or "Unstar";
        public bool Starred => Action is "Unstar";
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
        private static (string MiddleText, ActionType Type) GetAction(ActivityPayload type) => type switch
        {
            CreateEventPayload create when create.Ref == create.MasterBranch => ("created a repository", ActionType.Star),
            ForkEventPayload fork => ("forked", ActionType.Star),
            ReleaseEventPayload release => ($"{release.Action} a release {release.Release.Name} in", ActionType.Star),
            IssueEventPayload issue => ($"{issue.Action} an issue in", ActionType.Star),
            PushEventPayload push => ($"pushed {push.Commits.Count} {(push.Commits.Count == 1 ? "commit" : "commits")} to", ActionType.None),
            PullRequestEventPayload pull => ($"{pull.Action} a pull request in", ActionType.Star),
            StarredEventPayload star => ("starred", ActionType.Star),
            _ => ("", ActionType.None)
        };

        private async Task<string> GetDescriptionAsync(IGitHubClient client, Repository repo) => Activity.Payload switch
        {
            IssueEventPayload issue => (await client.Issue.Get(repo.Id, issue.Issue.Number)).Title,
            PullRequestEventPayload pull => (await client.PullRequest.Get(repo.Id, pull.PullRequest.Number)).Title,
            ReleaseEventPayload release => (await client.Repository.Release.Get(repo.Id, release.Release.Id)).Body,
            _ => repo.Description
        };

        private async Task GetDelayedInfoAsync()
        {
            var (client, _) = await App.Model.GetModelAsync();
            var avatarData = await client.Connection.GetRaw(new Uri(Activity.Actor.AvatarUrl), null);
            using var stream = new MemoryStream(avatarData.Body);
            Repository = await client.Repository.Get(Activity.Repo.Id);
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(Repository.Language))
            {
                sb.Append(Repository.Language);
                sb.Append(". ");
            }
            sb.AppendFormat("Stars: {0}. Updated at {1}.", Repository.StargazersCount, Repository.UpdatedAt.ToString("g"));

            var (_, action) = GetAction(Activity.Payload);

            Avatar = new Bitmap(stream);
            Description = await GetDescriptionAsync(client, Repository);
            Meta = sb.ToString();
            Action = action switch
            {
                ActionType.Star => await client.Activity.Starring.CheckStarred(Repository.Owner.Login, Repository.Name) ? "Unstar" : "Star",
                _ => ""
            };

            loading = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new(propertyName));

        ~ActivityItem()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                Avatar?.Dispose();
                GC.SuppressFinalize(this);
            }
        }
    }
    public class HomeViewModel : INotifyPropertyChanged
    {
        private int trendingDaysFilter = 1;

        public BulkObservableCollection<ActivityItem> Activities { get; } = new();
        public BulkObservableCollection<RepositoryItem> TrendingRepos { get; } = new();
        public int TrendingDaysFilter
        {
            get => trendingDaysFilter;
            set
            {
                trendingDaysFilter = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new(propertyName));

    }
}
