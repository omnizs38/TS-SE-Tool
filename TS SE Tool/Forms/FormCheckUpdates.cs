/*
   Original work copyright 2016-2022 LIPtoH and contributors.
   Maintenance modifications copyright 2026 omnizs38 and contributors.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
*/
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using TS_SE_Tool.Updates;
using TS_SE_Tool.Utilities;

namespace TS_SE_Tool
{
    public partial class FormCheckUpdates : Form
    {
        internal string[] NewVersion = { string.Empty, string.Empty };
        private readonly string formMode;
        private string releaseUrl = Web_Utilities.ReleasesUrl;

        public FormCheckUpdates(string formMode)
        {
            this.formMode = formMode ?? "check";
            InitializeComponent();
            ConfigureMaintainedLayout();
        }

        private void ConfigureMaintainedLayout()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ControlBox = true;
            MaximumSize = Size.Empty;
            MinimumSize = new Size(480, 280);
            ClientSize = new Size(520, 310);

            tableLayoutPanel1.Padding = new Padding(12);
            tableLayoutPanel1.RowStyles[0] = new RowStyle(SizeType.Absolute, 8F);
            tableLayoutPanel1.RowStyles[1] = new RowStyle(SizeType.Percent, 100F);
            tableLayoutPanel1.RowStyles[2] = new RowStyle(SizeType.Absolute, 8F);
            tableLayoutPanel1.RowStyles[3] = new RowStyle(SizeType.Absolute, 42F);
            tableLayoutPanel1.RowStyles[4] = new RowStyle(SizeType.Absolute, 0F);
            tableLayoutPanel1.RowStyles[5] = new RowStyle(SizeType.Absolute, 8F);
            tableLayoutPanel1.RowStyles[6] = new RowStyle(SizeType.Absolute, 42F);

            labelStatus.AutoSize = false;
            labelStatus.Dock = DockStyle.Fill;
            labelStatus.Padding = new Padding(12);
            labelStatus.TextAlign = ContentAlignment.MiddleCenter;
            buttonDownload.Margin = new Padding(32, 4, 32, 4);
            buttonOK.Margin = new Padding(32, 4, 32, 4);
            progressBarDownload.Visible = false;
        }

        private async void FormCheckUpdates_Load(object sender, EventArgs e)
        {
            buttonDownload.Text = "Open Releases";
            buttonDownload.Enabled = false;
            buttonDownload.Visible = true;
            buttonDownload.Click -= buttonDownload_Click;
            buttonDownload.Click += buttonDownload_Click;

            buttonOK.Text = "Close";
            buttonOK.Enabled = true;
            buttonOK.Click -= buttonOk_Click;
            buttonOK.Click += buttonOk_Click;

            labelStatus.Text = "Checking GitHub Releases…";

            try
            {
                using (CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(12)))
                {
                    GitHubReleaseInfo latest = await GitHubReleaseClient.GetLatestStableAsync(cancellation.Token);
                    if (latest == null)
                    {
                        labelStatus.Text = "No semantic stable release was found.\r\nCurrent version: " + GitHubReleaseClient.CurrentVersion;
                    }
                    else if (GitHubReleaseClient.IsNewer(latest))
                    {
                        NewVersion[0] = latest.TagName;
                        NewVersion[1] = latest.Url;
                        releaseUrl = latest.Url;
                        labelStatus.Text = BuildUpdateSummary(
                            "Update available: " + latest.TagName + "\r\nInstalled: " + GitHubReleaseClient.CurrentVersion,
                            latest.Notes);
                        buttonDownload.Text = "Open " + latest.TagName;
                    }
                    else
                    {
                        releaseUrl = latest.Url;
                        labelStatus.Text = "You are up to date.\r\nInstalled: " + GitHubReleaseClient.CurrentVersion;
                    }
                }
            }
            catch (Exception exception)
            {
                IO_Utilities.ErrorLogWriter("Update check failed: " + exception);
                labelStatus.Text = "Could not check GitHub Releases.\r\nYou can open the release page manually.";
            }
            finally
            {
                buttonDownload.Enabled = true;
            }
        }

        private static string BuildUpdateSummary(string heading, string notes)
        {
            string normalized = (notes ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Trim();
            if (normalized.Length == 0)
            {
                return heading;
            }
            if (normalized.Length > 700)
            {
                normalized = normalized.Substring(0, 697).TrimEnd() + "…";
            }
            return heading + "\r\n\r\nRelease notes:\r\n" + normalized.Replace("\n", "\r\n");
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonDownload_Click(object sender, EventArgs e)
        {
            if (!Web_Utilities.External.TryOpenUrl(releaseUrl))
            {
                MessageBox.Show("Could not open GitHub Releases.\r\n" + releaseUrl,
                    "Open releases", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.Equals(formMode, "startup", StringComparison.OrdinalIgnoreCase))
            {
                DialogResult = DialogResult.OK;
            }
            Close();
        }
    }
}
