using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;

namespace FlowLog
{
    public partial class MainForm : Form
    {
        private AppConfig cfg = null!;

        private TabControl tabs = new() { Dock = DockStyle.Fill };
        private ComboBox cbApproverForCreate = new() { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
        private Label lblApproverStatus = new() { Dock = DockStyle.Top, AutoSize = true, Text = "承認者: 読込未実行" };


        public MainForm(AppConfig cfg)
        {
            InitializeComponent();

            this.cfg = cfg;

            ApproverStore.Init(cfg.RemoteBare);
            ApproverStore.Changed += (_, __) => Invoke(new Action(RefreshApproverSelector));

            tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildCreateTab());
            tabs.TabPages.Add(BuildApproveTab());
            tabs.TabPages.Add(BuildMyTab());
            tabs.TabPages.Add(BuildSearchTab());

            BuildMenu();

            Controls.Add(tabs);
            ResumeLayout(performLayout: true);

            RefreshApproverSelector();
        }
        private MenuStrip BuildMenu()
        {
            var menu = new MenuStrip();

            var mFile = new ToolStripMenuItem("ファイル");
            var miSync = new ToolStripMenuItem("再同期 (Pull)") { ShortcutKeys = Keys.F5 };
            miSync.Click += (_, __) => SafeSync();
            var miExit = new ToolStripMenuItem("終了");
            miExit.Click += (_, __) => Close();
            mFile.DropDownItems.AddRange(new ToolStripItem[] { miSync, new ToolStripSeparator(), miExit });

            var mView = new ToolStripMenuItem("表示");
            var miTabCreate  = new ToolStripMenuItem("申請タブへ");
            var miTabApprove = new ToolStripMenuItem("承認/却下タブへ");
            var miTabSearch  = new ToolStripMenuItem("履歴検索タブへ");
            miTabCreate.Click  += (_, __) => tabs.SelectedIndex = 0;
            miTabApprove.Click += (_, __) => tabs.SelectedIndex = 1;
            miTabSearch.Click  += (_, __) => tabs.SelectedIndex = 2;
            mView.DropDownItems.AddRange(new[] { miTabCreate, miTabApprove, miTabSearch });

            var mConfig = new ToolStripMenuItem("設定");
            var miEditCfg = new ToolStripMenuItem("基本設定の編集…");
            miEditCfg.Click += (_, __) => EditConfig();
            var miOpenRepo = new ToolStripMenuItem("ローカルリポジトリを開く");
            miOpenRepo.Click += (_, __) => System.Diagnostics.Process.Start("explorer.exe", Paths.LocalRepo);
            var miOpenApproverJson = new ToolStripMenuItem("承認者JSONの場所を開く");
            miOpenApproverJson.Click += (_, __) =>
            {
                var p = ApproverStore.ResolveFilePath(cfg.RemoteBare);
                System.Diagnostics.Process.Start("explorer.exe", $"\"{Path.GetDirectoryName(p)}\"");
            };
            mConfig.DropDownItems.AddRange(new ToolStripItem[] { miEditCfg, miOpenRepo, miOpenApproverJson });

            var mHelp = new ToolStripMenuItem("ヘルプ");
            var miAbout = new ToolStripMenuItem("バージョン情報");
            miAbout.Click += (_, __) => MessageBox.Show("FlowLog\n軽量申請・承認ログツール", "About");
            mHelp.DropDownItems.Add(miAbout);

            menu.Items.AddRange(new ToolStripItem[] { mFile, mView, mConfig, mHelp });
            MainMenuStrip = menu;
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

        private void EditConfig()
        {
           using var setting = new ConfigSettingForm();
            var dr = setting.ShowDialog();
            if (dr != DialogResult.OK) return;
            var cfg = setting.ResultConfig;
            if (cfg is null) return;
            ConfigOps.Save(cfg);
            MessageBox.Show("設定を保存しました");
        }

        private void RefreshApproverSelector()
        {
            var list = ApproverStore.GetAll();
            cbApproverForCreate.BeginUpdate();
            cbApproverForCreate.Items.Clear();
            foreach (var a in list) cbApproverForCreate.Items.Add($"{a.Name} <{a.Email}>");
            cbApproverForCreate.EndUpdate();

            var path = ApproverStore.ResolveFilePath(ConfigOps.Load()!.RemoteBare);
            lblApproverStatus.Text = System.IO.File.Exists(path)
                ? $"承認者: {list.Count}名 読み込み済み ({path})"
                : $"承認者: 定義ファイル未検出 期待パス= {path}";

            if (cbApproverForCreate.Items.Count > 0 && cbApproverForCreate.SelectedIndex < 0)
                cbApproverForCreate.SelectedIndex = 0;
        }

        private sealed class ComboItem
        {
            public Approver Approver { get; }
            public ComboItem(Approver a) => Approver = a;
            public override string ToString() => $"{Approver.Name} <{Approver.Email}>";
        }

        // ---- Tabs ----

        TabPage BuildCreateTab()
        {
            var tab = new TabPage("申請");
            var tbTitle = new TextBox { Dock = DockStyle.Top, PlaceholderText = "タイトル" };
            var tbRequesterEmail = new TextBox { Dock = DockStyle.Top, PlaceholderText = "requester@example.co.jp" };
            var btn = new Button { Text = "申請作成→Push→通知", Dock = DockStyle.Bottom };

            btn.Click += (s, e) =>
            {
                var reqId = RequestOps.NewRequestId();
                var approverEmail = (cbApproverForCreate.SelectedItem as ComboItem)?.Approver.Email ?? cfg.Email;
                var requesterEmail = tbRequesterEmail.Text;

                RequestOps.CreatePendingJson(reqId, tbTitle.Text, requesterEmail, approverEmail, requesterEmail);

                // requesterEmail を CSV に保存
                var line = CsvLog.Line("CREATE", reqId, tbTitle.Text, requesterEmail, cfg.Actor, note: "", approver: approverEmail);
                CsvLog.AppendWithRetry(CsvLog.Header, line, cfg.Actor, cfg.Email);
                GitOps.CommitPush($"request: {reqId}", cfg.Actor, cfg.Email);

                MailUtil.SendSimple("smtp.example.co.jp", 25, "FlowLog", "no-reply@example.co.jp",
                    new[] { approverEmail }, $"[CREATE] {reqId}", $"申請: {reqId}\nタイトル: {tbTitle.Text}\n申請者: {requesterEmail}");
                MessageBox.Show($"Created: {reqId}");
            };

            tab.Controls.Add(btn);
            tab.Controls.Add(tbRequesterEmail);
            tab.Controls.Add(tbTitle);
            return tab;
        }

        TabPage BuildApproveTab()
        {
            var tab = new TabPage("承認/却下");

            var lv = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false };
            lv.Columns.Add("REQ-ID", 200);
            lv.Columns.Add("タイトル", 260);
            lv.Columns.Add("申請者", 140);
            lv.Columns.Add("作成日時", 180);
            lv.DoubleClick += (s, e) => ShowSelectedDetail(lv);

            var p = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true };
            var btnRefresh = new Button { Text = "更新" };
            var btnDetail  = new Button { Text = "詳細を見る" };
            var btnApprove = new Button { Text = "承認→Push→通知" };
            var btnReject  = new Button { Text = "却下→Push→通知" };
            var tbNote     = new TextBox { Width = 420, PlaceholderText = "メモ" };

