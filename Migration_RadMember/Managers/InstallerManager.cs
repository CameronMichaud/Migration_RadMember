using System.Diagnostics;

using Migration_RadMember.Output;

namespace Migration_RadMember.Installers;

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
}

