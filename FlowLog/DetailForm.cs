using System;
using System.Text.Json;
using System.Windows.Forms;

namespace FlowLog
{
    public sealed class DetailForm : Form
    {
        public DetailForm(RequestDto req, string rawJson)
        {
            Text = $"詳細: {req.Id}";
            Width = 700; Height = 540;

            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            void Row(string k, string v) { grid.Controls.Add(new Label { Text = k, AutoSize = true }); grid.Controls.Add(new TextBox { Text = v, ReadOnly = true, Dock = DockStyle.Fill }); }

            Row("REQ-ID", req.Id);
            Row("タイトル", req.Title);
            Row("申請者", req.Requester);
            Row("承認者", req.Approver);
            Row("状態", req.Status);
            Row("作成", req.CreatedAt ?? "");
            Row("更新", req.UpdatedAt ?? "");
            Row("メモ", req.Comment ?? "");

            var tbJson = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill, Font = new System.Drawing.Font("Consolas", 9) };
            try { tbJson.Text = JsonSerializer.Serialize(JsonSerializer.Deserialize<object>(rawJson)!, new JsonSerializerOptions { WriteIndented = true }); }
            catch { tbJson.Text = rawJson; }

            var tabs = new TabControl { Dock = DockStyle.Fill };
            var t1 = new TabPage("概要"); t1.Controls.Add(grid);
            var t2 = new TabPage("JSON"); t2.Controls.Add(tbJson);
            tabs.TabPages.Add(t1); tabs.TabPages.Add(t2);

            Controls.Add(tabs);
        }
    }
}