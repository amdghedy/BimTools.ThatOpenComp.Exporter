using System.Diagnostics;
using System.Drawing;

namespace RevitVersionSelector;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private readonly ComboBox version = new();
    private readonly TextBox repoPath = new();
    private readonly TextBox log = new();
    private readonly Button browse = new();
    private readonly Button build = new();
    private readonly Label status = new();

    public MainForm()
    {
        Text = "Revit Version Build Selector";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 560);
        Size = new Size(900, 680);
        Font = new Font("Segoe UI", 9F);
        BackColor = Color.FromArgb(245, 247, 251);

        var heading = new Label { Text = "Revit Add-in Builder", Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true, Location = new Point(24, 18) };
        var subtitle = new Label { Text = "Select the Revit version and build against its installed API.", AutoSize = true, ForeColor = Color.FromArgb(80, 90, 105), Location = new Point(26, 55) };
        var repoLabel = new Label { Text = "Repository folder", AutoSize = true, Location = new Point(26, 98) };
        repoPath.SetBounds(26, 121, 700, 30);
        repoPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        repoPath.PlaceholderText = "Select the cloned BimTools.ThatOpenComp.Exporter folder";
        browse.Text = "Browse...";
        browse.SetBounds(738, 120, 112, 32);
        browse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        browse.Click += (_, _) => BrowseRepository();

        var versionLabel = new Label { Text = "Revit version", AutoSize = true, Location = new Point(26, 169) };
        version.DropDownStyle = ComboBoxStyle.DropDownList;
        version.Items.AddRange(new object[] { "2020", "2021", "2022", "2023", "2024", "2025", "2026", "2027" });
        version.SelectedItem = "2027";
        version.SetBounds(26, 192, 180, 32);

        build.Text = "Build selected version";
        build.SetBounds(226, 191, 220, 34);
        build.BackColor = Color.FromArgb(35, 95, 170);
        build.ForeColor = Color.White;
        build.FlatStyle = FlatStyle.Flat;
        build.Click += async (_, _) => await StartBuildAsync();

        status.Text = "Ready. Requires Windows, PowerShell, .NET SDK, and the selected Revit API assemblies.";
        status.AutoEllipsis = true;
        status.SetBounds(26, 239, 824, 28);
        status.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var logLabel = new Label { Text = "Build output", AutoSize = true, Location = new Point(26, 275) };
        log.Multiline = true;
        log.ReadOnly = true;
        log.ScrollBars = ScrollBars.Both;
        log.WordWrap = false;
        log.Font = new Font("Consolas", 9F);
        log.BackColor = Color.White;
        log.SetBounds(26, 300, 824, 310);
        log.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        Controls.AddRange(new Control[] { heading, subtitle, repoLabel, repoPath, browse, versionLabel, version, build, status, log });
        AcceptButton = build;
    }

    private void BrowseRepository()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose the cloned repository root",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            repoPath.Text = dialog.SelectedPath;
            var script = Path.Combine(dialog.SelectedPath, "scripts", "Build-All-Revit-Versions.ps1");
            status.Text = File.Exists(script) ? "Build script found." : "Could not find scripts\\Build-All-Revit-Versions.ps1 in this folder.";
        }
    }

    private async Task StartBuildAsync()
    {
        if (version.SelectedItem is not string selectedVersion)
        {
            MessageBox.Show(this, "Select a Revit version first.", "Missing version", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var root = repoPath.Text.Trim().Trim('"');
        var script = Path.Combine(root, "scripts", "Build-All-Revit-Versions.ps1");
        if (!Directory.Exists(root) || !File.Exists(script))
        {
            MessageBox.Show(this, "Browse to the repository root containing scripts\\Build-All-Revit-Versions.ps1.", "Repository not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
        if (!File.Exists(powershell)) powershell = "powershell.exe";

        build.Enabled = false;
        browse.Enabled = false;
        version.Enabled = false;
        log.Clear();
        status.Text = "Building Revit " + selectedVersion + "...";
        AppendLog("Repository: " + root + Environment.NewLine + "Target: Revit " + selectedVersion + Environment.NewLine + Environment.NewLine);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = powershell,
                WorkingDirectory = root,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-ExecutionPolicy");
            psi.ArgumentList.Add("Bypass");
            psi.ArgumentList.Add("-File");
            psi.ArgumentList.Add(script);
            psi.ArgumentList.Add("-Versions");
            psi.ArgumentList.Add(selectedVersion);

            using var process = new Process { StartInfo = psi };
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) BeginInvoke(new Action(() => AppendLog(e.Data + Environment.NewLine))); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) BeginInvoke(new Action(() => AppendLog("[stderr] " + e.Data + Environment.NewLine))); };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            await Task.Delay(150);

            var exitCode = process.ExitCode;
            status.Text = exitCode == 0
                ? "Script completed for Revit " + selectedVersion + ". Check Build-Summary.csv and verify status is BUILT."
                : "Build script exited with code " + exitCode + ". Review the log.";
            AppendLog(Environment.NewLine + "Process exit code: " + exitCode + Environment.NewLine);
            MessageBox.Show(this,
                exitCode == 0 ? "The script completed. Check the log and Build-Summary.csv for BUILT versus SKIPPED." : "The build did not complete successfully. Review the output.",
                exitCode == 0 ? "Build script finished" : "Build failed",
                MessageBoxButtons.OK, exitCode == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            status.Text = "Could not run the build.";
            AppendLog(ex.ToString() + Environment.NewLine);
            MessageBox.Show(this, ex.Message, "Launcher error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            build.Enabled = true;
            browse.Enabled = true;
            version.Enabled = true;
        }
    }

    private void AppendLog(string value)
    {
        if (!IsDisposed) log.AppendText(value);
    }
}
