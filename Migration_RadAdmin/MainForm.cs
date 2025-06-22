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
            string currentUser = Environment.UserName;

            await Task.Run(async () =>
            {
                if (MigrationState == 0)
                {
                    await RunFunction("Installing .NET SDKs", dotnetProgress, () =>
                    {
                        bool dotnet6 = DotNetInstalled("6");
                        if (!dotnet6)
                        {
                            Log("Dowloading via curl .NET 6 (this may take a few minutes)");
                            RunTerminal("cmd.exe", $@"/c curl -o C:\\users\{currentUser}\desktop\dotnet6.exe https://builds.dotnet.microsoft.com/dotnet/Sdk/6.0.428/dotnet-sdk-6.0.428-win-x64.exe");
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
                            Log("Dowloading via curl .NET 8 (this may take a few minutes)");
                            RunTerminal("cmd.exe", $@"/c curl -o C:\\users\{currentUser}\desktop\dotnet8.exe https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.411/dotnet-sdk-8.0.411-win-x64.exe");
                            Log("Installing .NET 8 (this may take a few minutes)");
                            RunTerminal("cmd.exe", $@"/c C:\\users\{currentUser}\desktop\dotnet8.exe /quiet");
                            Log(".NET 8 install finished.\n");
                        }
                        else
                        {
                            Log(".NET 8 already installed");
                        }
                    });

                    await RunFunction("Installing Chrome", chromeProgress, () =>
                    {
                        bool chrome = IsInstalled("chrome.exe", "--version");
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
                            // If there's no package manager installed, try to donwload chocolatey
                            choco = InstallChocolatey();

                            // Restart PowerShell to apply changes
                            RestartPowershell();

                            if (choco)
                            {
                                Log("Installing Chrome via chocolatey (this may take a few minutes)");
                                RunTerminal("powershell.exe", "choco install googlechrome -y");
                            }
                            else
                            {
                                Log("No package manager installed; Chrome must be downloaded manually.");
                                Log("===MANUAL ACTION REQURED===");
                                Log("If you have Firefox installed, it will open the download page for Chrome.");
                                RunTerminal("cmd.exe", $@"/c start firefox https://www.google.com/chrome/");
                            }
                        }
                        else
                        {
                            Log("Chrome already installed");
                        }
                    });

                    await RunFunction("Removing Local Services", cleanProgress, () =>
                    {
                        RemoveAll(cleanProgress);
                        setProgress(cleanProgress, 50);
                        InstallServices("skyview-services-3.0.367.msi");
                    });


                    setStatus("Manual Action Required. (Install Radianse.io as app)");

                    ConfigureChrome();  // Open radianse.io, run shell:startup

                    startButton.Text = "Continue Migration";

                    startButton.Enabled = true;

                    MigrationState = 1; // Set migration state to 1 (last step is user management)

                    MessageBox.Show("Please install Radainse as an app.", "Manual action required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    await RunFunction("Updating Users", userProgress, () =>
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

        private void stopButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async Task RunFunction(string title, ProgressBar bar, Action action)
        {
            setStatus($"{title}...");
            Log($"==={title}===");
            setProgress(bar, 25);

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

        private bool InstallChocolatey()
        {
            try
            {
                // Check if Chocolatey is already installed
                if (IsInstalled("choco.exe", "-?"))
                {
                    Log("Chocolatey is already installed.");
                    return true;
                }
                // Download and install Chocolatey
                // Following the guide for https://chocolatey.org/install
                Log("Installing Chocolatey...");
                RunTerminal("powershell.exe", "Set-ExecutionPolicy Bypass -Scope Process; Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))");
                // Verify installation
                if (IsInstalled("choco.exe", "-?"))
                {
                    Log("Chocolatey installed successfully.");
                    return true;
                }
                else
                {
                    Log("Chocolatey installation failed.");
                    return false;
                }
            }
            catch (Exception e)
            {
                Log($"Error installing Chocolatey: {e.Message}");
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
            Process.Start("explorer.exe", "shell:startup").WaitForExit();

            bool chrome = IsInstalled("chrome.exe", "--version");

            // Start Chrome if it exists and open to skyview admin page
            string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            if (File.Exists(chromePath))
            {
                Process.Start(chromePath, "radianse.io");
            }
            else if (chrome)
            {
                RunTerminal("chrome.exe", "radianse.io");
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
            string[] wildcards = new[] { "radianse", "airpointe", "tanning", "massage", "kiosk", "local services" };
            string[] literals = new[] { "UpdateService", "ServiceManager" };

            RemovePrograms(wildcards, bar);
            RemoveServices(wildcards, literals, bar);

            Log("Removing Radianse & AirPointe program files directories:");
            RemoveDirectory("C:\\Program Files (x86)\\Radianse");
            RemoveDirectory("C:\\Program Files (x86)\\AirPointe");

            setProgress(bar, 75);
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
                // Loop the LM and CU registries
                var rootKey = Registry.LocalMachine.OpenSubKey(path) ?? Registry.CurrentUser.OpenSubKey(path);
                if (rootKey == null) continue;

                foreach (var subKeyName in rootKey.GetSubKeyNames())
                {
                    // If there's a subkey with a keyword, grab it, assign to variable
                    if (!keywords.Any(keyword => subKeyName.Contains(keyword, StringComparison.OrdinalIgnoreCase))) continue;
                    var subKey = rootKey.OpenSubKey(subKeyName);
                    if (subKey == null) continue;

                    // Try to pull the quiet uninstall, if not possible, pull the normal uninstall string
                    string uninstallString = subKey.GetValue("QuietUninstallString")?.ToString() ?? subKey.GetValue("UninstallString")?.ToString();
                    
                    // Visual Studio was angry
                    if (string.IsNullOrEmpty(uninstallString))
                    {
                        Log($"No uninstall string found for {subKeyName}");
                        continue; 
                    }
                    
                    // Stop programs before deleting
                    StopPrograms(keywords);

                    // Uninstall hits
                    Log($"Uninstalling: {subKey.GetValue("DisplayName")?.ToString()}");
                    RunTerminal("cmd.exe", $"/c \"{uninstallString}\"");
                }

                setProgress(bar, 50);
            }
        }

        private void StopPrograms(string[] keywords)
        {
            try
            {
                // Stores proceses
                List<string> validProcesses = new List<string>();

                foreach (string processName in keywords)
                {
                    ProcessStartInfo funcInfo = new ProcessStartInfo()
                    {
                        FileName = "powershell.exe",
                        Arguments = "Get-Process | Where-Object { $_.ProcessName -match " + $"'{processName}'" + "}" + " | Select-Object -ExpandProperty ProcessName",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };

                    var process = Process.Start(funcInfo);
                    string output = process.StandardOutput.ReadToEnd();

                    // If no error, pull the match and add to validProcess to be deleted later
                    if (!string.IsNullOrEmpty(output) && !string.IsNullOrWhiteSpace(output))
                    {
                        Log($"Valid process found: {output}");
                        validProcesses.Add(output.Trim());
                    }
                }

                // Stop programs before deletion to ensure they're deleted properly
                foreach (string processName in validProcesses)
                {
                    RunTerminal("taskkill.exe", $"/FI 'USERNAME eq Kiosk' /IM {processName} /F");
                    Log($"Stopped process: {processName}");
                }
            }
            catch (Exception e)
            {
                Log($"Error stopping process: {e.Message}");
            }
        }


        private void RemoveServices(string[] keywords, string[] literals, ProgressBar bar)
        {
            try
            {
                List<string> validServices = new List<string>();

                foreach (string service in keywords)
                {
                    ProcessStartInfo funcInfo = new ProcessStartInfo()
                    {
                        FileName = "powershell.exe",
                        Arguments = "Get-Service | Where-Object { $_.Name -match " + $"'{service}'" + "}" + " | Select-Object -ExpandProperty Name",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };

                    // Start the program/args
                    var process = Process.Start(funcInfo);

                    // Get whole output, if text trim and save
                    string output = process.StandardOutput.ReadToEnd();

                    // If there's a keyword found (names will output), save it
                    if (!string.IsNullOrEmpty(output) && !string.IsNullOrWhiteSpace(output))
                    {
                        Log($"Valid service found: {output}");
                        validServices.Add(output.Trim());
                    }
                }
                // Process all literal services; makes sure unrelated services aren't deleted
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
            catch (Exception e)
            {
                Log($"Error removing services: {e.Message}");
            }

        }

        private void InstallServices(string installFile)
        {
            Log($"===Installing Skyview Services===");
            setStatus("Installing Skyview Services...");
            statusText.Invoke((MethodInvoker)(() => servicesLabel.Text = "Installing Skyview Services"));

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
        private void RestartPowershell()
        {
            try
            {
                // Fixes chocolatey
                RunTerminal("powershell.exe", "-Command \"Set-ExecutionPolicy Bypass -Scope Process -Force; exit\"");
                Log("PowerShell context refreshed.");
            }
            catch (Exception e)
            {
                Log($"Error refreshing PowerShell context: {e.Message}");
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