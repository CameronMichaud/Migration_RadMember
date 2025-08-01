using Migration_RadAdmin.Output;
using Migration_RadAdmin.Migration;
using System.Diagnostics;
using System.Security.Principal;

namespace Migration_RadAdmin
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            // Send instance of MainForm to the classes so they can manipulate the UI
            OutputManager.Initialize(this);
            MigrationManager.Initialize(this);

            // Start as admin if not already
            if (!Admin())
            {
                var funcInfo = new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(funcInfo);    // Start this program as admin
                Environment.Exit(0);        // Close old instance
            }

            // If started as kiosk, notify user that they should run as Pf_Admin or Radianse
            if (Environment.UserName.Equals("kiosk", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Current user is 'kiosk'.\nPlease run as Pf_Admin or Radianse, users will not be updated", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            startButton.Click += startButton_Click;
            stopButton.Click += stopButton_Click;
        }

        private async void Main()
        {
            // Removes C:\ProgramData\Radianse, old log files ~20-80 GB
            await MigrationManager.DeleteServiceCentral();

            // Rest of Migration
            await MigrationManager.InstallDotNets();
            await MigrationManager.InstallChrome();
            await MigrationManager.InstallSkyview();
            MigrationManager.SetStartup();
            await MigrationManager.UpdateUsers();
            MigrationManager.CompleteMigration();
        }

        private async void startButton_Click(object sender, EventArgs e)
        {
            startButton.Enabled = false;    // Grey out start migration during execution
            startButton.Text = "Migrating...";

            await Task.Run(() => Main()); // For some reason, lambda fixes this
        }
        private void stopButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        internal async Task RunTask(string title, ProgressBar bar, Func<Task> task)
        {
            OutputManager.setStatus($"{title}...");
            OutputManager.Log($"==={title}===");
            OutputManager.setProgress(bar, 25);

            await task();
            OutputManager.setProgress(bar, 100);
        }

        private bool Admin()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}