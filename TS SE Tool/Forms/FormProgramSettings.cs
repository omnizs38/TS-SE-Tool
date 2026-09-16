/*
   Copyright 2016-2026 TS SE Tool contributors.
   Licensed under the Apache License, Version 2.0.
*/
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TS_SE_Tool
{
    public partial class FormProgramSettings : Form
    {
        private readonly FormMain MainForm = Application.OpenForms.OfType<FormMain>().Single();
        private CheckBox checkBoxAutoInstallUpdates;

        public FormProgramSettings()
        {
            InitializeComponent();
            Icon = Utilities.Graphics_TSSET.IconFromImage(MainForm.ProgUIImgsDict["ProgramSettings"]);
            SuspendLayout();
            MainForm.HelpTranslateControl(this);
            MainForm.HelpTranslateFormMethod(this);
            AddAutoUpdateSetting();
            ResumeLayout();
            LoadSettings();
        }

        private void AddAutoUpdateSetting()
        {
            labelCheckUpdatesOnStartup.Text = "Check updates on startup";
            Label label = new Label { Text = "Download and install updates automatically", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) };
            checkBoxAutoInstallUpdates = new CheckBox { AutoSize = true, CheckAlign = ContentAlignment.MiddleCenter, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            tableLayoutPanel1.Controls.Add(label, 0, 3);
            tableLayoutPanel1.Controls.Add(checkBoxAutoInstallUpdates, 1, 3);
            tableLayoutPanel1.ColumnStyles[0].SizeType = SizeType.Percent;
            tableLayoutPanel1.ColumnStyles[0].Width = 85F;
            tableLayoutPanel1.ColumnStyles[1].SizeType = SizeType.Percent;
            tableLayoutPanel1.ColumnStyles[1].Width = 15F;
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(460, 260);
        }

        private void LoadSettings()
        {
            checkBoxShowSplashOnStartup.Checked = Properties.Settings.Default.ShowSplashOnStartup;
            checkBoxCheckUpdatesOnStartup.Checked = Properties.Settings.Default.CheckUpdatesOnStartup;
            checkBoxAutoInstallUpdates.Checked = Properties.Settings.Default.AutoInstallUpdates;
            UpdateDependencies();
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.ShowSplashOnStartup = checkBoxShowSplashOnStartup.Checked;
            Properties.Settings.Default.CheckUpdatesOnStartup = checkBoxCheckUpdatesOnStartup.Checked;
            Properties.Settings.Default.AutoInstallUpdates = checkBoxAutoInstallUpdates.Checked;
            Properties.Settings.Default.Save();
            Close();
        }

        private void buttonSave_Click(object sender, EventArgs e) { SaveSettings(); }
        private void checkBoxCheckUpdatesOnStartup_CheckedChanged(object sender, EventArgs e) { UpdateDependencies(); }

        private void UpdateDependencies()
        {
            if (checkBoxAutoInstallUpdates == null) return;
            checkBoxAutoInstallUpdates.Enabled = checkBoxCheckUpdatesOnStartup.Checked;
            if (checkBoxCheckUpdatesOnStartup.Checked)
            {
                checkBoxShowSplashOnStartup.Checked = true;
                checkBoxShowSplashOnStartup.Enabled = false;
            }
            else checkBoxShowSplashOnStartup.Enabled = true;
        }
    }
}
