using Cake.Frosting;
using Git = LibGit2Sharp;

namespace Automation;

public class GitPush : AsyncFrostingTask<Context>
{
    public override async Task RunAsync(Context context)
    {
        var secrets = await Secrets.LoadAsync();
        string token = secrets.GitToken;
        using var repo = new Git.Repository(Context.ProjectRoot);
        string currentBranch = repo.Head.FriendlyName;

        var remote = repo.Network.Remotes["origin"] ?? throw new InvalidOperationException();
        var pushRefSpec = $"+refs/heads/{currentBranch}:refs/heads/{currentBranch}";
        var options = new Git.PushOptions
        {
            CredentialsProvider = (_, _, _) =>
                new Git.UsernamePasswordCredentials { Username = "git", Password = token },
        };
        repo.Network.Push(remote, pushRefSpec, options);
    }
}
