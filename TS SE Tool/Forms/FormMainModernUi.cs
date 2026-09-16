/*
   Original work copyright 2016-2022 LIPtoH and contributors.
   Maintenance modifications copyright 2026 omnizs38 and contributors.
   Licensed under the Apache License, Version 2.0.
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
        private FlowLayoutPanel convoyPackageActions;
        private FlowLayoutPanel cargoClipboardActions;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            InitializeMaintainedInterface();
        }

        private void InitializeMaintainedInterface()
        {
            if (maintainedUiInitialized) return;
            maintainedUiInitialized = true;

            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(920, 700);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            tabControlMain.Font = Font;
            tabControlMain.Padding = new Point(12, 5);
            TssetFoldersExist = new[] { "libs", "img", "lang" }.All(Directory.Exists);

            toolStripMenuItemDownload.Text = "Updates";
            checkSCSForumToolStripMenuItem.Visible = false;
            checkTMPForumToolStripMenuItem.Visible = false;
            toolStripMenuItemYouTubeVideo.Visible = false;
            toolStripMenuItemLocalPDF.Visible = File.Exists("HowTo.pdf");

            tableLayoutPanel1.Enabled = true;
            AddConvoyPackageButtons();
            AddCargoMarketButtons();
            ModernizeButtons(this);

            tabPageConvoyTools.Resize += delegate { LayoutConvoyTools(); };
            tabPageCargoMarket.Resize += delegate { LayoutCargoMarketDpi(); };
            DpiChanged += delegate
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    LayoutConvoyTools();
                    LayoutCargoMarketDpi();
                });
            };
            LayoutConvoyTools();
            LayoutCargoMarketDpi();
        }

        private int DpiPx(int logicalPixels)
        {
            return Math.Max(1, (int)Math.Round(logicalPixels * Math.Max(1f, DeviceDpi / 96f)));
        }

        private void AddConvoyPackageButtons()
        {
            convoyPackageActions = new FlowLayoutPanel
            {
                Name = "panelConvoyPackageActions",
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            convoyPackageActions.Controls.Add(CreateActionButton("Export package…", buttonConvoyExportPackage_Click));
            convoyPackageActions.Controls.Add(CreateActionButton("Import package…", buttonConvoyImportPackage_Click));
            convoyPackageActions.Controls.Add(CreateActionButton("Clear GPS route", buttonConvoyClearRoute_Click));
            tabPageConvoyTools.Controls.Add(convoyPackageActions);
            convoyPackageActions.BringToFront();

            label5.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            label5.ForeColor = Color.DimGray;
            label5.AutoEllipsis = true;
            label5.TextAlign = ContentAlignment.TopLeft;
            label5.Text = "Versioned convoy packages can share a truck position and complete GPS route. " +
                "Always load a save before importing and write the save after checking the route.";
        }

        private void LayoutConvoyTools()
        {
            if (convoyPackageActions == null || tabPageConvoyTools.ClientSize.Width < 1) return;
            int margin = DpiPx(12);
            int gap = DpiPx(8);
            int width = Math.Max(DpiPx(420), tabPageConvoyTools.ClientSize.Width - margin * 2);
            int standardRow = DpiPx(34);
            int largeRow = DpiPx(62);

            tableLayoutPanel1.Location = new Point(margin, margin);
            tableLayoutPanel1.Size = new Size(width, standardRow * 2 + largeRow);
            tableLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel1.ColumnStyles.Clear();
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.RowStyles.Clear();
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, standardRow));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, standardRow));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, largeRow));

            int actionsTop = tableLayoutPanel1.Bottom + gap;
            convoyPackageActions.SetBounds(margin, actionsTop, width, DpiPx(36));
            int actionWidth = Math.Max(DpiPx(120), (width - gap * 2) / 3);
            for (int index = 0; index < convoyPackageActions.Controls.Count; index++)
            {
                convoyPackageActions.Controls[index].Size = new Size(actionWidth, DpiPx(32));
                convoyPackageActions.Controls[index].Margin = new Padding(index == 0 ? 0 : gap, 0, 0, 0);
            }

            label5.SetBounds(margin, convoyPackageActions.Bottom + gap, width, DpiPx(52));
            label5.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        }

        private void AddCargoMarketButtons()
        {
            label1.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            label1.ForeColor = Color.DimGray;
            label1.TextAlign = ContentAlignment.MiddleLeft;
            label1.AutoEllipsis = true;
            label1.Text = "Select a city and company to manage cargo offer seeds.";

            cargoClipboardActions = new FlowLayoutPanel
            {
                Name = "panelCargoMarketClipboardActions",
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            cargoClipboardActions.Controls.Add(CreateActionButton("Copy company seeds", buttonCargoMarketCopySeeds_Click));
            cargoClipboardActions.Controls.Add(CreateActionButton("Paste company seeds", buttonCargoMarketPasteSeeds_Click));
            tabPageCargoMarket.Controls.Add(cargoClipboardActions);
            cargoClipboardActions.BringToFront();
        }

        private void LayoutCargoMarketDpi()
        {
            if (cargoClipboardActions == null || tabPageCargoMarket.ClientSize.Width < 1) return;
            int margin = DpiPx(12);
            int gap = DpiPx(10);
            int labelWidth = DpiPx(58);
            int width = Math.Max(DpiPx(440), tabPageCargoMarket.ClientSize.Width - margin * 2);
            int columnWidth = (width - gap) / 2;
            int fieldWidth = Math.Max(DpiPx(120), columnWidth - labelWidth);
            int rowHeight = DpiPx(28);

            SetBounds(labelCargoMarketSource, margin, DpiPx(8), width, DpiPx(20));
            SetBounds(labelCargoMarketCity, margin, DpiPx(32), labelWidth, rowHeight);
            SetBounds(comboBoxCargoMarketSourceCity, margin + labelWidth, DpiPx(28), fieldWidth, rowHeight);
            SetBounds(labelCargoMarketCompany, margin + columnWidth + gap, DpiPx(32), labelWidth, rowHeight);
            SetBounds(comboBoxCargoMarketSourceCompany, margin + columnWidth + gap + labelWidth, DpiPx(28), fieldWidth, rowHeight);

            SetBounds(buttonCargoMarketResetCargoCity, margin + labelWidth, DpiPx(62), fieldWidth, rowHeight);
            SetBounds(buttonCargoMarketResetCargoCompany, margin + columnWidth + gap + labelWidth, DpiPx(62), fieldWidth, rowHeight);
            SetBounds(buttonCargoMarketRandomizeCargoCity, margin + labelWidth, DpiPx(96), fieldWidth, rowHeight);
            SetBounds(buttonCargoMarketRandomizeCargoCompany, margin + columnWidth + gap + labelWidth, DpiPx(96), fieldWidth, rowHeight);

            int clipboardTop = DpiPx(132);
            cargoClipboardActions.SetBounds(margin + labelWidth, clipboardTop, width - labelWidth, DpiPx(34));
            int clipboardWidth = Math.Max(DpiPx(150), (cargoClipboardActions.Width - gap) / 2);
            for (int index = 0; index < cargoClipboardActions.Controls.Count; index++)
            {
                cargoClipboardActions.Controls[index].Size = new Size(clipboardWidth, DpiPx(30));
                cargoClipboardActions.Controls[index].Margin = new Padding(index == 0 ? 0 : gap, 0, 0, 0);
            }

            int seedTop = DpiPx(174);
            int seedHeight = DpiPx(138);
            SetBounds(listBoxCargoMarketSourceCargoSeeds, margin + labelWidth, seedTop, width - labelWidth, seedHeight);

            int trailerTop = seedTop + seedHeight + DpiPx(14);
            SetBounds(labelCMTrailerType, margin, trailerTop + DpiPx(5), labelWidth, rowHeight);
            SetBounds(comboBoxCMTrailerTypes, margin + labelWidth, trailerTop, width - labelWidth, rowHeight);

            int cargoTop = trailerTop + DpiPx(40);
            int statusHeight = DpiPx(38);
            int cargoHeight = Math.Max(DpiPx(100), tabPageCargoMarket.ClientSize.Height - cargoTop - statusHeight - margin - gap);
            SetBounds(listBoxCargoMarketCargoListForCompany, margin + labelWidth, cargoTop, width - labelWidth, cargoHeight);
            SetBounds(label1, margin, cargoTop + cargoHeight + gap, width, statusHeight);

            cargoClipboardActions.BringToFront();
        }

        private static void SetBounds(Control control, int x, int y, int width, int height)
        {
            if (control == null) return;
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            control.SetBounds(x, y, Math.Max(1, width), Math.Max(1, height));
        }

        private static Button CreateActionButton(string text, EventHandler click)
        {
            Button button = new Button
            {
                AutoSize = false,
                Size = new Size(178, 29),
                Text = text,
                UseVisualStyleBackColor = true,
                FlatStyle = FlatStyle.System,
                AutoEllipsis = true
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
                    button.AutoEllipsis = true;
                }
                if (control.HasChildren) ModernizeButtons(control);
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
