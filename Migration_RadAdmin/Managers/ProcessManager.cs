﻿using IWshRuntimeLibrary;
using Migration_RadAdmin.Output;
using System.Diagnostics;
using static System.Windows.Forms.DataFormats;

namespace Migration_RadAdmin.Processes
{
    internal class ProcessManager
    {
        public static void ConfigureChrome()
        {
            OutputManager.Log("===Configuring Chrome===");

            // Create shortcut to Radinse on desktop and startup folder
            string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            if (System.IO.File.Exists(chromePath))
            {
                try
                {
                    // Create a shortcut to Radianse on the desktop
                    WshShell shell = new WshShell();

                    // Desktop folder path
                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    
                    // Chrome args
                    string link = "https://schedulekiosk.radianse.io";
                    string flags = "--start-fullscreen";

                    // Shortcut path
                    string path = Path.Combine(desktop + @"\Radianse.lnk");

                    // Create the shortcut
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(path);

                    shortcut.TargetPath = chromePath;
                    shortcut.Arguments = $"--app={link} {flags}";
                    shortcut.Description = "Open Radianse";

                    shortcut.Save();
                    OutputManager.Log($"Chrome shortcut created on desktop: {path}");

                    // Get the Startup folder path, then copy the shortcut there as well
                    string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                    string finalPath = Path.Combine(startupPath, "Radianse.lnk");

                    String[] files = Directory.GetFiles(startupPath);

                    foreach (string file in files)
                    {
                        System.IO.File.Delete(file); // Delete all files in the startup folder
                        OutputManager.Log($"Deleted file in startup folder: {file}");
                    }

                    System.IO.File.Copy(path, finalPath, true);
                    if (System.IO.File.Exists(finalPath))
                    {
                        OutputManager.Log($"Chrome shortcut created in startup folder: {finalPath}");
                    }
                    else
                    {
                        OutputManager.Log($"Failed to create Chrome shortcut in startup folder: {finalPath}");
                    }
                }
                catch (Exception e)
                {
                    OutputManager.Log($"Error creating Chrome shortcut: {e.Message}");
                }
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

        public static void RunLogoff(string username)
        {
            try
            {
                ProcessStartInfo funcInfo = new ProcessStartInfo()
                {
                    FileName = "powershell.exe",
                    Arguments = "quser",
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
                    string OutputLine = process.StandardOutput.ReadLine();

                    var lines = OutputLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    // If header line, skip, then find kiosk user session
                    if (lines[0].Equals("USERNAME", StringComparison.OrdinalIgnoreCase))
                    {
                        OutputManager.Log($"Skipping user: {lines[0]}");
                    }
                    else if (lines[0].Equals(username, StringComparison.OrdinalIgnoreCase) || lines[0].Equals($">{username}", StringComparison.OrdinalIgnoreCase))
                    {
                        OutputManager.Log($"User found: {username}");
                        OutputManager.Log($"Logging out user: {lines[0]}");

                        // If session name is null, use the second index, should be the session ID
                        if (lines[2].Equals("Disc", StringComparison.OrdinalIgnoreCase))
                        {
                            OutputManager.Log($"Session ID: {lines[1]}");
                            RunTerminal("powershell.exe", $"logoff {lines[1]}");
                        }
                        else
                        {
                            OutputManager.Log($"Session ID: {lines[2]}");
                            RunTerminal("powershell.exe", $"logoff {lines[2]}");
                        }
                    }
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
    }
}
