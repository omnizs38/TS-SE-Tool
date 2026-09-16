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
using System.Windows.Forms;

namespace TS_SE_Tool
{
    public partial class FormMain
    {
        private bool maintainedUiInitialized;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            InitializeMaintainedInterface();
        }

        private void InitializeMaintainedInterface()
        {
            if (maintainedUiInitialized)
            {
                return;
            }
            maintainedUiInitialized = true;

            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(920, 700);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            tabControlMain.Padding = new Point(12, 5);

            // The maintained package no longer contains or requires the removed updater directory.
            TssetFoldersExist = new[] { "libs", "img", "lang" }.All(Directory.Exists);

            toolStripMenuItemDownload.Text = "Updates";
            checkSCSForumToolStripMenuItem.Visible = false;
            checkTMPForumToolStripMenuItem.Visible = false;
            toolStripMenuItemYouTubeVideo.Visible = false;
            toolStripMenuItemLocalPDF.Visible = File.Exists("HowTo.pdf");

            tableLayoutPanel1.Enabled = true;
            label5.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            label5.ForeColor = Color.DimGray;
            label5.Location = new Point(6, 176);
            label5.Size = new Size(558, 52);
            label5.Text = "Versioned convoy packages can share a truck position and complete GPS route. " +
                "Always load a save before importing and write the save after checking the route.";

            AddConvoyPackageButtons();
            AddCargoMarketButtons();
            ModernizeButtons(this);
        }

        private void AddConvoyPackageButtons()
        {
            FlowLayoutPanel panel = new FlowLayoutPanel
            {
                Name = "panelConvoyPackageActions",
                Location = new Point(6, 132),
                Size = new Size(558, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                WrapContents = false
            };

            panel.Controls.Add(CreateActionButton("Export package…", buttonConvoyExportPackage_Click));
            panel.Controls.Add(CreateActionButton("Import package…", buttonConvoyImportPackage_Click));
            panel.Controls.Add(CreateActionButton("Clear GPS route", buttonConvoyClearRoute_Click));
            tabPageConvoyTools.Controls.Add(panel);
            panel.BringToFront();
        }

        private void AddCargoMarketButtons()
        {
            label1.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            label1.ForeColor = Color.DimGray;
            label1.Location = new Point(6, 500);
            label1.Size = new Size(558, 30);
            label1.TextAlign = ContentAlignment.MiddleLeft;
            label1.Text = "Select a city and company to manage cargo offer seeds.";

            FlowLayoutPanel panel = new FlowLayoutPanel
            {
                Name = "panelCargoMarketClipboardActions",
                Location = new Point(406, 108),
                Size = new Size(156, 92),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            Button copy = CreateActionButton("Copy company seeds", buttonCargoMarketCopySeeds_Click);
            Button paste = CreateActionButton("Paste company seeds", buttonCargoMarketPasteSeeds_Click);
            copy.Width = 150;
            paste.Width = 150;
            panel.Controls.Add(copy);
            panel.Controls.Add(paste);
            tabPageCargoMarket.Controls.Add(panel);
            panel.BringToFront();
        }

        private static Button CreateActionButton(string text, EventHandler click)
        {
            Button button = new Button
            {
                AutoSize = false,
                Size = new Size(178, 29),
                Text = text,
                UseVisualStyleBackColor = true,
                FlatStyle = FlatStyle.System
            };
            button.Click += click;
            return button;
        }

        private static void ModernizeButtons(Control root)
        {
            foreach (Control control in root.Controls)
            {
                Button button = control as Button;
                if (button != null)
                {
                    button.FlatStyle = FlatStyle.System;
                }

                if (control.HasChildren)
                {
                    ModernizeButtons(control);
                }
            }
        }

        private void SetCargoMarketStatus(string message, bool error)
        {
            label1.ForeColor = error ? Color.Firebrick : Color.DimGray;
            label1.Text = message;
        }

        private void SetConvoyStatus(string message, bool error)
        {
            label5.ForeColor = error ? Color.Firebrick : Color.DimGray;
            label5.Text = message;
        }
    }
}
