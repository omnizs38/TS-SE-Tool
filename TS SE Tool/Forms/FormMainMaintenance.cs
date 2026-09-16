/*
   Maintenance fixes for ETS2/ATS 1.60+ and Windows 10/11 high-DPI displays.
   Copyright 2026 omnizs38 and contributors.
   Licensed under the Apache License, Version 2.0.
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TS_SE_Tool.Utilities;

namespace TS_SE_Tool
{
    public partial class FormMain
    {
        private bool maintenanceFixesInitialized;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            InitializeMaintenanceFixes();
        }

        private void InitializeMaintenanceFixes()
        {
            if (maintenanceFixesInitialized) return;
            maintenanceFixesInitialized = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScroll = true;
            MaximumSize = Size.Empty;
            ApplyResponsiveMinimumSize();
            ConsolidateSettingsMenu();
            ConfigureCompanyDpiLayout();
            ConfigureAutomaticVehicleSelection();
            AddMissingFallbackIcons(this);

            DpiChanged += delegate
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    ApplyResponsiveMinimumSize();
                    ConfigureCompanyDpiLayout();
                    AddMissingFallbackIcons(this);
                });
            };
            tabControlMain.SelectedIndexChanged += delegate
            {
                if (tabControlMain.SelectedTab == tabPageCompany) RefreshCompanyPresentation();
            };
            tableLayoutPanelCompanyMain.EnabledChanged += delegate
            {
                if (tableLayoutPanelCompanyMain.Enabled) BeginInvoke((MethodInvoker)RefreshCompanyPresentation);
            };
            buttonMainLoadSave.Click += delegate
            {
                if (workerLoadSaveFile == null) return;
                workerLoadSaveFile.RunWorkerCompleted -= worker_RunWorkerCompleted;
                workerLoadSaveFile.RunWorkerCompleted += MaintenanceLoadCompleted;
            };
        }

        private void MaintenanceLoadCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                IO_Utilities.ErrorLogWriter("Profile load failed" + Environment.NewLine + e.Error);
                ToggleMainControlsAccess(true);
                ToggleControlsAccess(false);
                MessageBox.Show("The profile could not be loaded. Details were written to errorlog.log.", "Profile load", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            List<string> preserved = null;
            if (!e.Cancelled && SiiNunitData != null && SiiNunitData.UnidentifiedBlocks.Count > 0)
            {
                preserved = new List<string>(SiiNunitData.UnidentifiedBlocks);
                SiiNunitData.UnidentifiedBlocks.Clear();
            }
            try { worker_RunWorkerCompleted(sender, e); }
            finally
            {
                if (preserved != null)
                {
                    SiiNunitData.UnidentifiedBlocks.AddRange(preserved);
                    IO_Utilities.LogWriter("Profile load | preserved " + preserved.Count + " unmodelled save blocks unchanged");
                }
            }
        }

        private void ApplyResponsiveMinimumSize()
        {
            Rectangle work = Screen.FromControl(this).WorkingArea;
            float scale = Math.Max(1f, DeviceDpi / 96f);
            int width = Math.Min((int)Math.Round(850 * scale), Math.Max(760, work.Width - 32));
            int height = Math.Min((int)Math.Round(660 * scale), Math.Max(600, work.Height - 32));
            MinimumSize = new Size(width, height);
            if (Width > work.Width || Height > work.Height)
                Size = new Size(Math.Min(Width, work.Width - 16), Math.Min(Height, work.Height - 16));
        }

        private void ConsolidateSettingsMenu()
        {
            ToolStripMenuItem startup = toolStripMenuItemProgramSettings;
            ToolStripMenuItem editor = toolStripMenuItemSettings;
            ToolStripMenuItem program = startup.OwnerItem as ToolStripMenuItem;
            if (program == null || editor.OwnerItem != program) return;
            program.DropDownItems.Remove(startup);
            program.DropDownItems.Remove(editor);
            ToolStripMenuItem settings = new ToolStripMenuItem("Settings") { Name = "toolStripMenuItemUnifiedSettings", Image = editor.Image };
            editor.Text = "Editor settings";
            startup.Text = "Startup and updates";
            settings.DropDownItems.Add(editor);
            settings.DropDownItems.Add(startup);
            program.DropDownItems.Insert(0, settings);
        }

        private void ConfigureCompanyDpiLayout()
        {
            tableLayoutPanelCompanyMain.RowStyles[0].Height = Math.Max(112, (int)Math.Round(112 * DeviceDpi / 96f));
            tableLayoutPanelCompanyDataTopRow.ColumnStyles.Clear();
            tableLayoutPanelCompanyDataTopRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Math.Max(112, (int)Math.Round(112 * DeviceDpi / 96f))));
            tableLayoutPanelCompanyDataTopRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableLayoutPanelCompanyDataTopRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tableLayoutPanelCompanyDataTopRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pictureBoxCompanyLogo.MinimumSize = Size.Empty;
            pictureBoxCompanyLogo.MaximumSize = Size.Empty;
            pictureBoxCompanyLogo.Dock = DockStyle.Fill;
            pictureBoxCompanyLogo.Margin = new Padding(8);
            pictureBoxCompanyLogo.SizeMode = PictureBoxSizeMode.Zoom;
            textBoxUserCompanyCompanyName.Dock = DockStyle.Fill;
            textBoxUserCompanyMoneyAccount.Dock = DockStyle.Fill;
            comboBoxUserCompanyHQcity.Dock = DockStyle.Fill;
        }

        private void RefreshCompanyPresentation()
        {
            if (MainSaveFileProfileData == null) return;
            if (textBoxUserCompanyCompanyName.Text.Contains("\\x"))
                textBoxUserCompanyCompanyName.Text = TextUtilities.FromUtfHexToString(textBoxUserCompanyCompanyName.Text);
            string logo = (MainSaveFileProfileData.Logo ?? string.Empty).Trim().Trim('"');
            if (logo.Length == 0) return;
            string root = Path.Combine("img", GameType, "player_logo");
            string[] candidates = { Path.Combine(root, logo + ".dds"), Path.Combine(root, "logo_" + logo + ".dds"), Path.Combine(root, logo.Replace("player_logo.", string.Empty) + ".dds") };
            string path = candidates.FirstOrDefault(File.Exists);
            if (path == null) return;
            try { pictureBoxCompanyLogo.Image = Graphics_TSSET.ddsImgLoader(path, 94, 94).images[0]; }
            catch (Exception ex) { IO_Utilities.LogWriter("Company logo could not be loaded: " + ex.Message); }
        }

        private void ConfigureAutomaticVehicleSelection()
        {
            comboBoxUserTruckCompanyTrucks.DataSourceChanged += AutomaticVehicleSelection;
            comboBoxUserTrailerCompanyTrailers.DataSourceChanged += AutomaticVehicleSelection;
        }

        private void AutomaticVehicleSelection(object sender, EventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo == null || !combo.IsHandleCreated) return;
            combo.BeginInvoke((MethodInvoker)delegate
            {
                List<object> values = new List<object>();
                foreach (object item in combo.Items)
                {
                    DataRowView row = item as DataRowView;
                    if (row == null || row.Row.ItemArray.Length == 0) continue;
                    object value = row.Row[0];
                    if (value != null && value != DBNull.Value && value.ToString() != "null") values.Add(value);
                }
                if (values.Count == 1) { combo.Enabled = true; combo.SelectedValue = values[0]; }
            });
        }

        private static void AddMissingFallbackIcons(Control root)
        {
            foreach (Control control in root.Controls)
            {
                Button button = control as Button;
                if (button != null && string.IsNullOrWhiteSpace(button.Text) && button.Image == null && button.BackgroundImage == null)
                {
                    button.BackgroundImage = SystemIcons.Application.ToBitmap();
                    button.BackgroundImageLayout = ImageLayout.Zoom;
                }
                if (control.HasChildren) AddMissingFallbackIcons(control);
            }
        }
    }
}
