namespace Migration_RadAdmin.Output
{
    internal class OutputManager
    {
        private static MainForm form;

        public static void Initialize(MainForm mainForm)
        {
            form = mainForm;
        }
        internal static void Log(string msg)
        {
            form.Invoke((MethodInvoker)(() =>
            {
                // Send text to the output box, scroll to bottom
                form.outputBox.AppendText(Environment.NewLine + msg + Environment.NewLine);
                form.outputBox.ScrollToCaret();
            }));
        }

        internal static void setProgress(ProgressBar bar, int percentage)
        {
            // Update given progress bar
            bar.Invoke((MethodInvoker)(() => bar.Value = percentage));
        }

        internal static void setStatus(string status)
        {
            // Update status text
            form.statusText.Invoke((MethodInvoker)(() => form.statusText.Text = status));
        }
    }
}
