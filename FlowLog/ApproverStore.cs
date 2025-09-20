using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FlowLog
{
    /// <summary>
    /// 承認者定義ファイル（NASのbareと同一階層）を読み込み＋監視する
    /// 例: RemoteBare= \\nas\git\flowlog.git → \\nas\git\flowlog.approvers.json
    /// JSONスキーマ: [{ "id":"managerA","name":"山田 太郎","email":"managerA@example.co.jp" }, ...]
    /// </summary>
    public record Approver(string Id, string Name, string Email);


    public static class ApproverStore
    {
        private static readonly object _lock = new();
        private static List<Approver> _cache = new();
        private static FileSystemWatcher? _watcher;
        private static string _path = string.Empty;

        public static event EventHandler? Changed;

        public static string ResolveFilePath(string remoteBarePath)
        {
            var dir = Path.GetDirectoryName(remoteBarePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) ?? "";
            var repoName = Path.GetFileName(remoteBarePath).Replace(".git", "", StringComparison.OrdinalIgnoreCase);
            return Path.Combine(dir, $"{repoName}.approvers.json");
        }

        public static void Init(string remoteBarePath)
        {
            _path = ResolveFilePath(remoteBarePath);
            Load(); // 初回読み込み
            StartWatch();
        }

        public static IReadOnlyList<Approver> GetAll()
        {
            lock (_lock) return _cache.AsReadOnly();
        }

        private static void Load()
        {
            try
            {
                if (!File.Exists(_path))
                {
                    lock (_lock) _cache = new List<Approver>();
                    return;
                }
                var json = File.ReadAllText(_path);
                var list = JsonSerializer.Deserialize<List<Approver>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                lock (_lock) _cache = list;
            }
            catch
            {
                lock (_lock) _cache = new List<Approver>();
            }
            Changed?.Invoke(null, EventArgs.Empty);
        }

        private static void StartWatch()
        {
            try
            {
                _watcher?.Dispose();
                var dir = Path.GetDirectoryName(_path);
                var file = Path.GetFileName(_path);
                if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(file)) return;

                _watcher = new FileSystemWatcher(dir, file)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
                };
                _watcher.Changed += (_, __) => Load();
                _watcher.Created += (_, __) => Load();
                _watcher.Renamed += (_, __) => Load();
                _watcher.EnableRaisingEvents = true;
            }
            catch { /* 監視できない環境でも致命ではない */ }
        }
    }
}