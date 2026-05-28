namespace VaR
{
    partial class LoginForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginForm));
            this.pass_textBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.vhod_button1 = new System.Windows.Forms.Button();
            this.login_textBox1 = new System.Windows.Forms.TextBox();
            this.viewPass_checkBox1 = new System.Windows.Forms.CheckBox();
            this.generate_button1 = new System.Windows.Forms.Button();
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            this.close_simpleButton1 = new DevExpress.XtraEditors.SimpleButton();
            this.titleLabel = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pass_textBox
            // 
            this.pass_textBox.Enabled = false;
            this.pass_textBox.Location = new System.Drawing.Point(89, 69);
            this.pass_textBox.Name = "pass_textBox";
            this.pass_textBox.Size = new System.Drawing.Size(266, 21);
            this.pass_textBox.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label2.Location = new System.Drawing.Point(12, 69);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 20);
            this.label2.TabIndex = 6;
            this.label2.Text = "Пароль";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label1.Location = new System.Drawing.Point(23, 37);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 20);
            this.label1.TabIndex = 7;
            this.label1.Text = "Логин";
            // 
            // vhod_button1
            // 
            this.vhod_button1.Enabled = false;
            this.vhod_button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.vhod_button1.Location = new System.Drawing.Point(89, 95);
            this.vhod_button1.Name = "vhod_button1";
            this.vhod_button1.Size = new System.Drawing.Size(115, 41);
            this.vhod_button1.TabIndex = 2;
            this.vhod_button1.Text = "Вход";
            this.vhod_button1.UseVisualStyleBackColor = true;
            this.vhod_button1.Click += new System.EventHandler(this.Login_button1_Click);
            // 
            // login_textBox1
            // 
            this.login_textBox1.Enabled = false;
            this.login_textBox1.Location = new System.Drawing.Point(89, 39);
            this.login_textBox1.Name = "login_textBox1";
            this.login_textBox1.Size = new System.Drawing.Size(266, 21);
            this.login_textBox1.TabIndex = 0;
            this.login_textBox1.TextChanged += new System.EventHandler(this.Login_textBox1_TextChanged);
            // 
            // viewPass_checkBox1
            // 
            this.viewPass_checkBox1.AutoSize = true;
            this.viewPass_checkBox1.Enabled = false;
            this.viewPass_checkBox1.Location = new System.Drawing.Point(241, 95);
            this.viewPass_checkBox1.Name = "viewPass_checkBox1";
            this.viewPass_checkBox1.Size = new System.Drawing.Size(113, 17);
            this.viewPass_checkBox1.TabIndex = 13;
            this.viewPass_checkBox1.Text = "Показать пароль";
            this.viewPass_checkBox1.UseVisualStyleBackColor = true;
            this.viewPass_checkBox1.CheckedChanged += new System.EventHandler(this.ViewPass_checkBox1_CheckedChanged);
            // 
            // generate_button1
            // 
            this.generate_button1.Location = new System.Drawing.Point(225, 113);
            this.generate_button1.Name = "generate_button1";
            this.generate_button1.Size = new System.Drawing.Size(130, 23);
            this.generate_button1.TabIndex = 14;
            this.generate_button1.Text = "Восстановить пароль";
            this.generate_button1.UseVisualStyleBackColor = true;
            this.generate_button1.Visible = false;
            this.generate_button1.Click += new System.EventHandler(this.Generate_button1_Click);
            // 
            // panelControl1
            // 
            this.panelControl1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.panelControl1.Controls.Add(this.close_simpleButton1);
            this.panelControl1.Controls.Add(this.titleLabel);
            this.panelControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelControl1.Location = new System.Drawing.Point(0, 0);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Size = new System.Drawing.Size(369, 30);
            this.panelControl1.TabIndex = 15;
            // 
            // close_simpleButton1
            // 
            this.close_simpleButton1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.close_simpleButton1.Dock = System.Windows.Forms.DockStyle.Right;
            this.close_simpleButton1.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("close_simpleButton1.ImageOptions.Image")));
            this.close_simpleButton1.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleCenter;
            this.close_simpleButton1.Location = new System.Drawing.Point(339, 0);
            this.close_simpleButton1.Name = "close_simpleButton1";
            this.close_simpleButton1.Size = new System.Drawing.Size(30, 30);
            this.close_simpleButton1.TabIndex = 16;
            this.close_simpleButton1.Click += new System.EventHandler(this.close_simpleButton1_Click);
            // 
            // titleLabel
            // 
            this.titleLabel.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.titleLabel.Appearance.Options.UseFont = true;
            this.titleLabel.Location = new System.Drawing.Point(10, 5);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(379, 19);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "Мой Самый Длинный Заголовок Внутри Окна";
            // 
            // LoginForm
            // 
            this.AcceptButton = this.vhod_button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.close_simpleButton1;
            this.ClientSize = new System.Drawing.Size(369, 152);
            this.Controls.Add(this.panelControl1);
            this.Controls.Add(this.generate_button1);
            this.Controls.Add(this.viewPass_checkBox1);
            this.Controls.Add(this.login_textBox1);
            this.Controls.Add(this.pass_textBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.vhod_button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.IconOptions.Icon = global::VaR.Properties.Resources._1511;
            this.IconOptions.Image = global::VaR.Properties.Resources._151;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(383, 159);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(369, 152);
            this.Name = "LoginForm";
            this.Text = " VaR";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LoginForm_FormClosing);
            this.Shown += new System.EventHandler(this.LoginForm_Shown);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.LoginForm_KeyPress);
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            this.panelControl1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox pass_textBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button vhod_button1;
        private System.Windows.Forms.TextBox login_textBox1;
        private System.Windows.Forms.CheckBox viewPass_checkBox1;
        private System.Windows.Forms.Button generate_button1;
        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraEditors.LabelControl titleLabel;
        private DevExpress.XtraEditors.SimpleButton close_simpleButton1;
    }
}