using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlowLog
{
    public partial class ConfigSettingForm : Form
    {

        public AppConfig? ResultConfig { get; private set; }

        public ConfigSettingForm(AppConfig? cfg = null)
        {
            InitializeComponent();
            this.ResultConfig = cfg;
        }

        private void ConfigSettingForm_Load(object sender, EventArgs e)
        {
            if(ResultConfig is not null)
            {
                tbActor.Text = ResultConfig.Actor;
                tbEmail.Text = ResultConfig.Email;
            }
            this.ActiveControl = btnOK;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbActor.Text))
            {
                MessageBox.Show("Actor を入力してください。", "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(tbEmail.Text) ||
                !tbEmail.Text.EndsWith("@example.co.jp", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Email は @example.co.jp ドメインで指定してください。", "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            ResultConfig = new AppConfig(@"\\nas\test.git", tbActor.Text, tbEmail.Text);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

    }
}
