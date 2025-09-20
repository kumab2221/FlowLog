namespace FlowLog
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            try
            {
                Paths.EnsureDirs();
                var cfg = ConfigOps.Load() ?? FirstRunSetup();
                if (cfg is null)
                {
                    MessageBox.Show("設定が未完了のため終了します。", "FlowLog", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // NAS の bare を初期化 / ローカルに自動クローン
                GitOps.EnsureInitialized(cfg.RemoteBare);

                Application.Run(new MainForm(cfg));
            }
            catch (Exception ex)
            {
                MessageBox.Show("起動時エラー: " + ex.Message, "FlowLog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static AppConfig? FirstRunSetup()
        {
            using var setting = new ConfigSettingForm();
            var dr = setting.ShowDialog();
            if (dr != DialogResult.OK) return null;
            var cfg = setting.ResultConfig;
            if (cfg is null) return null;
            ConfigOps.Save(cfg);
            return cfg;
        }
    }
}