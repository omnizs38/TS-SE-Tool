/*
   Original work copyright 2016-2022 LIPtoH and contributors.
   Maintenance modifications copyright 2026 omnizs38 and contributors.

   Licensed under the Apache License, Version 2.0 (the "License");
*/
using System;
using System.Drawing;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using TS_SE_Tool.Utilities;

namespace TS_SE_Tool
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0 && string.Equals(args[0], "--selftest", StringComparison.OrdinalIgnoreCase))
            {
                Environment.ExitCode = Diagnostics.SelfTest.Run(args);
                return;
            }

            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            Application.ThreadException += UIThreadException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            try
            {
                IO_Utilities.LogWriter("--- START ---");
                IO_Utilities.LogWriter(AssemblyData.AssemblyProduct + " - " + AssemblyData.AssemblyVersion);
                DetectEnviroment.DetectOS();
                DetectEnviroment.Get45PlusFromRegistry();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                FormMain mainForm = new FormMain();
                ConfigureResponsiveUi(mainForm);
                ConfigureJobManagement(mainForm);
                Application.Run(mainForm);
            }
            finally
            {
                IO_Utilities.LogWriter("--- END ---");
            }
        }

        private static void ConfigureResponsiveUi(FormMain form)
        {
            form.MaximumSize = Size.Empty;
            form.MinimumSize = new Size(850, 660);
            form.Size = new Size(Math.Max(form.Width, 1000), Math.Max(form.Height, 700));

            TabPage cargoPage = Find<TabPage>(form, "tabPageCargoMarket");
            if (cargoPage == null) return;
            Label workInProgress = Find<Label>(cargoPage, "label1");
            if (workInProgress != null) workInProgress.Visible = false;
            cargoPage.Resize += delegate { LayoutCargoMarket(cargoPage); };
            LayoutCargoMarket(cargoPage);
        }

        private static void ConfigureJobManagement(FormMain form)
        {
            TabPage page = Find<TabPage>(form, "tabPageFreightMarket");
            ListBox jobs = Find<ListBox>(page, "listBoxFreightMarketAddedJobs");
            if (page == null || jobs == null) return;

            FlowLayoutPanel bar = new FlowLayoutPanel();
            bar.Name = "panelJobManagement";
            bar.Dock = DockStyle.Bottom;
            bar.Height = 42;
            bar.Padding = new Padding(6);
            bar.WrapContents = false;
            bar.FlowDirection = FlowDirection.LeftToRight;

            bar.Controls.Add(MakeButton("Edit selected", delegate { InvokeJobAction(form, "FM_JobList_Edit", jobs); }));
            bar.Controls.Add(MakeButton("Delete selected", delegate { InvokeJobAction(form, "FM_JobList_Delete", jobs); }));
            bar.Controls.Add(MakeButton("Assign selected", delegate { AssignSelectedJob(form, jobs); }));
            bar.Controls.Add(MakeButton("Cancel current job", delegate { CancelCurrentJob(form, true); }));
            page.Controls.Add(bar);
            bar.BringToFront();
        }

        private static Button MakeButton(string text, EventHandler click)
        {
            Button button = new Button();
            button.AutoSize = true;
            button.MinimumSize = new Size(118, 28);
            button.Text = text;
            button.UseVisualStyleBackColor = true;
            button.Click += click;
            return button;
        }

        private static void InvokeJobAction(FormMain form, string methodName, ListBox jobs)
        {
            if (jobs.SelectedItem == null)
            {
                MessageBox.Show("Select a job first.", "Job management", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MethodInfo method = typeof(FormMain).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method != null) method.Invoke(form, null);
        }

        private static void AssignSelectedJob(FormMain form, ListBox jobs)
        {
            JobAdded selected = jobs.SelectedItem as JobAdded;
            if (selected == null)
            {
                MessageBox.Show("Select a job first.", "Job management", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (form.SiiNunitData == null) return;
            if (form.SiiNunitData.Player.current_job != "null")
            {
                DialogResult replace = MessageBox.Show("A job is already active. Cancel it and select this job?", "Replace current job", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (replace != DialogResult.Yes) return;
                CancelCurrentJob(form, false);
            }

            Save.Items.Job_Info job = new Save.Items.Job_Info();
            job.cargo = "cargo." + selected.Cargo;
            job.source_company = "company.volatile." + selected.SourceCompany + "." + selected.SourceCity;
            job.target_company = "company.volatile." + selected.DestinationCompany + "." + selected.DestinationCity;
            job.is_articulated = selected.Type == 2;
            job.is_cargo_market_job = false;
            job.start_time = (int)form.SiiNunitData.Economy.game_time;
            job.planned_distance_km = selected.Distance;
            job.ferry_time = selected.Ferrytime;
            job.ferry_price = selected.Ferryprice;
            job.urgency = selected.Urgency;
            job.special = "null";
            job.units_count = selected.UnitsCount;
            job.fill_ratio = 1;

            string id = form.GetSpareNameless();
            form.SiiNunitData.SiiNitems.Add(id, job);
            form.SiiNunitData.Player.selected_job = id;
            MessageBox.Show("The selected job will be available for assignment after saving and loading the profile in the game.", "Job selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void CancelCurrentJob(FormMain form, bool confirm)
        {
            if (form.SiiNunitData == null) return;
            string id = form.SiiNunitData.Player.current_job;
            if (String.IsNullOrEmpty(id) || id == "null")
            {
                if (confirm) MessageBox.Show("There is no active job.", "Job management", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (confirm && MessageBox.Show("Cancel the active job? A backup is recommended.", "Cancel current job", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            Save.Items.Player_Job current = form.SiiNunitData.SiiNitems[id] as Save.Items.Player_Job;
            if (current != null)
            {
                IgnoreBlock(form, current.company_truck);
                IgnoreBlock(form, current.company_trailer);
                IgnoreBlock(form, current.special);
            }
            IgnoreBlock(form, id);
            form.SiiNunitData.Player.current_job = "null";
            form.SiiNunitData.Player.assigned_trailer_connected = false;
            form.SiiNunitData.Player.my_trailer_attached = false;
            if (confirm) MessageBox.Show("The active job was cancelled. Save the profile to apply the change.", "Job cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void IgnoreBlock(FormMain form, string id)
        {
            if (!String.IsNullOrEmpty(id) && id != "null" && form.SiiNunitData.SiiNitems.ContainsKey(id) && !form.SiiNunitData.NamelessIgnoreList.Contains(id))
                form.SiiNunitData.NamelessIgnoreList.Add(id);
        }

        private static void LayoutCargoMarket(TabPage page)
        {
            int margin = 16, gap = 12, labelWidth = 70;
            int width = Math.Max(420, page.ClientSize.Width - margin * 2);
            int columnWidth = (width - gap) / 2;
            int fieldWidth = Math.Max(100, columnWidth - labelWidth);
            Place(Find<Label>(page, "labelCargoMarketSource"), margin, 14, width, 20);
            Place(Find<Label>(page, "labelCargoMarketCity"), margin, 44, labelWidth, 24);
            Place(Find<ComboBox>(page, "comboBoxCargoMarketSourceCity"), margin + labelWidth, 40, fieldWidth, 28);
            Place(Find<Label>(page, "labelCargoMarketCompany"), margin + columnWidth + gap, 44, labelWidth, 24);
            Place(Find<ComboBox>(page, "comboBoxCargoMarketSourceCompany"), margin + columnWidth + gap + labelWidth, 40, fieldWidth, 28);
            Place(Find<Button>(page, "buttonCargoMarketResetCargoCity"), margin + labelWidth, 76, fieldWidth, 30);
            Place(Find<Button>(page, "buttonCargoMarketResetCargoCompany"), margin + columnWidth + gap + labelWidth, 76, fieldWidth, 30);
            Place(Find<Button>(page, "buttonCargoMarketRandomizeCargoCity"), margin + labelWidth, 112, fieldWidth, 30);
            Place(Find<Button>(page, "buttonCargoMarketRandomizeCargoCompany"), margin + columnWidth + gap + labelWidth, 112, fieldWidth, 30);
            Place(Find<ListBox>(page, "listBoxCargoMarketSourceCargoSeeds"), margin + labelWidth, 154, width - labelWidth, 150);
            Place(Find<Label>(page, "labelCMTrailerType"), margin, 322, labelWidth, 24);
            Place(Find<ComboBox>(page, "comboBoxCMTrailerTypes"), margin + labelWidth, 318, width - labelWidth, 28);
            Place(Find<ListBox>(page, "listBoxCargoMarketCargoListForCompany"), margin + labelWidth, 358, width - labelWidth, Math.Max(100, page.ClientSize.Height - 374));
        }

        private static T Find<T>(Control root, string name) where T : Control
        {
            if (root == null) return null;
            Control[] controls = root.Controls.Find(name, true);
            return controls.Length == 0 ? null : controls[0] as T;
        }

        private static void Place(Control control, int x, int y, int width, int height)
        {
            if (control == null) return;
            control.SetBounds(x, y, Math.Max(1, width), Math.Max(1, height));
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        }

        private static void UIThreadException(object sender, ThreadExceptionEventArgs e) { ReportUnexpectedError(e.Exception, "Windows Forms error"); }
        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ReportUnexpectedError(e.ExceptionObject as Exception ?? new InvalidOperationException("An unknown non-UI error terminated the application."), "Application error");
        }
        private static void ReportUnexpectedError(Exception exception, string caption)
        {
            try { IO_Utilities.ErrorLogWriter(exception.ToString()); } catch { }
            try { MessageBox.Show("An unexpected error occurred. Details were written to errorlog.log.\r\n\r\nPlease report the problem at:\r\n" + Web_Utilities.IssuesUrl, caption, MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
        }
    }
}
