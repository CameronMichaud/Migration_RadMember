using IWshRuntimeLibrary;
using Migration_RadAdmin.Output;
using System.Diagnostics;
using static System.Windows.Forms.DataFormats;

namespace Migration_RadAdmin.Processes
{
    internal class ProcessManager
    {
        public static void ConfigureChrome()
        {

            // Start shell:startup for chrome shortcut
            Process.Start("explorer.exe", "shell:startup");

            // Start Chrome if it exists and open to skyview admin page
            string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            if (System.IO.File.Exists(chromePath))
            {
                WshShell shell = new WshShell();

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string link = "https://radianse.io";
                string flags = "--start-fullscreen";

                string path = desktop + "Radianse" + ".lnk";

                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(link);
                
                shortcut.TargetPath = chromePath;
                shortcut.Arguments = $"--app{link} {flags}";
                shortcut.Description = "Open Radianse";
                shortcut.IconLocation = Path.Combine(Application.StartupPath, "rad_icon_green.ico");

                shortcut.Save();
            }
            else
            {
                OutputManager.Log($"Chrome not found at directory: {chromePath}.");
            }
        }

        public static void RunTerminal(string command, string args)
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

                   OutputManager.Log(line + Environment.NewLine);
                }

                // After executing function, then return to main stream
                process?.WaitForExit();
            }
            catch (Exception e)
            {
                OutputManager.Log("ERROR: " + e.Message);
            }
        }

        public static void RunDelete(string command, string args)
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

                    OutputManager.Log(line + Environment.NewLine);
                }

                // After executing function, then return to main stream
                process?.WaitForExit();
            }
            catch (Exception e)
            {
                OutputManager.Log("ERROR: " + e.Message);
            }
        }

        public static bool RunReturn(string command, string args)
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
                OutputManager.Log($"Error removing services: {e.Message}");
                return false;
            }
        }
        internal static int GetDriveSpace()
        {
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();

                foreach (DriveInfo drive in drives)
                {
                    if (drive.Name.Equals(@"C:\", StringComparison.OrdinalIgnoreCase))
                    {
                        int size = (int)(drive.TotalSize / (1024 * 1024 * 1024)); // Convert to GB
                        return size; // Return the size of the C: drive in GB
                    }
                }

                return 0; // If C: drive not found, return 0
            }
            catch (Exception e)
            {
                OutputManager.Log($"Error getting drive space: {e.Message}");
                return 0;
            }
        }

        internal static void DeleteServiceCentral()
        {
            try
            {
                if (Directory.Exists(@"C:\ProgramData\Radianse"))
                {
                    OutputManager.Log(@"Directory found: C:\ProgramData\Radianse");
                    OutputManager.Log(@"Deleting directory: C:\ProgramData\Radianse");
                    
                    // Get space before and after deletion, and delete the directory
                    int spaceBefore = GetDriveSpace();

                    Directory.Delete(@"C:\ProgramData\Radianse", true);

                    int spaceAfter = GetDriveSpace();

                    // If spaceBefore and spaceAfter are both valid, log the difference
                    if ((spaceBefore != 0) && (spaceAfter != 0))
                    {
                        OutputManager.Log($"Space freed: {spaceBefore - spaceAfter} GB.");
                    }
                    else
                    {
                        OutputManager.Log("Error calculating space before or after deletion.");
                    }
                }
                else
                {
                    OutputManager.Log(@"Directory not found: C:\ProgramData\Radianse");
                }
            }
            catch (Exception e)
            {
                OutputManager.Log($"Error deleting directory: {e.Message}");
                Process.Start("explorer.exe", @"C:\ProgramData\Radianse"); // Open the directory in explorer to delete manually
            }
        }
    }
}
