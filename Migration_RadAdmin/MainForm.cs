using Microsoft.VisualBasic.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using static System.Formats.Asn1.AsnWriter;
using System.Net.Http;

namespace Migration_RadAdmin
{
    public partial class MainForm : Form
    {
        // Used to track progress of the migration when disabling the start button
        private static int MigrationState = 0; // 0 = not started, 1 = chrome installed
        public MainForm()
        {
            InitializeComponent();

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

            // Buttons won't work without this, I'm unsure why
            startButton.Click += startButton_Click;
            stopButton.Click += stopButton_Click;
        }

        private async void startButton_Click(object sender, EventArgs e)
        {
            startButton.Enabled = false;    // Grey out start migration during execution
            startButton.Text = "Migrating...";
            string currentUser = Environment.UserName;

            await Task.Run(async () =>
            {
                if (MigrationState == 0)
                {
                    await RunFunction("Installing .NET SDKs", dotnetProgress, async () =>
                    {
                        bool dotnet6 = DotNetInstalled("6");
                        if (!dotnet6)
                        {
                            // DOWNLOAD
                            Log("Dowloading .NET 6 (this may take a few minutes)");
                            string dotnet6URL = "https://builds.dotnet.microsoft.com/dotnet/Sdk/6.0.428/dotnet-sdk-6.0.428-win-x64.exe";
                            await GetInstaller(dotnet6URL, $@"C:\\users\{currentUser}\desktop\dotnet6.exe");

                            // INSTALL
                            Log("Installing .NET 6 (this may take a few minutes)");
                            RunTerminal("cmd.exe", $@"/c start C:\\users\{currentUser}\desktop\dotnet6.exe /quiet");
                            Log(".NET 6 install finished.\n");
                        }
                        else
                        {
                            Log(".NET 6 already installed");
                        }

                        setProgress(dotnetProgress, 50);

                        bool dotnet8 = DotNetInstalled("8");
                        if (!dotnet8)
                        {
                            // DOWNLOAD
                            Log("Dowloading .NET 8 (this may take a few minutes)");
                            string dotnet8URL = "https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.411/dotnet-sdk-8.0.411-win-x64.exe";
                            await GetInstaller(dotnet8URL, $@"C:\\users\{currentUser}\desktop\dotnet8.exe");

                            // INSTALL
                            Log("Installing .NET 8 (this may take a few minutes)");
                            RunTerminal("cmd.exe", $@"/c C:\\users\{currentUser}\desktop\dotnet8.exe /quiet");
                            Log(".NET 8 install finished.\n");
                        }
                        else
                        {
                            Log(".NET 8 already installed");
                        }
                    });

                    await RunFunction("Installing Chrome", chromeProgress, async () =>
                    {
                        bool chrome = File.Exists(@"C:\Program Files\Google\Chrome\Application\chrome.exe");
                        bool winget = IsInstalled("winget.exe", "--version");
                        bool choco = IsInstalled("choco.exe", "-?");

                        if (!chrome && winget)
                        {
                            Log("Installing Chrome via winget (this may take a few minutes)");
                            RunTerminal("powershell.exe", "winget install Google.Chrome --silent --accept-source-agreements --accept-package-agreements");
                        }
                        else if (!chrome && !winget && choco)
                        {
                            Log("Installing Chrome via chocolatey (this may take a few minutes)");
                            RunTerminal("powershell.exe", "choco install googlechrome -y");
                        }
                        else if (!chrome && !winget && !choco)
                        {
                            string chromeInstaller = @"https://dl.google.com/chrome/install/latest/chrome_installer.exe";
                            string filePath = $@"C:\\users\{currentUser}\desktop\chrome_installer.exe";
                            await GetInstaller(chromeInstaller, filePath);
                            Log("Installing Chrome (this may take a few minutes)");
                            RunTerminal("cmd.exe", $"/c \"{filePath} /silent /install\"");
                        }
                        else
                        {
                            Log("Chrome already installed");
                        }
                    });

                    await RunFunction("Removing Local Services", cleanProgress, async () =>
                    {
                        RemoveAll(cleanProgress);
                        setStatus("Manual Action Required. (Remove Local Services)");
                        MigrationState = 1; // Set migration state to 1 (remove services)
                        startButton.Text = "Continue Migration";
                        startButton.Enabled = true;
                        // MessageBox.Show("Please remove local services", "Manual action required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                }

                else if (MigrationState == 1)
                {
                    await RunFunction("Installing Skyview Services", cleanProgress, async () =>
                    {
                        setProgress(cleanProgress, 50);
                        InstallServices("skyview-services-3.0.367.msi");
                    });

                    // INSTALL RADIANSE.IO AS APPLICATION:
                    setStatus("Manual Action Required. (Install Radianse.io as app)");

                    ConfigureChrome();  // Open radianse.io, run shell:startup

                    MigrationState = 2; // Set migration state to 2 (last step is user management)
                    startButton.Text = "Continue Migration";
                    startButton.Enabled = true;

                    // MessageBox.Show("Please install Radainse as an app.", "Manual action required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    await RunFunction("Updating Users", userProgress, async () =>
                    {
                        DeleteUser("Kiosk");
                        setProgress(userProgress, 50);
                        RemoveUserPassword(currentUser);
                        setProgress(userProgress, 75);
                        RenameUser(currentUser, "Radianse");
                    });

                    setStatus("Migration Complete!");

                    MessageBox.Show("Migration completed successfully.", "Migration Complete!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
        }

        private async Task GetInstaller(string url, string filePath)
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs);

            Log($"{filePath} download complete!");
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async Task RunFunction(string title, ProgressBar bar, Func<Task> action)
        {
            setStatus($"{title}...");
            Log($"==={title}===");
            setProgress(bar, 25);

            await action();
            setProgress(bar, 100);
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

        private void RunTerminal(string command, string args)
        {
            try
            {
                ProcessStartInfo funcInfo = new ProcessStartInfo(command, args)
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                // Start the program/args
                var process = Process.Start(funcInfo);

                // While there's still output, read line
                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();
                    if (command == "sc.exe")
                    {
                        // Print full line (usually for SC, should be small)
                        outputBox.Invoke((MethodInvoker)(() => outputBox.AppendText(line + Environment.NewLine)));
                    }

                    outputBox.Invoke((MethodInvoker)(() => outputBox.AppendText(line + Environment.NewLine)));
                }

                // After executing function, then return to main stream
                process?.WaitForExit();
            }
            catch (Exception e)
            {
                Log("ERROR: " + e.Message);
            }
        }

        private void RunDelete(string command, string args)
        {
            try
            {
                ProcessStartInfo funcInfo = new ProcessStartInfo(command, args)
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Verb = "runas",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                // Start the program/args
                var process = Process.Start(funcInfo);

                // While there's still output, read line
                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();

                    outputBox.Invoke((MethodInvoker)(() => outputBox.AppendText(line + Environment.NewLine)));
                }

                // After executing function, then return to main stream
                process?.WaitForExit();
            }
            catch (Exception e)
            {
                Log("ERROR: " + e.Message);
            }
        }

        private bool DotNetInstalled(string version)
        {
            // Following the guide for https://chocolatey.org/install
            // Sets execution policy for the process, then downloads
            try
            {
                ProcessStartInfo funcInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--list-sdks",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process process = Process.Start(funcInfo);

                // Get list of SDKs
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Cut each line, see if it starts with the version requested, return true if so
                return output.Split('\n').Any(line => line.Trim().StartsWith(version));
            }
            catch
            {
                Log($".NET version {version} not found");
                return false;
            }
        }

        private bool IsInstalled(string command, string args)
        {
            try
            {
                ProcessStartInfo funcInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process process = Process.Start(funcInfo);

                // If output is an error, return false
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return (!string.IsNullOrWhiteSpace(output) && !string.IsNullOrEmpty(output));
            }
            catch
            {
                return false;
            }
        }

        private void ConfigureChrome()
        {
            // Start shell:startup for chrome shortcut
            Process.Start("explorer.exe", "shell:startup");

            // Start Chrome if it exists and open to skyview admin page
            string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            if (File.Exists(chromePath))
            {
                Process.Start(chromePath, "radianse.io");
            }
            else
            {
                Log($"Chrome not found at directory: {chromePath}.");
            }
        }

        private void DeleteUser(string userName)
        {
            try
            {
                // Check if user exists
                bool user = RunReturn("powershell.exe", $"Get-LocalUser -Name {userName}");

                if (user)
                {
                    string computerName = Environment.MachineName;
                    
                    // Delete user
                    RunDelete("powershell.exe", $"Remove-LocalUser -Name \"{userName}\"");
                    Log($"User '{userName}' deleted successfully.");
                }
                else
                {
                    Log($"User '{userName}' not found.");
                }
            }
            catch (Exception e)
            {
                Log($"Error deleting user: {e.Message}Likely user {userName} does not exist.");
            }
        }

        private void RenameUser(string oldName, string newName)
        {
            try
            {
                // Check if user exists
                bool user = RunReturn("powershell.exe", $"Get-LocalUser -Name {oldName}");

                if (user)
                {
                    // Rename user
                    RunTerminal("powershell.exe", $"Set-LocalUser -Name {oldName} -FullName {newName}");
                    Log($"User '{oldName}' renamed successfully to '{newName}'.");
                }
                else
                {
                    Log($"User '{oldName}' not found.");
                }
            }
            catch (Exception e)
            {
                Log($"Error renaming user: {e.Message}");
            }
        }

        private void RemoveUserPassword(string userName)
        {
            try
            {
                // Check if user exists
                DirectoryEntry localMachine = new DirectoryEntry("WinNT://" + Environment.MachineName);
                DirectoryEntry user = localMachine.Children.Find(userName);

                if (user != null)
                {
                    // Remove password; this was the only way I could find to do it without a password
                    user.Invoke("SetPassword", new object[] { "" });
                    user.CommitChanges();
                    Log($"Password for '{userName}' was removed successfully.");
                }
                else
                {
                    Log($"User '{userName}' not found.");
                }
            }
            catch (Exception e)
            {
                Log($"Error setting password: {e.Message}");
            }
        }

        private void Log(string msg)
        {
            outputBox.Invoke((MethodInvoker)(() =>
            {
                // Send text to the output box, scroll to bottom
                outputBox.AppendText(Environment.NewLine + msg + Environment.NewLine);
                outputBox.ScrollToCaret();
            }));
        }

        private bool RunReturn(string command, string args)
        {
            try
            {
                ProcessStartInfo funcInfo = new ProcessStartInfo(command, args)
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                // Start the program/args
                var process = Process.Start(funcInfo);

                // If the output is an error, return false
                string output = process.StandardOutput.ReadToEnd();
                return !string.IsNullOrWhiteSpace(output) && !string.IsNullOrWhiteSpace(output);
            }
            catch (Exception e)
            {
                Log($"Error removing services: {e.Message}");
                return false;
            }
        }

        private void RemoveAll(ProgressBar bar)
        {
            System.Diagnostics.Process.Start("control.exe", "appwiz.cpl");
            Log("MANUAL ACTION REQUIRED: Please remove local services.");

            setProgress(bar, 75);
        }

        private void InstallServices(string installFile)
        {
            

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string scriptDir = AppDomain.CurrentDomain.BaseDirectory;

            string desktopPath = Path.Combine(desktop, installFile);
            string migrationInstallPath = Path.Combine(scriptDir, installFile);

            ProcessStartInfo desktopDirectory = new ProcessStartInfo
            {
                FileName = "msiexec.exe",
                Arguments = $"/i \"{desktopPath}\" /quiet",
                UseShellExecute = true
            };

            ProcessStartInfo currDirectory = new ProcessStartInfo
            {
                FileName = "msiexec.exe",
                Arguments = $"/i \"{migrationInstallPath}\" /quiet",
                UseShellExecute = true
            };

            if (File.Exists(desktopPath))
            {
                Log($"Installing Skyview services found at: {desktopPath}");
                var process = Process.Start(desktopDirectory);
                process?.WaitForExit();
            }
            else if (File.Exists(migrationInstallPath))
            {
                Log($"Installing Skyview services found at: {migrationInstallPath}");
                var process = Process.Start(currDirectory);
                process?.WaitForExit();
            }
            else
            {
                Log($"No services found at: {desktopPath} || {migrationInstallPath}");
            }
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