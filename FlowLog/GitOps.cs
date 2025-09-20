using System;
using System.IO;
using LibGit2Sharp;

namespace FlowLog
{
    public static class GitOps
    {
        public static void EnsureInitialized(string remoteBarePath)
        {
            Paths.EnsureDirs();
            if (!Repository.IsValid(remoteBarePath))
            {
                Directory.CreateDirectory(remoteBarePath);
                Repository.Init(remoteBarePath, isBare: true);
            }
            if (!Directory.Exists(Path.Combine(Paths.LocalRepo, ".git")))
            {
                Repository.Clone(remoteBarePath, Paths.LocalRepo);
                using var repo = new Repository(Paths.LocalRepo);
                Directory.CreateDirectory(Path.Combine(Paths.LocalRepo, "logs", DateTime.Now.ToString("yyyy")));
                Directory.CreateDirectory(Path.Combine(Paths.LocalRepo, "requests", "pending"));
                Commands.Stage(repo, "*");
                var sig = new Signature("system", "system@example.co.jp", DateTimeOffset.Now);
                repo.Commit("init", sig, sig);
                repo.Network.Push(repo.Network.Remotes["origin"], "refs/heads/main", new PushOptions());
            }
        }

        public static void PullFF(Repository repo, Signature sig)
        {
            var opt = new PullOptions { MergeOptions = new MergeOptions { FastForwardStrategy = FastForwardStrategy.FastForwardOnly } };
            Commands.Pull(repo, sig, opt);
        }

        public static void CommitPush(string message, string actor, string email)
        {
            using var repo = new Repository(Paths.LocalRepo);
            var sig = new Signature(actor, email, DateTimeOffset.Now);
            PullFF(repo, sig);
            Commands.Stage(repo, "*");
            if (!repo.RetrieveStatus().IsDirty) return;
            repo.Commit(message, sig, sig);
            repo.Network.Push(repo.Network.Remotes["origin"], "refs/heads/main", new PushOptions());
        }
    }
}