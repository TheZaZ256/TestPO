namespace TestPO
{
    partial class TestPO
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
            components = new System.ComponentModel.Container();
            btn_choose_programm = new Button();
            txtFilePath = new TextBox();
            label1 = new Label();
            btn_start_analyze = new Button();
            txt_about_po = new TextBox();
            checkBox_Decompile = new CheckBox();
            label2 = new Label();
            label3 = new Label();
            checkBox_Defender = new CheckBox();
            btn_start_test = new Button();
            label4 = new Label();
            label5 = new Label();
            txtLog = new TextBox();
            label6 = new Label();
            checkBox_gui = new CheckBox();
            checkBox_DotNet = new CheckBox();
            checkBox_Debug = new CheckBox();
            checkBox_Admin = new CheckBox();
            checkBox_Unity = new CheckBox();
            memoryTimer = new System.Windows.Forms.Timer(components);
            lblMemory = new Label();
            label7 = new Label();
            btn_send_command = new Button();
            txt_Input = new TextBox();
            label8 = new Label();
            btnLoadTextAndSend = new Button();
            btn_make_dump = new Button();
            SuspendLayout();
            // 
            // btn_choose_programm
            // 
            btn_choose_programm.Location = new Point(694, 12);
            btn_choose_programm.Name = "btn_choose_programm";
            btn_choose_programm.Size = new Size(94, 29);
            btn_choose_programm.TabIndex = 0;
            btn_choose_programm.Text = "Выбрать программу";
            btn_choose_programm.UseVisualStyleBackColor = true;
            btn_choose_programm.Click += btn_choose_programm_Click;
            // 
            // txtFilePath
            // 
            txtFilePath.Location = new Point(12, 51);
            txtFilePath.Name = "txtFilePath";
            txtFilePath.ReadOnly = true;
            txtFilePath.Size = new Size(776, 27);
            txtFilePath.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 12);
            label1.Name = "label1";
            label1.Size = new Size(106, 20);
            label1.TabIndex = 2;
            label1.Text = "Выбран файл:";
            // 
            // btn_start_analyze
            // 
            btn_start_analyze.Enabled = false;
            btn_start_analyze.Location = new Point(577, 99);
            btn_start_analyze.Name = "btn_start_analyze";
            btn_start_analyze.Size = new Size(211, 29);
            btn_start_analyze.TabIndex = 3;
            btn_start_analyze.Text = "Проанализировать";
            btn_start_analyze.UseVisualStyleBackColor = true;
            btn_start_analyze.Click += btn_start_analyze_Click;
            // 
            // txt_about_po
            // 
            txt_about_po.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            txt_about_po.Location = new Point(12, 217);
            txt_about_po.Multiline = true;
            txt_about_po.Name = "txt_about_po";
            txt_about_po.ReadOnly = true;
            txt_about_po.ScrollBars = ScrollBars.Vertical;
            txt_about_po.Size = new Size(776, 221);
            txt_about_po.TabIndex = 4;
            // 
            // checkBox_Decompile
            // 
            checkBox_Decompile.AutoSize = true;
            checkBox_Decompile.Location = new Point(223, 99);
            checkBox_Decompile.Name = "checkBox_Decompile";
            checkBox_Decompile.Size = new Size(49, 24);
            checkBox_Decompile.TabIndex = 5;
            checkBox_Decompile.Text = "Да";
            checkBox_Decompile.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 99);
            label2.Name = "label2";
            label2.Size = new Size(205, 20);
            label2.TabIndex = 6;
            label2.Text = "Декомпилировать, если C#?";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(315, 98);
            label3.Name = "label3";
            label3.Size = new Size(183, 20);
            label3.TabIndex = 8;
            label3.Text = "Проверять защитником?";
            // 
            // checkBox_Defender
            // 
            checkBox_Defender.AutoSize = true;
            checkBox_Defender.Location = new Point(504, 99);
            checkBox_Defender.Name = "checkBox_Defender";
            checkBox_Defender.Size = new Size(49, 24);
            checkBox_Defender.TabIndex = 7;
            checkBox_Defender.Text = "Да";
            checkBox_Defender.UseVisualStyleBackColor = true;
            // 
            // btn_start_test
            // 
            btn_start_test.Enabled = false;
            btn_start_test.Location = new Point(826, 32);
            btn_start_test.Name = "btn_start_test";
            btn_start_test.Size = new Size(538, 86);
            btn_start_test.TabIndex = 9;
            btn_start_test.Text = "Начать тестирование";
            btn_start_test.UseVisualStyleBackColor = true;
            btn_start_test.Click += btn_start_test_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 131);
            label4.Name = "label4";
            label4.Size = new Size(112, 20);
            label4.TabIndex = 10;
            label4.Text = "Отчет анализа:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 204);
            label5.Location = new Point(12, 151);
            label5.Name = "label5";
            label5.Size = new Size(527, 17);
            label5.TabIndex = 11;
            label5.Text = "*Отчет анализа, и декомпилированные исходники сохраняются в папку с программой";
            // 
            // txtLog
            // 
            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.BackColor = SystemColors.ButtonFace;
            txtLog.Location = new Point(826, 217);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(538, 221);
            txtLog.TabIndex = 12;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(12, 187);
            label6.Name = "label6";
            label6.Size = new Size(247, 20);
            label6.TabIndex = 13;
            label6.Text = "Ключевые данные после анализа:";
            // 
            // checkBox_gui
            // 
            checkBox_gui.AutoSize = true;
            checkBox_gui.Location = new Point(265, 187);
            checkBox_gui.Name = "checkBox_gui";
            checkBox_gui.Size = new Size(55, 24);
            checkBox_gui.TabIndex = 15;
            checkBox_gui.Text = "GUI";
            checkBox_gui.UseVisualStyleBackColor = true;
            // 
            // checkBox_DotNet
            // 
            checkBox_DotNet.AutoSize = true;
            checkBox_DotNet.Location = new Point(326, 187);
            checkBox_DotNet.Name = "checkBox_DotNet";
            checkBox_DotNet.Size = new Size(90, 24);
            checkBox_DotNet.TabIndex = 16;
            checkBox_DotNet.Text = "IsDotNet";
            checkBox_DotNet.UseVisualStyleBackColor = true;
            // 
            // checkBox_Debug
            // 
            checkBox_Debug.AutoSize = true;
            checkBox_Debug.Location = new Point(422, 187);
            checkBox_Debug.Name = "checkBox_Debug";
            checkBox_Debug.Size = new Size(120, 24);
            checkBox_Debug.TabIndex = 17;
            checkBox_Debug.Text = "IsDebugBuild";
            checkBox_Debug.UseVisualStyleBackColor = true;
            // 
            // checkBox_Admin
            // 
            checkBox_Admin.AutoSize = true;
            checkBox_Admin.Location = new Point(548, 187);
            checkBox_Admin.Name = "checkBox_Admin";
            checkBox_Admin.Size = new Size(85, 24);
            checkBox_Admin.TabIndex = 18;
            checkBox_Admin.Text = "IsAdmin";
            checkBox_Admin.UseVisualStyleBackColor = true;
            // 
            // checkBox_Unity
            // 
            checkBox_Unity.AutoSize = true;
            checkBox_Unity.Location = new Point(639, 187);
            checkBox_Unity.Name = "checkBox_Unity";
            checkBox_Unity.Size = new Size(75, 24);
            checkBox_Unity.TabIndex = 19;
            checkBox_Unity.Text = "IsUnity";
            checkBox_Unity.UseVisualStyleBackColor = true;
            // 
            // lblMemory
            // 
            lblMemory.AutoSize = true;
            lblMemory.Location = new Point(826, 128);
            lblMemory.Name = "lblMemory";
            lblMemory.Size = new Size(82, 20);
            lblMemory.TabIndex = 20;
            lblMemory.Text = "RAM: 0 MB";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(826, 154);
            label7.Name = "label7";
            label7.Size = new Size(93, 20);
            label7.TabIndex = 21;
            label7.Text = "Лог работы:";
            // 
            // btn_send_command
            // 
            btn_send_command.Enabled = false;
            btn_send_command.Location = new Point(1270, 183);
            btn_send_command.Name = "btn_send_command";
            btn_send_command.Size = new Size(94, 29);
            btn_send_command.TabIndex = 22;
            btn_send_command.Text = "Отправить текст";
            btn_send_command.UseVisualStyleBackColor = true;
            btn_send_command.Click += btn_send_command_Click;
            // 
            // txt_Input
            // 
            txt_Input.Location = new Point(826, 180);
            txt_Input.Name = "txt_Input";
            txt_Input.Size = new Size(228, 27);
            txt_Input.TabIndex = 23;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(826, 9);
            label8.Name = "label8";
            label8.Size = new Size(311, 20);
            label8.TabIndex = 24;
            label8.Text = "*Заблокировано, пока не проведен анализ";
            // 
            // btnLoadTextAndSend
            // 
            btnLoadTextAndSend.Location = new Point(1070, 183);
            btnLoadTextAndSend.Name = "btnLoadTextAndSend";
            btnLoadTextAndSend.Size = new Size(191, 28);
            btnLoadTextAndSend.TabIndex = 25;
            btnLoadTextAndSend.Text = "Загрузить текст команд";
            btnLoadTextAndSend.UseVisualStyleBackColor = true;
            btnLoadTextAndSend.Click += btnLoadTextAndSend_Click;
            // 
            // btn_make_dump
            // 
            btn_make_dump.Location = new Point(1270, 131);
            btn_make_dump.Name = "btn_make_dump";
            btn_make_dump.Size = new Size(94, 29);
            btn_make_dump.TabIndex = 26;
            btn_make_dump.Text = "ДАМП";
            btn_make_dump.UseVisualStyleBackColor = true;
            btn_make_dump.Click += btn_make_dump_Click;
            // 
            // TestPO
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlDark;
            ClientSize = new Size(1376, 450);
            Controls.Add(btn_make_dump);
            Controls.Add(btnLoadTextAndSend);
            Controls.Add(label8);
            Controls.Add(txt_Input);
            Controls.Add(btn_send_command);
            Controls.Add(label7);
            Controls.Add(lblMemory);
            Controls.Add(checkBox_Unity);
            Controls.Add(checkBox_Admin);
            Controls.Add(checkBox_Debug);
            Controls.Add(checkBox_DotNet);
            Controls.Add(checkBox_gui);
            Controls.Add(label6);
            Controls.Add(txtLog);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(btn_start_test);
            Controls.Add(label3);
            Controls.Add(checkBox_Defender);
            Controls.Add(label2);
            Controls.Add(checkBox_Decompile);
            Controls.Add(txt_about_po);
            Controls.Add(btn_start_analyze);
            Controls.Add(label1);
            Controls.Add(txtFilePath);
            Controls.Add(btn_choose_programm);
            Name = "TestPO";
            Text = "Тестирование п/о";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btn_choose_programm;
        private TextBox txtFilePath;
        private Label label1;
        private Button btn_start_analyze;
        private TextBox txt_about_po;
        private CheckBox checkBox_Decompile;
        private Label label2;
        private Label label3;
        private CheckBox checkBox_Defender;
        private Button btn_start_test;
        private Label label4;
        private Label label5;
        private TextBox txtLog;
        private Label label6;
        private CheckBox checkBox_gui;
        private CheckBox checkBox_DotNet;
        private CheckBox checkBox_Debug;
        private CheckBox checkBox_Admin;
        public CheckBox checkBox_Unity;
        private System.Windows.Forms.Timer memoryTimer;
        private Label lblMemory;
        private Label label7;
        private Button btn_send_command;
        private TextBox txt_Input;
        private Label label8;
        private Button btnLoadTextAndSend;
        private Button btn_make_dump;
    }
}
