using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowLog
{
    public sealed record class RequestDto
    {
        [JsonPropertyName("id")] public string Id { get; init; } = "";
        [JsonPropertyName("title")] public string Title { get; init; } = "";
        [JsonPropertyName("content")] public string Content { get; init; } = "";
        [JsonPropertyName("requester_email")] public string RequesterEmail { get; init; } = "";
        [JsonPropertyName("approver")] public string Approver { get; init; } = "";
        [JsonPropertyName("status")] public string Status { get; init; } = "PENDING";
        [JsonPropertyName("comment")] public string? Comment { get; init; }
        [JsonPropertyName("created_at")] public string? CreatedAt { get; init; }
        [JsonPropertyName("updated_at")] public string? UpdatedAt { get; init; }
    }

    public static class RequestOps
    {
        public static string NewRequestId()
        {
            var ts = DateTime.Now.ToString("yyyyMMdd");
            return $"REQ-{ts}-{DateTime.Now:HHmmss}-{Random.Shared.Next(1000, 9999)}";
        }

        public static void CreatePendingJson(string reqId, string title, string content, string requesterEmail, string approverEmail)
        {
            var now = DateTimeOffset.Now.ToString("o");
            var obj = new RequestDto
            {
                Id = reqId,
                Title = title,
                Content = content,
                RequesterEmail = requesterEmail,
                Approver = approverEmail,
                Status = "PENDING",
                CreatedAt = now,
                UpdatedAt = now
            };

            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
            var dir = Path.Combine(Paths.LocalRepo, "requests", "pending");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"{reqId}.json");
            File.WriteAllText(path + ".tmp", json, new UTF8Encoding(false));
            File.Move(path + ".tmp", path, true);
        }

        public static RequestDto? LoadPending(string reqId)
        {
            var path = Path.Combine(Paths.LocalRepo, "requests", "pending", $"{reqId}.json");
            if (!File.Exists(path)) return null;
            var raw = File.ReadAllText(path, new UTF8Encoding(false));
            return JsonSerializer.Deserialize<RequestDto>(raw);
        }

        public static void RemovePendingJson(string reqId)
        {
            var path = Path.Combine(Paths.LocalRepo, "requests", "pending", $"{reqId}.json");
            if (File.Exists(path)) File.Delete(path);
        }
    }
}