            btnRefresh.Click += (s, e) => LoadPendingIntoList(lv);
            btnDetail.Click  += (s, e) => ShowSelectedDetail(lv);

            btnApprove.Click += (s, e) =>
            {
                if (lv.SelectedItems.Count == 0) return;
                var it = lv.SelectedItems[0];
                var reqId = (string)it.Tag;

                RequestOps.RemovePendingJson(reqId);
                var title = it.SubItems[1].Text;
                var requester = it.SubItems[2].Text;

                var line = CsvLog.Line("APPROVE", reqId, title, requester, cfg.Actor, tbNote.Text);
                CsvLog.AppendWithRetry(CsvLog.Header, line, cfg.Actor, cfg.Email);
                GitOps.CommitPush($"approve: {reqId}", cfg.Actor, cfg.Email);

                MailUtil.SendSimple("smtp.example.co.jp", 25, "FlowLog", "no-reply@example.co.jp",
                    new[] { cfg.Email }, $"[APPROVE] {reqId}", $"承認しました: {reqId}\nNote: {tbNote.Text}");

                btnRefresh.PerformClick();
            };

            btnReject.Click += (s, e) =>
            {
                if (lv.SelectedItems.Count == 0) return;
                var it = lv.SelectedItems[0];
                var reqId = (string)it.Tag;

                RequestOps.RemovePendingJson(reqId);
                var title = it.SubItems[1].Text;
                var requester = it.SubItems[2].Text;

                var line = CsvLog.Line("REJECT", reqId, title, requester, cfg.Actor, tbNote.Text);
                CsvLog.AppendWithRetry(CsvLog.Header, line, cfg.Actor, cfg.Email);
                GitOps.CommitPush($"reject: {reqId}", cfg.Actor, cfg.Email);

                MailUtil.SendSimple("smtp.example.co.jp", 25, "FlowLog", "no-reply@example.co.jp",
                    new[] { cfg.Email }, $"[REJECT] {reqId}", $"却下しました: {reqId}\nNote: {tbNote.Text}");

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

            LoadPendingIntoList(lv);
            return tab;
        }

        TabPage BuildSearchTab()
        {
            var tab = new TabPage("履歴検索");
            var tbQuery = new TextBox { Dock = DockStyle.Top, PlaceholderText = "req_id, actor, title を含む行をフィルタ" };
            var tbOut = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, Font = new System.Drawing.Font("Consolas", 10) };
            var btn = new Button { Text = "検索", Dock = DockStyle.Top };

