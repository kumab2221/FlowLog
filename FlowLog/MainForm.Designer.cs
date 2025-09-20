namespace FlowLog
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            menu = new MenuStrip();
            SuspendLayout();
            // 
            // menu
            // 
            menu.Location = new Point(0, 0);
            menu.Name = "menu";
            menu.Size = new Size(964, 24);
            menu.TabIndex = 0;
            menu.Text = "menuStrip1";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(964, 601);
            Controls.Add(menu);
            MainMenuStrip = menu;
            Name = "MainForm";
            Text = "FlowLog";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menu;
    }
}
