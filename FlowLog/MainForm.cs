// MainForm.cs
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;

namespace FlowLog
{
    public partial class MainForm : Form
    {
        private readonly AppConfig cfg;

        // UI roots
        private readonly TabControl tabs = new() { Dock = DockStyle.Fill };

        // Create tab controls
        private readonly ComboBox cbApproverForCreate = new() { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly Label lblApproverStatus = new() { Dock = DockStyle.Top, AutoSize = true, Text = "承認者: 読込未実行" };

        public MainForm(AppConfig cfg)
        {
            this.cfg = cfg;
            InitializeComponent();

            ApproverStore.Changed += OnApproverChanged;
            ApproverStore.Init(cfg.RemoteBare);

            var menu = BuildMenu();
            menu.Dock = DockStyle.Fill;     // TableLayoutPanelの行にフィット
            menu.Margin = Padding.Empty;

            // TabControl 準備（ここでページを追加してから配置）
            tabs.Dock = DockStyle.Fill;
            tabs.Margin = Padding.Empty;
            tabs.TabPages.Add(BuildCreateTab());
            tabs.TabPages.Add(BuildApproveTab());
            tabs.TabPages.Add(BuildMyTab());
            tabs.TabPages.Add(BuildSearchTab());

            // レイアウトを親で管理（最も安定）
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));         // メニュー行は高さ自動
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));    // タブは残り全体
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.Margin = Padding.Empty;
            root.Padding = Padding.Empty;

            SuspendLayout();
            Controls.Clear();
            MainMenuStrip = menu;
            root.Controls.Add(menu, 0, 0);
            root.Controls.Add(tabs, 0, 1);
            Controls.Add(root);
            ResumeLayout(true);

            RefreshApproverSelector();
        }


        private void EditConfig()
        {
            using var setting = new ConfigSettingForm(this.cfg);
            var dr = setting.ShowDialog();
            if (dr != DialogResult.OK) return;
            var cfg = setting.ResultConfig;
            if (cfg is null) return;
            ConfigOps.Save(cfg);
            MessageBox.Show("設定を保存しました");
        }

        // ---- Menu ----
        private MenuStrip BuildMenu()
        {
            var menu = new MenuStrip();

            // File
            var mFile = new ToolStripMenuItem("ファイル");
            var miSync = new ToolStripMenuItem("再同期 (Pull)") { ShortcutKeys = Keys.F5 };
            miSync.Click += (_, __) => SafeSync();
            var miExit = new ToolStripMenuItem("終了");
            miExit.Click += (_, __) => Close();
            mFile.DropDownItems.AddRange(new ToolStripItem[] { miSync, new ToolStripSeparator(), miExit });

            // View
            var mView = new ToolStripMenuItem("表示");
            var miTabCreate = new ToolStripMenuItem("申請タブ");  miTabCreate.Click  += (_, __) => tabs.SelectedIndex = 0;
            var miTabApprove = new ToolStripMenuItem("承認/却下タブ"); miTabApprove.Click += (_, __) => tabs.SelectedIndex = 1;
            var miTabMine = new ToolStripMenuItem("申請状況/取消タブ"); miTabMine.Click += (_, __) => tabs.SelectedIndex = 2;
            var miTabSearch = new ToolStripMenuItem("履歴検索タブ"); miTabSearch.Click += (_, __) => tabs.SelectedIndex = 3;
            mView.DropDownItems.AddRange([miTabCreate, miTabApprove, miTabMine, miTabSearch]);

            // Config
            var mConfig = new ToolStripMenuItem("設定");
            var miCinfigSetting = new ToolStripMenuItem("設定変更"); 
            miCinfigSetting.Click += (_, __) => EditConfig();
            var miOpenRepo = new ToolStripMenuItem("ローカルリポジトリを開く");
            miOpenRepo.Click += (_, __) => System.Diagnostics.Process.Start("explorer.exe", Paths.LocalRepo);
            var miOpenApproverJson = new ToolStripMenuItem("承認者JSONの場所を開く");
            miOpenApproverJson.Click += (_, __) =>
            {
                var p = ApproverStore.ResolveFilePath(cfg.RemoteBare);
                var d = Path.GetDirectoryName(p) ?? "";
                if (!string.IsNullOrEmpty(d)) System.Diagnostics.Process.Start("explorer.exe", d);
            };
            mConfig.DropDownItems.AddRange([ miCinfigSetting, miOpenRepo, miOpenApproverJson ]);

            // Help
            var mHelp = new ToolStripMenuItem("ヘルプ");
            var miAbout = new ToolStripMenuItem("バージョン情報");
            miAbout.Click += (_, __) => MessageBox.Show("FlowLog\n軽量申請・承認ログツール", "About");
            mHelp.DropDownItems.Add(miAbout);

            menu.Items.AddRange([ mFile, mView, mConfig, mHelp ]);
            return menu;
        }

        private void SafeSync()
        {
            try
            {
                GitOps.CommitPush("(sync)", cfg.Actor, cfg.Email);
                MessageBox.Show("同期完了");
            }
            catch (Exception ex) { MessageBox.Show("同期失敗: " + ex.Message); }
        }

        // ---- Approver auto refresh ----
        private void OnApproverChanged(object? s, EventArgs e)
        {
            if (IsHandleCreated && InvokeRequired) BeginInvoke((Action)RefreshApproverSelector);
            else RefreshApproverSelector();
        }

        private void RefreshApproverSelector()
        {
            var list = ApproverStore.GetAll();
            cbApproverForCreate.BeginUpdate();
            cbApproverForCreate.Items.Clear();
            foreach (var a in list) cbApproverForCreate.Items.Add(new ComboItem(a));
            cbApproverForCreate.EndUpdate();

            var path = ApproverStore.ResolveFilePath(cfg.RemoteBare);
            lblApproverStatus.Text = File.Exists(path)
                ? $"承認者: {list.Count}名 読み込み済み  ({path})"
                : $"承認者: 定義ファイル未検出 期待パス= {path}";

            if (cbApproverForCreate.Items.Count > 0 && cbApproverForCreate.SelectedIndex < 0)
                cbApproverForCreate.SelectedIndex = 0;
        }

        private sealed class ComboItem
        {
            public Approver A { get; }
            public ComboItem(Approver a) => A = a;
            public override string ToString() => $"{A.Name} <{A.Email}>";
        }

        // ---- Tabs ----

        // Create
        private TabPage BuildCreateTab()
        {
            var tab = new TabPage("申請");

            var tbTitle = new TextBox { Dock = DockStyle.Top, PlaceholderText = "タイトル" };
            var tbContent = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, PlaceholderText = "内容（本文）" }; // ★内容

            var grpApprover = new GroupBox { Text = "承認者（通知先）", Dock = DockStyle.Top, Height = 110 };
            grpApprover.Controls.Add(cbApproverForCreate);
            grpApprover.Controls.Add(lblApproverStatus);

            var btn = new Button { Text = "申請作成→Push→通知", Dock = DockStyle.Bottom };
            btn.Click += (s, e) =>
            {
                var reqId = RequestOps.NewRequestId();

                var approverEmail = (cbApproverForCreate.SelectedItem as ComboItem)?.A.Email ?? cfg.Email;
                var requesterEmail = cfg.Email; // ★AppConfigのEmailを使用

                // pending JSON（requesterはEmail、contentを保存）
                RequestOps.CreatePendingJson(reqId, tbTitle.Text, tbContent.Text, requesterEmail, approverEmail);

                // CSV: title + content + requesterEmail を保存
                var line = CsvLog.Line("CREATE", reqId, tbTitle.Text, tbContent.Text, requesterEmail, cfg.Actor, note: "", approver: approverEmail);
                CsvLog.AppendWithRetry(CsvLog.Header, line, cfg.Actor, cfg.Email);

                GitOps.CommitPush($"request: {reqId}", cfg.Actor, cfg.Email);

                // Approverへ通知（本文の先頭だけ抜粋などは任意）
                MailUtil.SendSimple("smtp.example.co.jp", 25, "FlowLog", "no-reply@example.co.jp",
                    new[] { approverEmail }, $"[CREATE] {reqId}",
                    $"申請: {reqId}\nタイトル: {tbTitle.Text}\n申請者: {requesterEmail}\n内容:\n{tbContent.Text}");

                MessageBox.Show($"Created: {reqId}");
            };

            // レイアウト順: 上=承認者, 中=タイトル, 下=本文, 最下=ボタン
            tab.Controls.Add(tbContent);
            tab.Controls.Add(tbTitle);
            tab.Controls.Add(grpApprover);
            tab.Controls.Add(btn);
            return tab;
        }

        // Approve/Reject
        private TabPage BuildApproveTab()
        {
            var tab = new TabPage("承認/却下");

            var lv = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false };
            lv.Columns.Add("REQ-ID", 200);
            lv.Columns.Add("タイトル", 260);
            lv.Columns.Add("申請者(Email)", 200);
            lv.Columns.Add("作成日時", 160);
            lv.DoubleClick += (s, e) => ShowSelectedDetail(lv);

            var p = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true };
            var btnRefresh = new Button { Text = "更新" };
            var btnDetail = new Button { Text = "詳細を見る" };
            var btnApprove = new Button { Text = "承認→Push→通知" };
            var btnReject = new Button { Text = "却下→Push→通知" };
            var tbNote = new TextBox { Width = 420, PlaceholderText = "メモ" };

            btnRefresh.Click += (s, e) => LoadPendingIntoList(lv, onlyMyApprovals: true);
            btnDetail.Click += (s, e) => ShowSelectedDetail(lv);

            btnApprove.Click += (s, e) =>
            {
                if (lv.SelectedItems.Count == 0) return;
                var reqId = (string)lv.SelectedItems[0].Tag;

                var dto = RequestOps.LoadPending(reqId);
                if (dto is null) { MessageBox.Show("pendingが見つかりません"); return; }

                var line = CsvLog.Line("APPROVE", reqId, dto.Title, dto.Content, dto.RequesterEmail, cfg.Actor, tbNote.Text, approver: dto.Approver);
                CsvLog.AppendWithRetry(CsvLog.Header, line, cfg.Actor, cfg.Email);

                RequestOps.RemovePendingJson(reqId);
                GitOps.CommitPush($"approve: {reqId}", cfg.Actor, cfg.Email);

                var to = string.IsNullOrWhiteSpace(dto.RequesterEmail) ? cfg.Email : dto.RequesterEmail;
                MailUtil.SendSimple("smtp.example.co.jp", 25, "FlowLog", "no-reply@example.co.jp",
                    new[] { to }, $"[APPROVE] {reqId}", $"承認されました: {reqId}\nタイトル: {dto.Title}\nNote: {tbNote.Text}");
                btnRefresh.PerformClick();
            };

            // 却下
            btnReject.Click += (s, e) =>
            {
                if (lv.SelectedItems.Count == 0) return;
                var reqId = (string)lv.SelectedItems[0].Tag;

                var dto = RequestOps.LoadPending(reqId);
                if (dto is null) { MessageBox.Show("pendingが見つかりません"); return; }

                var line = CsvLog.Line("REJECT", reqId, dto.Title, dto.Content, dto.RequesterEmail, cfg.Actor, tbNote.Text, approver: dto.Approver);
                CsvLog.AppendWithRetry(CsvLog.Header, line, cfg.Actor, cfg.Email);

                RequestOps.RemovePendingJson(reqId);
                GitOps.CommitPush($"reject: {reqId}", cfg.Actor, cfg.Email);

                var to = string.IsNullOrWhiteSpace(dto.RequesterEmail) ? cfg.Email : dto.RequesterEmail;
                MailUtil.SendSimple("smtp.example.co.jp", 25, "FlowLog", "no-reply@example.co.jp",
                    new[] { to }, $"[REJECT] {reqId}", $"却下されました: {reqId}\nタイトル: {dto.Title}\nNote: {tbNote.Text}");
                btnRefresh.PerformClick();
            };

            p.Controls.Add(btnRefresh);
            p.Controls.Add(btnDetail);
            p.Controls.Add(btnApprove);
            p.Controls.Add(btnReject);
            p.Controls.Add(new Label() { Text = "Note:" });
            p.Controls.Add(tbNote);

            tab.Controls.Add(lv);
            tab.Controls.Add(p);

            LoadPendingIntoList(lv, onlyMyApprovals: true);
            btnRefresh.PerformClick();
            return tab;
        }

        // My pending + Cancel
        private TabPage BuildMyTab()
        {
            var tab = new TabPage("申請状況/取消");

            var lv = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false
            };
            lv.Columns.Add("REQ-ID", 200);
            lv.Columns.Add("タイトル", 260);
            lv.Columns.Add("承認者(Email)", 220);
            lv.Columns.Add("作成日時", 160);

            lv.DoubleClick += (s, e) => ShowSelectedDetail(lv);

            var p = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true };
            var btnRefresh = new Button { Text = "更新" };
            var btnDetail = new Button { Text = "詳細を見る" };
            var btnCancel = new Button { Text = "未承認を取消（CANCEL）" };
            var tbNote = new TextBox { Width = 420, PlaceholderText = "取消メモ（任意）" };

            btnRefresh.Click += (s, e) => LoadMyPending(lv);
            btnDetail.Click += (s, e) => ShowSelectedDetail(lv);

            btnCancel.Click += (s, e) =>
            {
                if (lv.SelectedItems.Count == 0) return;
                var reqId = (string)lv.SelectedItems[0].Tag;

                var dto = RequestOps.LoadPending(reqId);
                if (dto is null) { MessageBox.Show("pendingが見つかりません"); return; }

                // CANCEL をCSVに記録（requesterはEmail）
                var line = CsvLog.Line("CANCEL", reqId, dto.Title, dto.Content, dto.RequesterEmail, cfg.Actor, tbNote.Text, approver: dto.Approver);
                CsvLog.AppendWithRetry(CsvLog.Header, line, cfg.Actor, cfg.Email);

                RequestOps.RemovePendingJson(reqId);
                GitOps.CommitPush($"cancel: {reqId}", cfg.Actor, cfg.Email);

                MessageBox.Show("取消しました");
                btnRefresh.PerformClick();
            };

            p.Controls.AddRange(new Control[] { btnRefresh, btnDetail, btnCancel, new Label { Text = "Note:" }, tbNote });

            tab.Controls.Add(lv);
            tab.Controls.Add(p);

            LoadMyPending(lv);
            btnRefresh.PerformClick();
            return tab;
        }

        // Search history
        private TabPage BuildSearchTab()
        {
            var tab = new TabPage("履歴検索");
            var tbQuery = new TextBox { Dock = DockStyle.Top, PlaceholderText = "req_id, actor, title などでフィルタ" };
            var tbOut = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, Font = new System.Drawing.Font("Consolas", 10) };
            var btn = new Button { Text = "検索", Dock = DockStyle.Top };

            btn.Click += (s, e) =>
            {
                var yearDir = Path.Combine(Paths.LocalRepo, "logs", DateTime.Now.ToString("yyyy"));
                if (!Directory.Exists(yearDir)) { tbOut.Text = "(no logs)"; return; }

                // ①改行を無視するためのサニタイズ
                string San(string? x) => string.IsNullOrEmpty(x) ? "" : x.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");

                // ②approverメール→氏名解決
                var approverDict = ApproverStore.GetAll()
                    .GroupBy(a => a.Email, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);

                int wFile = 12, wAt = 19, wAct = 8, wReq = 24, wTitle = 16, wReqer = 24, wAppr = 24; // atは秒まで19桁
                string Cut(string s, int w) => s.Length <= w ? s.PadRight(w) : s.Substring(0, w - 1) + "…";

                var sb = new StringBuilder();
                sb.AppendLine(string.Join(" | ", new[]
                {
                    Cut("file", wFile), Cut("at", wAt), Cut("action", wAct),
                    Cut("req_id", wReq), Cut("title", wTitle),
                    Cut("requester", wReqer), Cut("approver", wAppr)
                }));

                foreach (var csv in Directory.GetFiles(yearDir, "*.csv"))
                {
                    using var p = new TextFieldParser(csv, new UTF8Encoding(false));
                    p.HasFieldsEnclosedInQuotes = true;
                    p.SetDelimiters(",");

                    if (p.EndOfData) continue;
                    var h = p.ReadFields() ?? Array.Empty<string>();
                    int idx(string name) => Array.FindIndex(h, x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
                    int iAt = idx("at"), iAct = idx("action"), iReq = idx("req_id"),
                        iTitle = idx("title"), iReqer = idx("requester"), iAppr = idx("approver"),
                        iContent = idx("content"); // 参照しないが行内の改行対策としてサニタイズ対象にできる

                    while (!p.EndOfData)
                    {
                        var f = p.ReadFields() ?? Array.Empty<string>();
                        string V(int i) => (i >= 0 && i < f.Length) ? f[i] : "";

                        // サニタイズ（content含む全てから改行を除去）
                        var rawAt = San(V(iAt));
                        var act = San(V(iAct));
                        var req = San(V(iReq));
                        var ttl = San(V(iTitle));
                        var reqMail = San(V(iReqer));
                        var apprMail = San(V(iAppr));
                        var _ = San(V(iContent)); // 破壊的に使わないが改行はここで吸収

                        // 表示は秒まで
                        var atFmt = rawAt;
                        if (DateTimeOffset.TryParse(rawAt, out var dto)) atFmt = dto.ToString("yyyy-MM-ddTHH:mm:ss");

                        // approverは氏名に解決（なければメール）
                        var apprDisp = approverDict.TryGetValue(apprMail, out var nm) && !string.IsNullOrWhiteSpace(nm)
                                        ? nm
                                        : apprMail;

                        var line = string.Join(" | ", new[]
                        {
                    Cut(Path.GetFileName(csv), wFile),
                    Cut(atFmt, wAt),
                    Cut(act, wAct),
                    Cut(req, wReq),
                    Cut(ttl, wTitle),
                    Cut(reqMail, wReqer),
                    Cut(apprDisp, wAppr)
                });

                        if (string.IsNullOrWhiteSpace(tbQuery.Text) ||
                            line.IndexOf(tbQuery.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            sb.AppendLine(line);
                        }
                    }
                }

                tbOut.Text = sb.ToString();
            };

            tab.Controls.Add(tbOut);
            tab.Controls.Add(btn);
            tab.Controls.Add(tbQuery);
            return tab;
        }

        // ---- Helpers ----

        private void LoadPendingIntoList(ListView lv, bool onlyMyApprovals)
        {
            lv.Items.Clear();
            var dir = Path.Combine(Paths.LocalRepo, "requests", "pending");
            Directory.CreateDirectory(dir);

            foreach (var f in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var raw = File.ReadAllText(f, new UTF8Encoding(false));
                    var dto = JsonSerializer.Deserialize<RequestDto>(raw);
                    if (dto is null || string.IsNullOrWhiteSpace(dto.Id)) continue;

                    if (onlyMyApprovals)
                    {
                        // 単一承認者制: 自分が承認者でない案件は除外
                        if (!string.Equals(dto.Approver, cfg.Email, StringComparison.OrdinalIgnoreCase)) continue;
                    }

                    var created = dto.CreatedAt ?? "";
                    var it = new ListViewItem(new[] { dto.Id, dto.Title, dto.RequesterEmail, created }) { Tag = dto.Id };
                    lv.Items.Add(it);
                }
                catch { /* skip bad json */ }
            }
        }

        private void LoadMyPending(ListView lv)
        {
            lv.Items.Clear();
            var dir = Path.Combine(Paths.LocalRepo, "requests", "pending");
            Directory.CreateDirectory(dir);

            foreach (var f in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var raw = File.ReadAllText(f, new UTF8Encoding(false));
                    var dto = JsonSerializer.Deserialize<RequestDto>(raw);
                    if (dto is null || string.IsNullOrWhiteSpace(dto.Id)) continue;

                    // 自分が申請者のPENDINGのみ
                    if (!string.Equals(dto.RequesterEmail, cfg.Email, StringComparison.OrdinalIgnoreCase)) continue;

                    var created = dto.CreatedAt ?? "";
                    var it = new ListViewItem(new[] { dto.Id, dto.Title, dto.Approver, created }) { Tag = dto.Id };
                    lv.Items.Add(it);
                }
                catch { /* skip bad json */ }
            }
        }

        private void ShowSelectedDetail(ListView lv)
        {
            if (lv.SelectedItems.Count == 0) return;
            var reqId = (string?)lv.SelectedItems[0].Tag;
            if(reqId is null) return;
            var path = Path.Combine(Paths.LocalRepo, "requests", "pending", $"{reqId}.json");
            if (!File.Exists(path)) { MessageBox.Show("ファイルが見つかりません"); return; }

            var raw = File.ReadAllText(path, new UTF8Encoding(false));
            var dto = JsonSerializer.Deserialize<RequestDto>(raw) ?? new RequestDto { Id = reqId };
            using var dlg = new DetailForm(dto, raw);
            dlg.ShowDialog(this);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ApproverStore.Changed -= OnApproverChanged;
            base.OnFormClosed(e);
        }
    }
}