            btn.Click += (s, e) =>
            {
                var yearDir = Path.Combine(Paths.LocalRepo, "logs", DateTime.Now.ToString("yyyy"));
                if (!Directory.Exists(yearDir)) { tbOut.Text = "(no logs)"; return; }

                var sb = new StringBuilder();
                foreach (var csv in Directory.GetFiles(yearDir, "*.csv"))
                {
                    foreach (var line in File.ReadLines(csv, new UTF8Encoding(false)))
                    {
                        if (line.StartsWith("at,")) continue;
                        if (string.IsNullOrWhiteSpace(tbQuery.Text) ||
                            line.Contains(tbQuery.Text, StringComparison.OrdinalIgnoreCase))
                        {
                            sb.AppendLine($"{Path.GetFileName(csv)}: {line}");
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

        private void LoadPendingIntoList(ListView lv)
        {
            lv.Items.Clear();
            var dir = Path.Combine(Paths.LocalRepo, "requests", "pending");
            Directory.CreateDirectory(dir);

            foreach (var f in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var raw = File.ReadAllText(f, new UTF8Encoding(false));
                    var dto = System.Text.Json.JsonSerializer.Deserialize<RequestDto>(raw);
                    if (dto is null || string.IsNullOrWhiteSpace(dto.Id)) continue;

                    var me = cfg.Email;
                    if (!string.Equals(dto.Approver, me, StringComparison.OrdinalIgnoreCase)) continue;

                    var created = dto.CreatedAt ?? "";
                    var it = new ListViewItem(new[] { dto.Id, dto.Title, dto.Requester, created }) { Tag = dto.Id };
                    lv.Items.Add(it);
                }
                catch {}
            }
        }

        private void ShowSelectedDetail(ListView lv)
        {
            if (lv.SelectedItems.Count == 0) return;
            var reqId = (string)lv.SelectedItems[0].Tag;
            var path = Path.Combine(Paths.LocalRepo, "requests", "pending", $"{reqId}.json");
            if (!File.Exists(path)) { MessageBox.Show("ファイルが見つかりません"); return; }

            var raw = File.ReadAllText(path, new UTF8Encoding(false));
            var dto = System.Text.Json.JsonSerializer.Deserialize<RequestDto>(raw) ?? new RequestDto { Id = reqId };
            using var dlg = new DetailForm(dto, raw);
            dlg.ShowDialog(this);
        }

        TabPage BuildMyTab()
        {
            var tab = new TabPage("申請状況/取消");

            var lv = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false };
            lv.Columns.Add("REQ-ID", 200);
            lv.Columns.Add("タイトル", 260);
            lv.Columns.Add("承認者", 220);
            lv.Columns.Add("作成日時", 180);

            var p = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true };
            var btnRefresh = new Button { Text = "更新" };
            var btnCancel = new Button { Text = "未承認を取消（CANCEL）" };
            var tbNote = new TextBox { Width = 420, PlaceholderText = "取消メモ（任意）" };

            btnRefresh.Click += (s, e) => LoadMyPending(lv);
            btnCancel.Click += (s, e) =>
            {
                if (lv.SelectedItems.Count == 0) return;
                var it = lv.SelectedItems[0];
                var reqId = (string)it.Tag;

                var dto = RequestOps.LoadPending(reqId);
                if (dto is null) { MessageBox.Show("pendingが見つかりません"); return; }

                // 取消は「承認前のみ」
                // （承認タブに残っていない＝まだPENDINGとみなす。必要ならCSV確認で二重防止を強化）
                var line = CsvLog.Line("CANCEL", reqId, dto.Title, dto.Requester, cfg.Actor, tbNote.Text, approver: dto.Approver);
                CsvLog.AppendWithRetry(CsvLog.Header, line, cfg.Actor, cfg.Email);

                RequestOps.RemovePendingJson(reqId);
                GitOps.CommitPush($"cancel: {reqId}", cfg.Actor, cfg.Email);

                MessageBox.Show("取消しました");
                btnRefresh.PerformClick();
            };

            p.Controls.Add(btnRefresh);
            p.Controls.Add(btnCancel);
            p.Controls.Add(new Label() { Text = "Note:" });
            p.Controls.Add(tbNote);

            tab.Controls.Add(lv);
            tab.Controls.Add(p);

            LoadMyPending(lv);
            return tab;

            void LoadMyPending(ListView list)
            {
                list.Items.Clear();
                var dir = Path.Combine(Paths.LocalRepo, "requests", "pending");
                Directory.CreateDirectory(dir);

                foreach (var f in Directory.GetFiles(dir, "*.json"))
                {
                    try
                    {
                        var raw = File.ReadAllText(f, new UTF8Encoding(false));
                        var dto = System.Text.Json.JsonSerializer.Deserialize<RequestDto>(raw);
                        if (dto is null || string.IsNullOrWhiteSpace(dto.Id)) continue;

                        // ★自分が申請者のPENDINGのみ
                        if (!string.Equals(dto.RequesterEmail, cfg.Email, StringComparison.OrdinalIgnoreCase)) continue;

                        var created = dto.CreatedAt ?? "";
                        var it = new ListViewItem(new[] { dto.Id, dto.Title, dto.Approver, created }) { Tag = dto.Id };
                        list.Items.Add(it);
                    }
                    catch { }
                }
            }
        }
    }
}