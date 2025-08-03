using Migration_RadAdmin.Installers;
using Migration_RadAdmin.Output;
using Migration_RadAdmin.Processes;
using Migration_RadAdmin.Users;
using System.Diagnostics;

namespace Migration_RadAdmin.Migration
{
    internal class MigrationManager
    {
        private static MainForm form;
        public static void Initialize(MainForm mainForm)
        {
            form = mainForm;
        }

        internal static async Task UpdateUsers()
        {
            string currentUser = Environment.UserName;

            await form.RunTask("Updating Users", form.userProgress, async () =>
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
                    OutputManager.setProgress(form.userProgress, 50);
                    UserManager.RemoveUserPassword(currentUser);
                    OutputManager.setProgress(form.userProgress, 75);
                    UserManager.RenameUser(currentUser, "Radianse");
                }
            });
        }

        internal static async Task InstallChrome()
        {
            string currentUser = Environment.UserName;

            await form.RunTask("Installing Chrome", form.chromeProgress, async () =>
            {
                // Check if chrome exists or if there's a package manager, if so use them
                bool chrome = File.Exists(@"C:\Program Files\Google\Chrome\Application\chrome.exe");

                // If chrome does not exist, grab directly form URL
                if (!chrome)
                {
                    // If there's no package manager, download the file
                    string chromeInstaller = @"https://dl.google.com/chrome/install/latest/chrome_installer.exe";
                    string filePath = $@"C:\users\{currentUser}\downloads\chrome_installer.exe";
                    await InstallManager.GetInstaller(chromeInstaller, filePath);

                    OutputManager.setProgress(form.chromeProgress, 75);

                    // Install Chrome
                    OutputManager.Log("Installing Chrome (this may take a few minutes)");
                    ProcessManager.RunTerminal("cmd.exe", $"/c \"{filePath} /silent /install\"");
                    File.Delete(filePath); // Delete the installer after running it
                }
                else
                {
                    OutputManager.Log("Chrome already installed");
                }
            });
        }

        internal static void CompleteMigration()
        {
            OutputManager.setStatus("Migration Complete!");
            form.startButton.Text = "Complete";

            MessageBox.Show("Migration completed successfully.", "Migration Complete!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        internal static void SetStartup()
        {
            // INSTALL RADIANSE.IO AS APPLICATION:
            OutputManager.setStatus("Creating Radianse Shortcuts...");

            // Open radianse.io in Chrome, run shell:startup
            ProcessManager.ConfigureChrome();
        }
    }
}
