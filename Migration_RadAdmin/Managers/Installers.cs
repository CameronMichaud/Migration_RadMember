using System.Diagnostics;

using Migration_RadAdmin.Output;

namespace Migration_RadAdmin.Installers;

internal class InstallManager
{
    public static async Task GetInstaller(string url, string filePath)
    {
        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(url);

        response.EnsureSuccessStatusCode();

        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fs);

        OutputManager.Log($"Download Complete: {filePath}");
    }

    public static bool DotNetInstalled(string version)
    {
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
            OutputManager.Log($".NET version {version} not found");
            return false;
        }
    }

    public static bool IsInstalled(string command, string args)
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

    public static bool InstallServices(string installFile)
    {
        // Paths the installer could be, either on the desktop, or the program directory
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string scriptDir = AppDomain.CurrentDomain.BaseDirectory;

        // Get the full paths as strings
        string desktopPath = Path.Combine(desktop, installFile);
        string migrationInstallPath = Path.Combine(scriptDir, installFile);
        
        // Create two process infos, one for desktop and the other for current directory
        ProcessStartInfo desktopDirectory = new ProcessStartInfo
        {
            FileName = "msiexec",
            Arguments = $"/i \"{desktopPath}\" /passive",
            UseShellExecute = true
        };
        ProcessStartInfo currDirectory = new ProcessStartInfo
        {
            FileName = "msiexec",
            Arguments = $"/i \"{migrationInstallPath}\" /passive",
            UseShellExecute = true
        };

        // Run the process for which case is true for the MSI, on desktop, or in current directory
        if (File.Exists(desktopPath))
        {
            OutputManager.Log($"Installing Skyview services found at: {desktopPath}");
            var process = Process.Start(desktopDirectory);
            process?.WaitForExit();

            return true;
        }
        else if (File.Exists(migrationInstallPath))
        {
            OutputManager.Log($"Installing Skyview services found at: {migrationInstallPath}");
            var process = Process.Start(currDirectory);
            process?.WaitForExit();

            return true;
        }
        else
        {
            OutputManager.Log($"No services found at: {desktopPath} || {migrationInstallPath}");

            return false;
        }
    }
}

