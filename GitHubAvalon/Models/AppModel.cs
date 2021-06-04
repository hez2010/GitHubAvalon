using Octokit;
using System;
using System.Threading.Tasks;

namespace GitHubAvalon.Models
{
    public class AppModel
    {
        private IGitHubClient? client;
        private User? currentUser;
        private readonly TaskCompletionSource tcs = new();

        public async Task SetModelAsync(IGitHubClient client)
        {
            this.client = client;
            currentUser = await client.User.Current();
            tcs.SetResult();
        }

        public async Task<(IGitHubClient Client, User User)> GetModelAsync()
        {
            if (currentUser is null)
            {
                await tcs.Task;
            }

            if (client is null || currentUser is null)
            {
                throw new Exception("invalid user");
            }

            return (client, currentUser);
        }
    }
}
