/*
   Original work copyright 2016-2022 LIPtoH and contributors.
   Maintenance modifications copyright 2026 omnizs38 and contributors.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
*/
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using TS_SE_Tool.Updates;
using TS_SE_Tool.Utilities;

namespace TS_SE_Tool
{
    public partial class FormSplash : Form
    {
        private readonly FormMain mainForm = Application.OpenForms.OfType<FormMain>().FirstOrDefault();
        private string releaseUrl = Web_Utilities.ReleasesUrl;

        public FormSplash()
        {
            InitializeComponent();
            if (mainForm != null) mainForm.HelpTranslateFormMethod(this);

            labelTSSE.Text = AssemblyData.AssemblyProduct;
            labelVersion.Text = AssemblyData.AssemblyVersion;
            labelSupportDeveloper.Visible = false;
            buttonSupportDeveloper.Visible = false;
        }

        private async void FormSplash_Load(object sender, EventArgs e)
        {
            buttonOK.Text = "OK";
            buttonOK.Click -= buttonOK_Click;
            buttonOK.Click += buttonOK_Click;
            linkLabelNewVersion.Click -= linkLabelNewVersion_Click;
            linkLabelNewVersion.Click += linkLabelNewVersion_Click;

            if (mainForm != null && !mainForm.TssetFoldersExist)
            {
                linkLabelNewVersion.Text = "Installation files are incomplete. Reinstall the latest release.";
                SetReleaseLinkStyle(Color.Crimson, FontStyle.Bold);
                return;
            }

            bool checkUpdates;
            try
            {
                checkUpdates = Properties.Settings.Default.CheckUpdatesOnStartup;
            }
            catch
            {
                checkUpdates = true;
            }

            if (!checkUpdates)
            {
                HideUpdateRow();
                return;
            }

            linkLabelNewVersion.Text = "Checking GitHub Releases…";
            linkLabelNewVersion.Enabled = false;

            try
            {
                using (CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(12)))
                {
                    GitHubReleaseInfo latest = await GitHubReleaseClient.GetLatestStableAsync(cancellation.Token);
                    if (latest != null && GitHubReleaseClient.IsNewer(latest))
                    {
                        releaseUrl = latest.Url;
                        linkLabelNewVersion.Text = "Update available: " + latest.TagName;
                        linkLabelNewVersion.Enabled = true;
                        SetReleaseLinkStyle(Color.Crimson, FontStyle.Bold);
                    }
                    else
                    {
                        HideUpdateRow();
                    }
                }
            }
            catch (Exception exception)
            {
                IO_Utilities.ErrorLogWriter("Startup update check failed: " + exception);
                linkLabelNewVersion.Text = "Update check unavailable — open GitHub Releases";
                linkLabelNewVersion.Enabled = true;
                SetReleaseLinkStyle(ForeColor, FontStyle.Regular);
            }
        }

        private void FormSplash_Shown(object sender, EventArgs e)
        {
        }

        private void HideUpdateRow()
        {
            linkLabelNewVersion.Visible = false;
            if (tableLayoutPanel2.RowStyles.Count > 3)
            {
                tableLayoutPanel2.RowStyles[3] = new RowStyle(SizeType.Absolute, 0F);
            }
        }

        private void linkLabelNewVersion_Click(object sender, EventArgs e)
        {
            OpenUrl(releaseUrl);
        }

        private void linkFirst_Click(object sender, EventArgs e)
        {
            OpenUrl(Web_Utilities.RepositoryUrl);
        }

        private void linkSecond_Click(object sender, EventArgs e)
        {
            OpenUrl(Web_Utilities.IssuesUrl);
        }

        private void linkLabelGitHub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenUrl(Web_Utilities.ReleasesUrl);
        }

        private void linkLabelHelpLocalPDF_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            const string file = "HowTo.pdf";
            if (File.Exists(file))
            {
                try { System.Diagnostics.Process.Start(file); }
                catch (Exception exception) { IO_Utilities.ErrorLogWriter("Could not open local help: " + exception); }
            }
            else
            {
                OpenUrl(Web_Utilities.RepositoryUrl + "#readme");
            }
        }

        private void linkLabelHelpYouTube_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenUrl(Web_Utilities.RepositoryUrl + "#features");
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonOK_ClickCloseApp(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void buttonSupportDeveloper_Click(object sender, EventArgs e)
        {
            OpenUrl(Web_Utilities.IssuesUrl);
        }

        private void SetReleaseLinkStyle(Color color, FontStyle style)
        {
            linkLabelNewVersion.Visible = true;
            linkLabelNewVersion.LinkBehavior = LinkBehavior.AlwaysUnderline;
            linkLabelNewVersion.LinkColor = color;
            linkLabelNewVersion.DisabledLinkColor = color;
            linkLabelNewVersion.Font = new Font("Segoe UI", 9F, style, GraphicsUnit.Point);
        }

        private static void OpenUrl(string url)
        {
            if (!Web_Utilities.External.TryOpenUrl(url))
            {
                MessageBox.Show("Could not open the browser.\r\n" + url,
                    "Open link", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
