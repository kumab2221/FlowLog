namespace FlowLog
{
    partial class ConfigSettingForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tbActor = new TextBox();
            tbEmail = new TextBox();
            btnOK = new Button();
            label1 = new Label();
            label2 = new Label();
            SuspendLayout();
            // 
            // tbActor
            // 
            tbActor.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbActor.Location = new Point(24, 34);
            tbActor.Name = "tbActor";
            tbActor.PlaceholderText = "山田 太郎";
            tbActor.Size = new Size(453, 23);
            tbActor.TabIndex = 0;
            // 
            // tbEmail
            // 
            tbEmail.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbEmail.Location = new Point(24, 93);
            tbEmail.Name = "tbEmail";
            tbEmail.PlaceholderText = "actor@example.co.jp";
            tbEmail.Size = new Size(453, 23);
            tbEmail.TabIndex = 1;
            // 
            // btnOK
            // 
            btnOK.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnOK.Location = new Point(402, 146);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 23);
            btnOK.TabIndex = 2;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(24, 16);
            label1.Name = "label1";
            label1.Size = new Size(31, 15);
            label1.TabIndex = 3;
            label1.Text = "氏名";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(24, 75);
            label2.Name = "label2";
            label2.Size = new Size(68, 15);
            label2.TabIndex = 4;
            label2.Text = "メールアドレス";
            // 
            // ConfigSettingForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(504, 190);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnOK);
            Controls.Add(tbEmail);
            Controls.Add(tbActor);
            Name = "ConfigSettingForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FlowLog 初期設定";
            Load += ConfigSettingForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox tbActor;
        private TextBox tbEmail;
        private Button btnOK;
        private Label label1;
        private Label label2;
    }
}