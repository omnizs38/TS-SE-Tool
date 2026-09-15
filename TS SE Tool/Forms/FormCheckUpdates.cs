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
using System.Windows.Forms;
using TS_SE_Tool.Utilities;

namespace TS_SE_Tool
{
    public partial class FormCheckUpdates : Form
    {
        // Kept for binary/source compatibility with callers from older UI code.
        internal string[] NewVersion = { string.Empty, string.Empty };

        public FormCheckUpdates(string formMode)
        {
            InitializeComponent();
            Size = new Size(360, 185);
        }

        private void FormCheckUpdates_Load(object sender, EventArgs e)
        {
            labelStatus.Text =
                "Updates are published only on GitHub Releases.\r\n" +
                "This build never downloads or executes updates in the background.";

            buttonDownload.Text = "Open GitHub Releases";
            buttonDownload.Visible = true;
            buttonDownload.Enabled = true;
            buttonDownload.Click += buttonDownload_Click;

            buttonOK.Text = "Close";
            buttonOK.Enabled = true;
            buttonOK.Click += buttonOk_Click;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonDownload_Click(object sender, EventArgs e)
        {
            if (!Web_Utilities.External.TryOpenUrl(Web_Utilities.LatestReleaseUrl))
            {
                MessageBox.Show(
                    "Could not open GitHub Releases.\r\n" + Web_Utilities.LatestReleaseUrl,
                    "Open releases",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
