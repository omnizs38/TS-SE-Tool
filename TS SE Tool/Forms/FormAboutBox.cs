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
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TS_SE_Tool.Utilities;

namespace TS_SE_Tool
{
    internal partial class FormAboutBox : Form
    {
        private readonly FormMain mainForm = Application.OpenForms.OfType<FormMain>().FirstOrDefault();

        public FormAboutBox()
        {
            InitializeComponent();
            PopulateFormControls();

            if (mainForm != null)
            {
                Icon = Graphics_TSSET.IconFromImage(mainForm.ProgUIImgsDict["Info"]);
                mainForm.HelpTranslateFormMethod(this);
                mainForm.HelpTranslateControlExt(this, AssemblyData.AssemblyTitle);
                mainForm.HelpTranslateControlExt(labelVersion, AssemblyData.AssemblyVersion);
            }
        }

        private void PopulateFormControls()
        {
            buttonSupportDeveloper.Visible = false;
            labelProductName.Text = AssemblyData.AssemblyProduct;
            labelCopyright.Text = AssemblyData.AssemblyCopyright;
            labelETS2version.Text = "61 - 97 (ETS2 1.43.x - 1.61.x)";
            labelATSversion.Text = "61 - 97 (ATS 1.43.x - 1.61.x)";

            StringBuilder description = new StringBuilder();
            description.AppendLine("Maintained at:");
            description.AppendLine(Web_Utilities.RepositoryUrl);
            description.AppendLine();
            description.AppendLine("Issues and support:");
            description.AppendLine(Web_Utilities.IssuesUrl);
            description.AppendLine();
            description.AppendLine("Third-party components:");
            description.AppendLine("SII_Decrypt — github.com/ncs-sniper/SII_Decrypt");
            description.AppendLine("PsColorPicker — github.com/exectails/PsColorPicker");
            description.AppendLine("SharpZipLib — github.com/icsharpcode/SharpZipLib");
            description.AppendLine("SqlCeBulkCopy — github.com/ErikEJ/SqlCeBulkCopy");
            description.AppendLine("DDSImageParser, TGASharpLib, FlexibleMessageBox");
            description.AppendLine();
            description.AppendLine("See LICENSE and NOTICE for required attribution.");
            textBoxDescription.Text = description.ToString();
        }

        private void buttonSupportDeveloper_Click(object sender, EventArgs e)
        {
            Web_Utilities.External.TryOpenUrl(Web_Utilities.IssuesUrl);
        }
    }
}
