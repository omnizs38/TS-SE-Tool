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
            Size = new Size(440, 210);
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
                        labelStatus.Text = "Update available: " + latest.TagName + "\r\nInstalled: " + GitHubReleaseClient.CurrentVersion;
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
