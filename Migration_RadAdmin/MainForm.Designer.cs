namespace Migration_RadAdmin
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            titleText = new Label();
            statusText = new Label();
            chromeProgress = new ProgressBar();
            chromeLabel = new Label();
            userProgress = new ProgressBar();
            userLabel = new Label();
            startButton = new Button();
            stopButton = new Button();
            outputBox = new TextBox();
            radianseLogo = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)radianseLogo).BeginInit();
            SuspendLayout();
            // 
            // titleText
            // 
            titleText.AutoSize = true;
            titleText.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point, 0);
            titleText.Location = new Point(2, 9);
            titleText.Margin = new Padding(0);
            titleText.Name = "titleText";
            titleText.Size = new Size(387, 45);
            titleText.TabIndex = 0;
            titleText.Text = "Radianse Migration Tool";
            titleText.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // statusText
            // 
            statusText.AutoSize = true;
            statusText.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            statusText.Location = new Point(7, 65);
            statusText.Margin = new Padding(0, 10, 0, 0);
            statusText.Name = "statusText";
            statusText.Size = new Size(262, 30);
            statusText.TabIndex = 1;
            statusText.Text = "Ready to begin migration...";
            statusText.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // chromeProgress
            // 
            chromeProgress.Location = new Point(15, 127);
            chromeProgress.Name = "chromeProgress";
            chromeProgress.Size = new Size(773, 23);
            chromeProgress.TabIndex = 5;
            // 
            // chromeLabel
            // 
            chromeLabel.AutoSize = true;
            chromeLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chromeLabel.Location = new Point(12, 107);
            chromeLabel.Margin = new Padding(0);
            chromeLabel.Name = "chromeLabel";
            chromeLabel.Size = new Size(91, 17);
            chromeLabel.TabIndex = 4;
            chromeLabel.Text = "Install Chrome";
            chromeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // userProgress
            // 
            userProgress.Location = new Point(15, 188);
            userProgress.Name = "userProgress";
            userProgress.Size = new Size(773, 23);
            userProgress.TabIndex = 9;
            // 
            // userLabel
            // 
            userLabel.AutoSize = true;
            userLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            userLabel.Location = new Point(12, 168);
            userLabel.Margin = new Padding(0);
            userLabel.Name = "userLabel";
            userLabel.Size = new Size(88, 17);
            userLabel.TabIndex = 8;
            userLabel.Text = "Update Users";
            userLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // startButton
            // 
            startButton.Location = new Point(610, 241);
            startButton.Name = "startButton";
            startButton.Size = new Size(143, 68);
            startButton.TabIndex = 10;
            startButton.Text = "Start Migration";
            startButton.UseVisualStyleBackColor = true;
            // 
            // stopButton
            // 
            stopButton.Location = new Point(610, 332);
            stopButton.Name = "stopButton";
            stopButton.Size = new Size(143, 68);
            stopButton.TabIndex = 11;
            stopButton.Text = "Exit";
            stopButton.UseVisualStyleBackColor = true;
            // 
            // outputBox
            // 
            outputBox.BackColor = SystemColors.ControlLightLight;
            outputBox.Location = new Point(15, 241);
            outputBox.Multiline = true;
            outputBox.Name = "outputBox";
            outputBox.ReadOnly = true;
            outputBox.ScrollBars = ScrollBars.Vertical;
            outputBox.Size = new Size(537, 159);
            outputBox.TabIndex = 12;
            outputBox.WordWrap = false;
            // 
            // radianseLogo
            // 
            radianseLogo.Image = (Image)resources.GetObject("radianseLogo.Image");
            radianseLogo.Location = new Point(658, 9);
            radianseLogo.Name = "radianseLogo";
            radianseLogo.Size = new Size(139, 32);
            radianseLogo.TabIndex = 13;
            radianseLogo.TabStop = false;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlLightLight;
            ClientSize = new Size(809, 427);
            Controls.Add(radianseLogo);
            Controls.Add(outputBox);
            Controls.Add(stopButton);
            Controls.Add(startButton);
            Controls.Add(userProgress);
            Controls.Add(userLabel);
            Controls.Add(chromeProgress);
            Controls.Add(chromeLabel);
            Controls.Add(statusText);
            Controls.Add(titleText);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "Radianse Migration Tool";
            ((System.ComponentModel.ISupportInitialize)radianseLogo).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        internal Label titleText;
        internal Label statusText;
        internal ProgressBar chromeProgress;
        internal Label chromeLabel;
        internal ProgressBar userProgress;
        internal Label userLabel;
        internal Button startButton;
        internal Button stopButton;
        internal TextBox outputBox;
        internal PictureBox radianseLogo;
    }
}
