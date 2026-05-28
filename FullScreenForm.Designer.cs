
namespace VaR
{
    partial class FullScreenForm
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
            this.components = new System.ComponentModel.Container();
            this.panel1 = new System.Windows.Forms.Panel();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.восстановитьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.свернутьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.закрытьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.наДругойЭкранToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.одиндваЭкранаToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.min_button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.close_button2 = new System.Windows.Forms.Button();
            this.max_button1 = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.ContextMenuStrip = this.contextMenuStrip1;
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.min_button1);
            this.panel1.Controls.Add(this.max_button1);
            this.panel1.Controls.Add(this.close_button2);
            this.panel1.Location = new System.Drawing.Point(228, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(137, 20);
            this.panel1.TabIndex = 0;
            this.panel1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Panel1_MouseDown);
            this.panel1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Panel1_MouseMove);
            this.panel1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Panel1_MouseUp);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.восстановитьToolStripMenuItem,
            this.свернутьToolStripMenuItem,
            this.закрытьToolStripMenuItem,
            this.toolStripSeparator1,
            this.наДругойЭкранToolStripMenuItem,
            this.одиндваЭкранаToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(168, 120);
            // 
            // восстановитьToolStripMenuItem
            // 
            this.восстановитьToolStripMenuItem.Image = global::VaR.Properties.Resources.minimaze;
            this.восстановитьToolStripMenuItem.Name = "восстановитьToolStripMenuItem";
            this.восстановитьToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.восстановитьToolStripMenuItem.Text = "Восстановить";
            this.восстановитьToolStripMenuItem.Click += new System.EventHandler(this.ВосстановитьToolStripMenuItem_Click);
            // 
            // свернутьToolStripMenuItem
            // 
            this.свернутьToolStripMenuItem.Image = global::VaR.Properties.Resources.swipe;
            this.свернутьToolStripMenuItem.Name = "свернутьToolStripMenuItem";
            this.свернутьToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.свернутьToolStripMenuItem.Text = "Свернуть";
            this.свернутьToolStripMenuItem.Click += new System.EventHandler(this.СвернутьToolStripMenuItem_Click);
            // 
            // закрытьToolStripMenuItem
            // 
            this.закрытьToolStripMenuItem.Image = global::VaR.Properties.Resources.Close;
            this.закрытьToolStripMenuItem.Name = "закрытьToolStripMenuItem";
            this.закрытьToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.закрытьToolStripMenuItem.Text = "Закрыть";
            this.закрытьToolStripMenuItem.Click += new System.EventHandler(this.ЗакрытьToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(164, 6);
            // 
            // наДругойЭкранToolStripMenuItem
            // 
            this.наДругойЭкранToolStripMenuItem.Name = "наДругойЭкранToolStripMenuItem";
            this.наДругойЭкранToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.наДругойЭкранToolStripMenuItem.Text = "На другой экран";
            this.наДругойЭкранToolStripMenuItem.Click += new System.EventHandler(this.НаДругойЭкранToolStripMenuItem_Click);
            // 
            // одиндваЭкранаToolStripMenuItem
            // 
            this.одиндваЭкранаToolStripMenuItem.Name = "одиндваЭкранаToolStripMenuItem";
            this.одиндваЭкранаToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
            this.одиндваЭкранаToolStripMenuItem.Text = "Один/два экрана";
            this.одиндваЭкранаToolStripMenuItem.Click += new System.EventHandler(this.OneTwoScreenToolStripMenuItem_Click);
            // 
            // min_button1
            // 
            this.min_button1.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.min_button1.BackgroundImage = global::VaR.Properties.Resources.swipe;
            this.min_button1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.min_button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.min_button1.Location = new System.Drawing.Point(76, -1);
            this.min_button1.Margin = new System.Windows.Forms.Padding(0);
            this.min_button1.Name = "min_button1";
            this.min_button1.Size = new System.Drawing.Size(20, 20);
            this.min_button1.TabIndex = 3;
            this.min_button1.UseVisualStyleBackColor = true;
            this.min_button1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Min_button1_MouseDown);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 16);
            this.label1.TabIndex = 2;
            this.label1.Text = "Сервер";
            this.label1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Label1_MouseDown);
            this.label1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Label1_MouseMove);
            this.label1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Label1_MouseUp);
            // 
            // close_button2
            // 
            this.close_button2.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.close_button2.BackgroundImage = global::VaR.Properties.Resources.Close;
            this.close_button2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.close_button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.close_button2.Location = new System.Drawing.Point(116, -1);
            this.close_button2.Margin = new System.Windows.Forms.Padding(0);
            this.close_button2.Name = "close_button2";
            this.close_button2.Size = new System.Drawing.Size(20, 20);
            this.close_button2.TabIndex = 1;
            this.close_button2.UseVisualStyleBackColor = true;
            this.close_button2.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Close_button2_MouseDown);
            // 
            // max_button1
            // 
            this.max_button1.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.max_button1.BackgroundImage = global::VaR.Properties.Resources.minimaze;
            this.max_button1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.max_button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.max_button1.Location = new System.Drawing.Point(96, -1);
            this.max_button1.Margin = new System.Windows.Forms.Padding(0);
            this.max_button1.Name = "max_button1";
            this.max_button1.Size = new System.Drawing.Size(20, 20);
            this.max_button1.TabIndex = 0;
            this.max_button1.UseVisualStyleBackColor = true;
            this.max_button1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Max_button1_MouseDown);
            // 
            // FullScreenForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(691, 156);
            this.ControlBox = false;
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = global::VaR.Properties.Resources._1511;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FullScreenForm";
            this.Text = " ";
            this.Shown += new System.EventHandler(this.FullScreenForm_Shown);
            this.Resize += new System.EventHandler(this.FullScreenForm_Resize);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button close_button2;
        private System.Windows.Forms.Button max_button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button min_button1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem восстановитьToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem свернутьToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem закрытьToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem наДругойЭкранToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem одиндваЭкранаToolStripMenuItem;
    }
}