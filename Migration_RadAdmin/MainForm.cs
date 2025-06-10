using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Text.RegularExpressions;

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

                Process.Start(funcInfo);    // Start admin shell
                Environment.Exit(0);        // Exit old shell
            }

            // Buttons won't work without this, I'm unsure why
            startButton.Click += startButton_Click;
            stopButton.Click += stopButton_Click;
        }

        private async void startButton_Click(object sender, EventArgs e)
        {
            startButton.Enabled = false;    // Grey out start migration during execution

            await Task.Run(async () =>
            {
                await RunFunction("Installing .NET SDKs", dotnetProgress, () =>
                {
                    Log("Installing .NET 6 SDK via winget");
                    RunTerminal("powershell.exe", "winget install Microsoft.DotNet.SDK.6 --accept-package-agreements --accept-source-agreements -h");
                    setProgress(dotnetProgress, 50);
                    Log("Installing .NET 8 SDK via winget");
                    RunTerminal("powershell.exe", "winget install Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements -h");
                });

                await RunFunction("Installing Chrome", chromeProgress, () =>
                {
                    Log("Installing Chrome SDK via winget");
                    RunTerminal("powershell", "winget install Google.Chrome --accept-package-agreements --accept-source-agreements -h");
                });

                await RunFunction("Updating Users", userProgress, () =>
                {
                    string currentUser = Environment.UserName;
                    DeleteUser("Kiosk");
                    setProgress(userProgress, 50);
                    RenameUser(currentUser, "Radianse");
                    setProgress(userProgress, 75);
                    RemoveUserPassword("Radianse");
                });

                await RunFunction("Removing Local Services", cleanProgress, () =>
                {
                    RemoveAll(cleanProgress);
                    setProgress(cleanProgress, 50);
                    InstallServices("skyview-services-3.0.367.msi");
                });

                MessageBox.Show("Migration completed successfully.\nPlease log out and log back in.", "Migration Complete!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                setStatus("Migration Complete!");

                ConfigureChrome();  // Open radianse.io, run shell:startup

            });
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async Task RunFunction(string title, ProgressBar bar, Action action)
        {
            setStatus($"{title}...");
            Log($"==={title}===");
            setProgress(bar,  25);

            action();
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
                    else if (line.Contains("No available upgrade"))
                    {
                        // Up to date .NET, print "Already Installed"
                        outputBox.Invoke((MethodInvoker)(() => outputBox.AppendText("Already installed." + Environment.NewLine)));
                    }
                    else if (line.Contains("Successfully installed"))
                    {
                        // Don't use log to keep output under the SDK log
                        // Prints "Successfully Installed"
                        outputBox.Invoke((MethodInvoker)(() => outputBox.AppendText(line + Environment.NewLine)));
                    }
                }

                // After executing function, then return to main stream
                process?.WaitForExit();
            }
            catch (Exception e)
            {
                Log("ERROR: " + e.Message);
            }
        }

        private void ConfigureChrome()
        {
            // Start shell:startup for chrome shortcut
            // Process.Start("explorer.exe", "shell:startup");

            string userStartupPath = @"C:\Users\cam\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup";
            string allUsersStartupPath = @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Startup";
            try
            {
                if (Directory.Exists(userStartupPath))
                {
                    Process.Start("explorer.exe", $@"{userStartupPath}");
                }
                else
                {
                    Process.Start("explorer.exe", $@"{allUsersStartupPath}");
                }
            }
            catch (Exception e)
            {
                Log($"Error with opening startup folder: {e}");
            }


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
                bool user = RunReturn("powershell.exe", $"Get-LocalUser -Name {userName}");

                if (user)
                {
                    RunTerminal("powershell.exe", $"Remove-LocalUser -Name ${userName}");
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
                bool user = RunReturn("powershell.exe", $"Get-LocalUser -Name {oldName}");

                if (user)
                {
                    RunTerminal("powershell.exe", $"Rename-LocalUser -Name {oldName} -NewName {newName}");
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
            catch (Exception e)
            {
                Log($"Error setting password: {e.Message}");
            }
        }

        private void RemoveDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Log($"Removing directory: {path}");
                    Directory.Delete(path, true);
                }
                else
                {
                    Log($"Directory not found: {path}");
                }
            }
            catch (Exception e)
            {
                Log($"Error removing directory: {e.Message}");
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
            string[] wildcards = new[] { "radianse", "airpointe", "tanning", "massage", "kiosk" };
            string[] literals = new[] { "UpdateService", "ServiceManager" };

            RemovePrograms(wildcards, bar);
            RemoveServices(wildcards, literals, bar);

            Log("Removing Radianse & AirPointe program files directories:");
            RemoveDirectory("C:\\Program Files (x86)\\Radianse");
            RemoveDirectory("C:\\Program Files (x86)\\AirPointe");

            setProgress(bar,  75);
        }

        private void RemovePrograms(string[] keywords, ProgressBar bar)
        {
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

                    Log($"Uninstalling: {subKey.GetValue("DisplayName")?.ToString()}");
                    RunTerminal("cmd.exe", $"/c \"{uninstallString}\"");
                }

                setProgress(bar, 50);
            }
        }

        private void RemoveServices(string[] keywords, string[] literals, ProgressBar bar)
        {
            try
            { 
                List<string> validServices = new List<string>();

                foreach(string service in keywords)
                {
                    ProcessStartInfo funcInfo = new ProcessStartInfo()
                    {
                        FileName = "powershell.exe",
                        Arguments = "Get-Service | Where-Object { $_.Name -like " + $"'*{service}*'" + "}" + " | Select-Object -ExpandProperty Name",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };

                    // Start the program/args
                    var process = Process.Start(funcInfo);

                    // Get whole output, if text trim and save
                    string output = process.StandardOutput.ReadToEnd();

                    // If there's a name, save it
                    if (!string.IsNullOrEmpty(output) && !string.IsNullOrWhiteSpace(output)) 
                    { 
                        Log($"Valid service found: {output}"); 
                        validServices.Add(output.Trim()); 
                    }
                }
                // Process all literal services
                foreach (string service in literals)
                {
                    if (RunReturn("powershell.exe", "Get-Service | Where-Object { $_.Name -eq " + $"{service}" + "}" + " | Select-Object -ExpandProperty Name"))
                    {
                        Log($"Valid service found: {service}");
                        validServices.Add(service);
                    }
                    
                }
                // Stop/Delete all services
                foreach (string service in validServices)
                {
                    RunTerminal("sc.exe", $"stop \"{service}\""); 
                    RunTerminal("sc.exe", $"delete \"{service}\"");
                    Log($"Removed: {service}");
                }
            }
            catch(Exception e)
            {
                Log($"Error removing services: {e.Message}");
            }

        }

        private void InstallServices(string installFile)
        {
            Log($"===Installing Skyview Services===");
            setStatus("Installing Skyview Services...");
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
            else
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