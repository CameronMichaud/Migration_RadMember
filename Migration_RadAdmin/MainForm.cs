using Migration_RadAdmin.Installers;
using Migration_RadAdmin.Output;
using Migration_RadAdmin.Processes;
using Migration_RadAdmin.Users;
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

            // If started as kiosk, prevent deletion of the kiosk user
            if (Environment.UserName.Equals("kiosk", StringComparison.OrdinalIgnoreCase))
            {
                // Notify user that they should run as Pf_Admin or Radianse
                MessageBox.Show($"Current user is 'kiosk'.\nPlease run as Pf_Admin or Radianse, users will not be updated", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Buttons won't work without this, I'm unsure why
            startButton.Click += startButton_Click;
            stopButton.Click += stopButton_Click;
        }

        private async void Main()
        {
            string currentUser = Environment.UserName;

            InstallDotNets(currentUser);
            InstallChrome(currentUser);
            InstallSkyview();
            SetStartup();
            UpdateUsers(currentUser);
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

        private async Task RunFunction(string title, ProgressBar bar, Func<Task> task)
        {
            setStatus($"{title}...");
            OutputManager.Log($"==={title}===");
            setProgress(bar, 25);

            await task();
            setProgress(bar, 100);
        }

        private async void InstallDotNets(string currentUser)
        {
            await RunFunction("Installing .NET SDKs", dotnetProgress, async () =>
            {
                bool dotnet6 = InstallManager.DotNetInstalled("6");
                if (!dotnet6)
                {
                    // DOWNLOAD
                    OutputManager.Log("Dowloading .NET 6 (this may take a few minutes)");
                    string dotnet6URL = "https://builds.dotnet.microsoft.com/dotnet/Sdk/6.0.428/dotnet-sdk-6.0.428-win-x64.exe";
                    await InstallManager.GetInstaller(dotnet6URL, $@"C:\\users\{currentUser}\desktop\dotnet6.exe");

                    setProgress(dotnetProgress, 40);

                    // INSTALL
                    OutputManager.Log("Installing .NET 6 (this may take a few minutes)");
                    ProcessManager.RunTerminal("cmd.exe", $@"/c start C:\\users\{currentUser}\desktop\dotnet6.exe /quiet");
                    OutputManager.Log(".NET 6 install finished.\n");
                }
                else
                {
                    OutputManager.Log(".NET 6 already installed");
                }

                setProgress(dotnetProgress, 50);

                bool dotnet8 = InstallManager.DotNetInstalled("8");
                if (!dotnet8)
                {
                    // DOWNLOAD
                    OutputManager.Log("Dowloading .NET 8 (this may take a few minutes)");
                    string dotnet8URL = "https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.411/dotnet-sdk-8.0.411-win-x64.exe";
                    await InstallManager.GetInstaller(dotnet8URL, $@"C:\\users\{currentUser}\desktop\dotnet8.exe");

                    setProgress(dotnetProgress, 75);

                    // INSTALL
                    OutputManager.Log("Installing .NET 8 (this may take a few minutes)");
                    ProcessManager.RunTerminal("cmd.exe", $@"/c C:\\users\{currentUser}\desktop\dotnet8.exe /quiet");
                    OutputManager.Log(".NET 8 install finished.\n");
                }
                else
                {
                    OutputManager.Log(".NET 8 already installed");
                }
            });
        }
        private async void UpdateUsers(string currentUser)
        {
            await RunFunction("Updating Users", userProgress, async () =>
            {
                if (Environment.UserName.Equals("kiosk", StringComparison.OrdinalIgnoreCase))
                {
                    // Prevent deletion of the 'kiosk' user
                    OutputManager.Log("ERROR: Current user is 'kiosk'.");
                    MessageBox.Show($"Cannot update users.\nPlease run as Pf_Admin or Radianse", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    UserManager.DeleteUser("Kiosk");
                    setProgress(userProgress, 50);
                    UserManager.RemoveUserPassword(currentUser);
                    setProgress(userProgress, 75);
                    UserManager.RenameUser(currentUser, "Radianse");
                }
            });

            setStatus("Migration Complete!");
            startButton.Text = "Complete";

            //MessageBox.Show("Migration completed successfully.", "Migration Complete!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void InstallChrome(string currentUser)
        {
            await RunFunction("Installing Chrome", chromeProgress, async () =>
            {
                // Check if chrome exists or if there's a package manager, if so use them
                bool chrome = File.Exists(@"C:\Program Files\Google\Chrome\Application\chrome.exe");
                bool winget = InstallManager.IsInstalled("winget.exe", "--version");
                bool choco = InstallManager.IsInstalled("choco.exe", "-?");

                // If chrome does not exist, try a package manager, else grab directly form URL
                if (!chrome && winget)
                {
                    OutputManager.Log("Installing Chrome via winget (this may take a few minutes)");
                    ProcessManager.RunTerminal("powershell.exe", "winget install Google.Chrome --silent --accept-source-agreements --accept-package-agreements");
                }
                else if (!chrome && !winget && choco)
                {
                    OutputManager.Log("Installing Chrome via chocolatey (this may take a few minutes)");
                    ProcessManager.RunTerminal("powershell.exe", "choco install googlechrome -y");
                }
                else if (!chrome && !winget && !choco)
                {
                    // If there's no package manager, download the file
                    string chromeInstaller = @"https://dl.google.com/chrome/install/latest/chrome_installer.exe";
                    string filePath = $@"C:\\users\{currentUser}\desktop\chrome_installer.exe";
                    await InstallManager.GetInstaller(chromeInstaller, filePath);

                    setProgress(chromeProgress, 75);

                    // Install Chrome
                    OutputManager.Log("Installing Chrome (this may take a few minutes)");
                    ProcessManager.RunTerminal("cmd.exe", $"/c \"{filePath} /silent /install\"");
                }
                else
                {
                    OutputManager.Log("Chrome already installed");
                }
            });
        }

        private async void InstallSkyview()
        {
            await RunFunction("Installing Skyview Services", cleanProgress, async () =>
            {
                setProgress(cleanProgress, 50);

                // Install Skyview services, if the latest fails, try the previous version
                if (!InstallManager.InstallServices("skyview-services-3.0.375.msi"))
                {
                    InstallManager.InstallServices("skyview-services-3.0.365.msi");
                };
            });
        }

        private void SetStartup()
        {
            // INSTALL RADIANSE.IO AS APPLICATION:
            setStatus("Manual Action Required. (Install Radianse.io as app)");

            // Open radianse.io, run shell:startup
            ProcessManager.ConfigureChrome();
        }

        private void setProgress(ProgressBar bar, int percentage)
        {
            // Update given progress bar
            bar.Invoke((MethodInvoker)(() => bar.Value = percentage));
        }

        private void setStatus(string status)
        {
            // Update status text
            statusText.Invoke((MethodInvoker)(() => statusText.Text = status));
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