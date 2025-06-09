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
            dotnetLabel = new Label();
            dotnetProgress = new ProgressBar();
            chromeProgress = new ProgressBar();
            chromeLabel = new Label();
            userProgress = new ProgressBar();
            userLabel = new Label();
            cleanProgress = new ProgressBar();
            servicesLabel = new Label();
            startButton = new Button();
            stopButton = new Button();
            outputBox = new TextBox();
            pictureBox1 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
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
            // dotnetLabel
            // 
            dotnetLabel.AutoSize = true;
            dotnetLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dotnetLabel.Location = new Point(9, 104);
            dotnetLabel.Margin = new Padding(0);
            dotnetLabel.Name = "dotnetLabel";
            dotnetLabel.Size = new Size(106, 17);
            dotnetLabel.TabIndex = 2;
            dotnetLabel.Text = "Install .NET SDKs";
            dotnetLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // dotnetProgress
            // 
            dotnetProgress.Location = new Point(12, 124);
            dotnetProgress.Name = "dotnetProgress";
            dotnetProgress.Size = new Size(773, 23);
            dotnetProgress.TabIndex = 3;
            // 
            // chromeProgress
            // 
            chromeProgress.Location = new Point(12, 191);
            chromeProgress.Name = "chromeProgress";
            chromeProgress.Size = new Size(773, 23);
            chromeProgress.TabIndex = 5;
            // 
            // chromeLabel
            // 
            chromeLabel.AutoSize = true;
            chromeLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chromeLabel.Location = new Point(9, 171);
            chromeLabel.Margin = new Padding(0);
            chromeLabel.Name = "chromeLabel";
            chromeLabel.Size = new Size(91, 17);
            chromeLabel.TabIndex = 4;
            chromeLabel.Text = "Install Chrome";
            chromeLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // userProgress
            // 
            userProgress.Location = new Point(12, 259);
            userProgress.Name = "userProgress";
            userProgress.Size = new Size(773, 23);
            userProgress.TabIndex = 7;
            // 
            // userLabel
            // 
            userLabel.AutoSize = true;
            userLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            userLabel.Location = new Point(9, 239);
            userLabel.Margin = new Padding(0);
            userLabel.Name = "userLabel";
            userLabel.Size = new Size(88, 17);
            userLabel.TabIndex = 6;
            userLabel.Text = "Update Users";
            userLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cleanProgress
            // 
            cleanProgress.Location = new Point(12, 326);
            cleanProgress.Name = "cleanProgress";
            cleanProgress.Size = new Size(773, 23);
            cleanProgress.TabIndex = 9;
            // 
            // servicesLabel
            // 
            servicesLabel.AutoSize = true;
            servicesLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            servicesLabel.Location = new Point(9, 306);
            servicesLabel.Margin = new Padding(0);
            servicesLabel.Name = "servicesLabel";
            servicesLabel.Size = new Size(163, 17);
            servicesLabel.TabIndex = 8;
            servicesLabel.Text = "Remove Radianse Services";
            servicesLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // startButton
            // 
            startButton.Location = new Point(610, 371);
            startButton.Name = "startButton";
            startButton.Size = new Size(143, 68);
            startButton.TabIndex = 10;
            startButton.Text = "Start Migration";
            startButton.UseVisualStyleBackColor = true;
            // 
            // stopButton
            // 
            stopButton.Location = new Point(610, 462);
            stopButton.Name = "stopButton";
            stopButton.Size = new Size(143, 68);
            stopButton.TabIndex = 11;
            stopButton.Text = "Exit";
            stopButton.UseVisualStyleBackColor = true;
            // 
            // outputBox
            // 
            outputBox.Location = new Point(15, 371);
            outputBox.Multiline = true;
            outputBox.Name = "outputBox";
            outputBox.ReadOnly = true;
            outputBox.ScrollBars = ScrollBars.Vertical;
            outputBox.Size = new Size(537, 159);
            outputBox.TabIndex = 12;
            outputBox.WordWrap = false;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(658, 9);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(139, 32);
            pictureBox1.TabIndex = 13;
            pictureBox1.TabStop = false;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(809, 542);
            Controls.Add(pictureBox1);
            Controls.Add(outputBox);
            Controls.Add(stopButton);
            Controls.Add(startButton);
            Controls.Add(cleanProgress);
            Controls.Add(servicesLabel);
            Controls.Add(userProgress);
            Controls.Add(userLabel);
            Controls.Add(chromeProgress);
            Controls.Add(chromeLabel);
            Controls.Add(dotnetProgress);
            Controls.Add(dotnetLabel);
            Controls.Add(statusText);
            Controls.Add(titleText);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "Radianse Migration Tool";
            TopMost = true;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label titleText;
        private Label statusText;
        private Label dotnetLabel;
        private ProgressBar dotnetProgress;
        private ProgressBar chromeProgress;
        private Label chromeLabel;
        private ProgressBar userProgress;
        private Label userLabel;
        private ProgressBar cleanProgress;
        private Label servicesLabel;
        private Button startButton;
        private Button stopButton;
        private TextBox outputBox;
        private PictureBox pictureBox1;
    }
}
