/*
   Original work copyright 2016-2022 LIPtoH <liptoh.codebase@gmail.com>.
   Maintenance modifications copyright 2026 omnizs38 and contributors.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       https://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using TS_SE_Tool.Utilities;

namespace TS_SE_Tool
{
    public partial class FormSplash : Form
    {
        private readonly FormMain mainForm = Application.OpenForms.OfType<FormMain>().FirstOrDefault();

        public FormSplash()
        {
            InitializeComponent();

            if (mainForm != null)
            {
                mainForm.HelpTranslateFormMethod(this);
            }

            labelTSSE.Text = AssemblyData.AssemblyProduct;
            labelVersion.Text = AssemblyData.AssemblyVersion;
            labelSupportDeveloper.Visible = false;
            buttonSupportDeveloper.Visible = false;
        }

        private void FormSplash_Load(object sender, EventArgs e)
        {
            bool showReleaseLink = true;

            try
            {
                showReleaseLink = Properties.Settings.Default.CheckUpdatesOnStartup;
            }
            catch (Exception exception)
            {
                IO_Utilities.ErrorLogWriter("Could not read update preference: " + exception);
            }

            if (mainForm != null && !mainForm.TssetFoldersExist)
            {
                linkLabelNewVersion.Text = "Installation files are incomplete. Reinstall the latest release.";
                SetReleaseLinkStyle(Color.Crimson, FontStyle.Bold);
                showReleaseLink = true;
            }
            else if (showReleaseLink)
            {
                linkLabelNewVersion.Text = "Check GitHub Releases for updates";
                SetReleaseLinkStyle(ForeColor, FontStyle.Bold);
            }
            else
            {
                tableLayoutPanel2.RowStyles[3] = new RowStyle(SizeType.Absolute, 0F);
            }

            if (showReleaseLink)
            {
                linkLabelNewVersion.Click += linkLabelNewVersion_Click;
            }

            buttonOK.Text = "OK";
            buttonOK.Click += buttonOK_Click;
        }

        private void FormSplash_Shown(object sender, EventArgs e)
        {
        }

        private void linkLabelNewVersion_Click(object sender, EventArgs e)
        {
            OpenUrl(Web_Utilities.LatestReleaseUrl);
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
            OpenUrl(Web_Utilities.LatestReleaseUrl);
        }

        private void linkLabelHelpLocalPDF_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            const string file = "HowTo.pdf";
            if (File.Exists(file))
            {
                try
                {
                    System.Diagnostics.Process.Start(file);
                }
                catch (Exception exception)
                {
                    IO_Utilities.ErrorLogWriter("Could not open local help: " + exception);
                }
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
            if (linkLabelNewVersion.Links.Count > 0)
            {
                linkLabelNewVersion.Links[0].Enabled = true;
            }

            linkLabelNewVersion.LinkBehavior = LinkBehavior.AlwaysUnderline;
            linkLabelNewVersion.LinkColor = color;
            linkLabelNewVersion.DisabledLinkColor = color;
            linkLabelNewVersion.Font = new Font("Microsoft Sans Serif", 8.25F, style, GraphicsUnit.Point, 204);
        }

        private static void OpenUrl(string url)
        {
            if (!Web_Utilities.External.TryOpenUrl(url))
            {
                MessageBox.Show(
                    "Could not open the browser.\r\n" + url,
                    "Open link",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
