namespace KUpdater
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
            panel_main = new Panel();
            border_bottom_right = new Panel();
            border_top_center = new Panel();
            border_top_right = new Panel();
            border_right_center = new Panel();
            border_bottom_center = new Panel();
            border_bottom_left = new Panel();
            border_left_center = new Panel();
            panel_top_left = new Panel();
            panel_main.SuspendLayout();
            SuspendLayout();
            // 
            // panel_main
            // 
            panel_main.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel_main.BackColor = Color.Black;
            panel_main.BackgroundImageLayout = ImageLayout.None;
            panel_main.Controls.Add(border_bottom_right);
            panel_main.Controls.Add(border_top_center);
            panel_main.Controls.Add(border_top_right);
            panel_main.Controls.Add(border_right_center);
            panel_main.Controls.Add(border_bottom_center);
            panel_main.Controls.Add(border_bottom_left);
            panel_main.Controls.Add(border_left_center);
            panel_main.Controls.Add(panel_top_left);
            panel_main.Dock = DockStyle.Fill;
            panel_main.Location = new Point(0, 0);
            panel_main.Margin = new Padding(0);
            panel_main.Name = "panel_main";
            panel_main.Size = new Size(850, 500);
            panel_main.TabIndex = 0;
            // 
            // border_bottom_right
            // 
            border_bottom_right.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            border_bottom_right.BackColor = Color.Transparent;
            border_bottom_right.BackgroundImage = Properties.Resources.border_bottom_right;
            border_bottom_right.BackgroundImageLayout = ImageLayout.None;
            border_bottom_right.Location = new Point(820, 436);
            border_bottom_right.Name = "border_bottom_right";
            border_bottom_right.Size = new Size(30, 64);
            border_bottom_right.TabIndex = 4;
            // 
            // border_top_center
            // 
            border_top_center.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            border_top_center.BackColor = Color.Transparent;
            border_top_center.BackgroundImage = Properties.Resources.border_top_center;
            border_top_center.Location = new Point(74, 0);
            border_top_center.Name = "border_top_center";
            border_top_center.Size = new Size(727, 50);
            border_top_center.TabIndex = 7;
            // 
            // border_top_right
            // 
            border_top_right.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            border_top_right.BackColor = Color.Transparent;
            border_top_right.BackgroundImage = Properties.Resources.border_top_right;
            border_top_right.BackgroundImageLayout = ImageLayout.None;
            border_top_right.Location = new Point(801, 0);
            border_top_right.Name = "border_top_right";
            border_top_right.Size = new Size(49, 84);
            border_top_right.TabIndex = 6;
            // 
            // border_right_center
            // 
            border_right_center.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            border_right_center.BackColor = Color.Transparent;
            border_right_center.BackgroundImage = Properties.Resources.border_right_center;
            border_right_center.Location = new Point(821, 84);
            border_right_center.Name = "border_right_center";
            border_right_center.Size = new Size(29, 355);
            border_right_center.TabIndex = 5;
            // 
            // border_bottom_center
            // 
            border_bottom_center.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            border_bottom_center.BackColor = Color.Transparent;
            border_bottom_center.BackgroundImage = Properties.Resources.border_bottom_center;
            border_bottom_center.Location = new Point(30, 472);
            border_bottom_center.Name = "border_bottom_center";
            border_bottom_center.Size = new Size(790, 28);
            border_bottom_center.TabIndex = 3;
            // 
            // border_bottom_left
            // 
            border_bottom_left.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            border_bottom_left.BackColor = Color.Transparent;
            border_bottom_left.BackgroundImage = Properties.Resources.border_bottom_left;
            border_bottom_left.BackgroundImageLayout = ImageLayout.None;
            border_bottom_left.Location = new Point(0, 436);
            border_bottom_left.Name = "border_bottom_left";
            border_bottom_left.Size = new Size(30, 64);
            border_bottom_left.TabIndex = 2;
            // 
            // border_left_center
            // 
            border_left_center.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            border_left_center.BackColor = Color.Transparent;
            border_left_center.BackgroundImage = Properties.Resources.border_left_center;
            border_left_center.Location = new Point(0, 84);
            border_left_center.Name = "border_left_center";
            border_left_center.Size = new Size(31, 355);
            border_left_center.TabIndex = 1;
            // 
            // panel_top_left
            // 
            panel_top_left.BackColor = Color.Transparent;
            panel_top_left.BackgroundImage = Properties.Resources.border_top_left;
            panel_top_left.BackgroundImageLayout = ImageLayout.None;
            panel_top_left.Location = new Point(0, 0);
            panel_top_left.Margin = new Padding(0);
            panel_top_left.Name = "panel_top_left";
            panel_top_left.Size = new Size(74, 84);
            panel_top_left.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Magenta;
            ClientSize = new Size(850, 500);
            Controls.Add(panel_main);
            FormBorderStyle = FormBorderStyle.None;
            Name = "Form1";
            Text = "Form1";
            TransparencyKey = Color.Magenta;
            panel_main.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panel_main;
        private Panel panel_top_left;
        private Panel border_bottom_left;
        private Panel border_left_center;
        private Panel border_bottom_center;
        private Panel border_right_center;
        private Panel border_bottom_right;
        private Panel border_top_center;
        private Panel border_top_right;
    }
}
