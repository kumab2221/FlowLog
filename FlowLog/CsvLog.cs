using System;
using System.IO;
using System.Text;
using LibGit2Sharp;

namespace FlowLog
{
    public static class CsvLog
    {
        public const string Header = "at,actor,action,req_id,title,content,requester,approver,note"; // ★content追加

        public static string Line(
            string action,
            string reqId,
            string title,
            string content,
            string requesterEmail,
            string actor,
            string note,
            string approver = "")
        {
            string esc(string s) => $"\"{(s ?? string.Empty).Replace("\"", "\"\"")}\"";
            return string.Join(",",
                DateTimeOffset.Now.ToString("o"),
                esc(actor),
                esc(action),
                esc(reqId),
                esc(title),
                esc(content),
                esc(requesterEmail),
                esc(approver),
                esc(note)
            );
        }

        public static string MonthlyCsvPath(DateTime now)
        {
            var dir = Path.Combine(Paths.LocalRepo, "logs", now.ToString("yyyy"));
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"{now:yyyy-MM}.csv");
        }

        static void AppendAtomically(string csvPath, string header, string line)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(csvPath)!);
            var tmp = Path.Combine(Path.GetDirectoryName(csvPath)!, Path.GetRandomFileName());
            var utf8 = new UTF8Encoding(false);

            using (var sw = new StreamWriter(tmp, false, utf8))
            {
                if (File.Exists(csvPath))
                {
                    var text = File.ReadAllText(csvPath, utf8);
                    sw.Write(text);
                    if (text.Length > 0 && !text.EndsWith("\n")) sw.WriteLine();
                }
                else
                {
                    sw.WriteLine(header);
                }
                sw.WriteLine(line);
            }
            File.Move(tmp, csvPath, true);
        }

        public static void AppendWithRetry(string header, string line, string actor, string email, int maxRetry = 3)
        {
            using var repo = new Repository(Paths.LocalRepo);
            var sig = new Signature(actor, email, DateTimeOffset.Now);
            for (int i = 0; i <= maxRetry; i++)
            {
                GitOps.PullFF(repo, sig);
                var csv = MonthlyCsvPath(DateTime.Now);
                AppendAtomically(csv, header, line);
                try
                {
                    Commands.Stage(repo, "*");
                    if (!repo.RetrieveStatus().IsDirty) return;
                    repo.Commit("log: append", sig, sig);
                    repo.Network.Push(repo.Network.Remotes["origin"], "refs/heads/main", new PushOptions());
                    return;
                }
                catch (NonFastForwardException) { /* retry */ }
                System.Threading.Thread.Sleep(300 * (int)Math.Pow(2, i));
            }
            throw new Exception("push retry exceeded");
        }

        
    }
}