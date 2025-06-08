// MainWindow.xaml.cs
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;

namespace RadianseMigrationApp
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
        }

        private async void startButton_Click(object sender, RoutedEventArgs e)
        {
            startButton.Enabled = false;

            await Task.Run(async () =>
            {
                await RunFunction("Installing .NET SDKs", dotnetProgress, () =>
                {
                    Log("Installing .NET 6 SDK via winget");
                    RunTerminal("powershell.exe", "winget install Microsoft.DotNet.SDK.6 --accept-source-agreements -h");
                    Log("\nInstalling .NET 8 SDK via winget");
                    RunTerminal("powershell.exe", "winget install Microsoft.DotNet.SDK.8 --accept-source-agreements -h");
                });

                await RunFunction("Installing Chrome", chromeProgress, () =>
                { 
                    Log("Installing Chrome SDK via winget");
                    RunTerminal("powershell", "winget install Google.Chrome --accept-source-agreements -h");
                });

                await RunFunction("Updating Users", userProgress, () =>
                {
                    // Remove Kiosk if present
                    //RunTerminal("powershell", "Remove-LocalUser -Name 'Kiosk' -ErrorAction SilentlyContinue");

                    string currentUser = Environment.UserName;
                    DeleteUser("Kiosk");
                    RenameUser(currentUser, "Radianse");
                    SetUserPassword("Radianse");

                   
                    // Set main user as Radianse
                    // Log($"Current User: {currentUser}");
                    // if (currentUser != "Radianse")
                    // {
                    //     Log($"Renaming {currentUser} to 'Radianse'");
                    //     RunTerminal("powershell", $"Rename-LocalUser -Name '{currentUser}' -NewName 'Radianse'");
                    // }
                });

                await RunFunction("Removing Local Services", cleanProgress, () =>
                    { RemoveServices(cleanProgress); InstallServices("skyview-services-3.0.367.msi"); });

                MessageBox.Show("Migration completed successfully.", "Migration Complete!", MessageBoxButton.OK, MessageBoxImage.Information);

                Dispatcher.Invoke(() => StatusText.Content = "Migration Complete!");
                Process.Start("explorer.exe", @$"C:\Users\Radianse\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup");
                Process.Start("chrome.exe", "www.radianse.io");
            });
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
                else if (user == null)
                {
                    Log($"User '{userName}' not found.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
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

        private void stopButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async Task RunFunction(string title, ProgressBar bar, Action action)
        {
            // Print name of step, start progress
            Dispatcher.Invoke(() => StatusText.Content = $"{title}...");
            Dispatcher.Invoke(() => Log($"==={title}==="));
            Dispatcher.Invoke(() => bar.Value = 25);
            //await Task.Delay(400);

            // Run function, then fill progress bar
            action();
            Dispatcher.Invoke(() => bar.Value = 100);
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
                    UseShellExecute = false // Stop UAC prompts if already admin
                };

                var process = Process.Start(funcInfo);

                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();
                    Dispatcher.Invoke(() => LogBox.AppendText(line + "\n"));
                }

                process?.WaitForExit();
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
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
            Dispatcher.Invoke(() =>
            {
                LogBox.AppendText(msg + "\n\n");
                LogBox.ScrollToEnd();
            });
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
                // Get rootkey of HKLM or HKCU
                var rootKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path) ?? Registry.CurrentUser.OpenSubKey(path);
                if (rootKey == null) continue; // No access to registry

                foreach (var subKeyName in rootKey.GetSubKeyNames())
                {
                    // If no keyword found in name, skip to next key
                    if (!keywords.Any(keyword => subKeyName.Contains(keyword, StringComparison.OrdinalIgnoreCase))) continue;

                    var subKey = rootKey.OpenSubKey(subKeyName);
                    if (subKey == null) continue; // Skip subKey if null

                    // Get the quiet uninstall string, otherwise get the normal uninstall string
                    string uninstallString = subKey.GetValue("QuietUninstallString")?.ToString() ?? subKey.GetValue("UninstallString")?.ToString();
                    if (string.IsNullOrEmpty(uninstallString)) { Log($"No uninstall string found for {subKey}"); continue; }
                    ; // Skip if there's no uninstall string

                    Log($"Uninstalling: {subKeyName}");
                    RunTerminal("cmd.exe", $"/c \"{uninstallString}\""); //
                }

                Dispatcher.Invoke(() => bar.Value = 50);
            }

            // Remove old services directories
            Log($"Removing Radianse & AirPointe program files directories:");
            RemoveDirectory("C:\\Program Files (x86)\\Radianse");
            RemoveDirectory("C:\\Program Files (x86)\\AirPointe");

            Dispatcher.Invoke(() => bar.Value = 75);
        }

        private void InstallServices(string installFile)
        {
            // Update UI
            Dispatcher.Invoke(() => ServicesButton.Text = "Install Skyview Services");
            Dispatcher.Invoke(() => StatusText.Content = "Installing Skyview Services...");

            // Get path for services install
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string installPath = Path.Combine(desktopPath, installFile);

            // Run .msi installer
            ProcessStartInfo funcInfo = new ProcessStartInfo
            {
                FileName = "msiexec.exe",
                Arguments = $"/i \"{installPath}\" /qn",
                UseShellExecute = true // Stop UAC prompts if already admin
            };
            if (Directory.Exists(installPath))
            {
                Log($"Installing Skyview services found at directory: {installPath}");
                var process = Process.Start(funcInfo);
                process?.WaitForExit();
            }
            else
            {
                Log($"No services found at directory: {installPath}");
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
