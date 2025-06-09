using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Migration_RadAdmin
{
    public partial class MainForm : Form
    {
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

                Process.Start(funcInfo);
                Environment.Exit(0);
            }

            startButton.Click += startButton_Click;
            stopButton.Click += stopButton_Click;
        }

        private async void startButton_Click(object sender, EventArgs e)
        {
            startButton.Enabled = false;

            await Task.Run(async () =>
            {
                await RunFunction("Installing .NET SDKs", dotnetProgress, () =>
                {
                    Log("Installing .NET 6 SDK via winget");
                    RunTerminal("powershell.exe", "winget install Microsoft.DotNet.SDK.6 --accept-package-agreements --accept-source-agreements -h");
                    dotnetProgress.Invoke((MethodInvoker)(() => dotnetProgress.Value = 50));
                    Log("\nInstalling .NET 8 SDK via winget");
                    RunTerminal("powershell.exe", "winget install Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements -h");
                });

                await RunFunction("Installing Chrome", chromeProgress, () =>
                {
                    Log("Installing Chrome SDK via winget");
                    RunTerminal("powershell", "winget install Google.Chrome --accept-source-agreements -h");
                });

                await RunFunction("Updating Users", userProgress, () =>
                {
                    string currentUser = Environment.UserName;
                    DeleteUser("Kiosk");
                    userProgress.Invoke((MethodInvoker)(() => userProgress.Value = 50));
                    RenameUser(currentUser, "Radianse");
                    userProgress.Invoke((MethodInvoker)(() => userProgress.Value = 75));
                    SetUserPassword("Radianse");
                });

                await RunFunction("Removing Local Services", cleanProgress, () =>
                {
                    RemoveServices(cleanProgress);
                    cleanProgress.Invoke((MethodInvoker)(() => cleanProgress.Value = 50));
                    InstallServices("skyview-services-3.0.367.msi");
                });

                MessageBox.Show("Migration completed successfully.", "Migration Complete!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                statusText.Invoke((MethodInvoker)(() => statusText.Text = "Migration Complete!"));

                ConfigureChrome();

            });
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async Task RunFunction(string title, ProgressBar bar, Action action)
        {
            statusText.Invoke((MethodInvoker)(() => statusText.Text = $"{title}..."));
            Log($"==={title}===");
            bar.Invoke((MethodInvoker)(() => bar.Value = 25));

            action();
            bar.Invoke((MethodInvoker)(() => bar.Value = 100));
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

                var process = Process.Start(funcInfo);

                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();
                    outputBox.Invoke((MethodInvoker)(() => outputBox.AppendText(line + "\n")));
                }

                process?.WaitForExit();
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
            }
        }

        private void ConfigureChrome()
        {
            // Start shell:startup for chrome shortcut
            Process.Start("explorer.exe", "shell:startup");

            // Start Chrome if it exists and move to admin page
            string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            if (File.Exists(chromePath))
            {
                Process.Start(chromePath, "https://www.radianse.io");
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
                DirectoryEntry localMachine = new DirectoryEntry("WinNT://" + Environment.MachineName);
                DirectoryEntries users = localMachine.Children;
                DirectoryEntry user = users.Find(userName);

                if (user != null)
                {
                    users.Remove(user);
                    Log($"User '{userName}' deleted successfully.");
                }
                else
                {
                    Log($"User '{userName}' not found.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}.");
            }
        }

        private void RenameUser(string oldName, string newName)
        {
            try
            {
                DirectoryEntry localMachine = new DirectoryEntry("WinNT://" + Environment.MachineName);
                DirectoryEntry user = localMachine.Children.Find(oldName);

                if (user != null)
                {
                    user.Rename(newName);
                    user.CommitChanges();
                    Log($"User '{oldName}' renamed successfully to '{newName}'.");
                }
                else
                {
                    Log($"User '{oldName}' not found.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }

        private void SetUserPassword(string userName)
        {
            try
            {
                DirectoryEntry localMachine = new DirectoryEntry("WinNT://" + Environment.MachineName);
                DirectoryEntry user = localMachine.Children.Find(userName);

                if (user != null)
                {
                    user.Invoke("SetPassword", new object[] { "" });
                    user.CommitChanges();
                    Log($"Password for '{userName}' was removed successfully.");
                }
                else
                {
                    Log($"User '{userName}' not found.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }

        private void RemoveDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Log($"Removing directory: {path}");
                Directory.Delete(path, true);
            }
        }

        private void Log(string msg)
        {
            outputBox.Invoke((MethodInvoker)(() =>
            {
                outputBox.AppendText(Environment.NewLine + msg + Environment.NewLine);
                outputBox.ScrollToCaret();
            }));
        }

        private void RemoveServices(ProgressBar bar)
        {
            string[] keywords = new[] { "radianse", "airpointe" };
            string[] registries = new[]
            {
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
                "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
            };

            foreach (string path in registries)
            {
                var rootKey = Registry.LocalMachine.OpenSubKey(path) ?? Registry.CurrentUser.OpenSubKey(path);
                if (rootKey == null) continue;

                foreach (var subKeyName in rootKey.GetSubKeyNames())
                {
                    if (!keywords.Any(keyword => subKeyName.Contains(keyword, StringComparison.OrdinalIgnoreCase))) continue;

                    var subKey = rootKey.OpenSubKey(subKeyName);
                    if (subKey == null) continue;

                    string uninstallString = subKey.GetValue("QuietUninstallString")?.ToString() ?? subKey.GetValue("UninstallString")?.ToString();
                    if (string.IsNullOrEmpty(uninstallString)) { Log($"No uninstall string found for {subKeyName}"); continue; }

                    Log($"Uninstalling: {subKeyName}");
                    RunTerminal("cmd.exe", $"/c \"{uninstallString}\"");
                }

                bar.Invoke((MethodInvoker)(() => bar.Value = 50));
            }

            Log("Removing Radianse & AirPointe program files directories:");
            RemoveDirectory("C:\\Program Files (x86)\\Radianse");
            RemoveDirectory("C:\\Program Files (x86)\\AirPointe");

            bar.Invoke((MethodInvoker)(() => bar.Value = 75));
        }

        private void InstallServices(string installFile)
        {
            statusText.Invoke((MethodInvoker)(() => statusText.Text = "Installing Skyview Services..."));
            statusText.Invoke((MethodInvoker)(() => servicesLabel.Text = "Installing Skyview Services"));

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string installPath = Path.Combine(desktopPath, installFile);
            string migrationInstallPath = Path.Combine(desktopPath, "migration", installFile);

            ProcessStartInfo funcInfo = new ProcessStartInfo
            {
                FileName = "msiexec.exe",
                Arguments = $"/i \"{installPath}\" /qn",
                UseShellExecute = true
            };

            if (File.Exists(installPath))
            {
                Log($"Installing Skyview services found at: {installPath}");
                var process = Process.Start(funcInfo);
                process?.WaitForExit();
            }
            else if (File.Exists(migrationInstallPath))
            {
                Log($"Installing Skyview services found at: {migrationInstallPath}");
            }
            {
                Log($"No services found at: {installPath} || {migrationInstallPath}");
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