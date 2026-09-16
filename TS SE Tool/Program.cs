/*
   Original work copyright 2016-2022 LIPtoH and contributors.
   Maintenance modifications copyright 2026 omnizs38 and contributors.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
*/
using System;
using System.Drawing;
using System.Net;
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
                Application.Run(mainForm);
            }
            finally
            {
                TryWriteLog("--- END ---");
            }
        }

        private static void ConfigureResponsiveUi(FormMain form)
        {
            form.MaximumSize = Size.Empty;
            form.MinimumSize = new Size(850, 660);
            form.Size = new Size(Math.Max(form.Width, 1000), Math.Max(form.Height, 700));

            TabPage cargoPage = Find<TabPage>(form, "tabPageCargoMarket");
            if (cargoPage == null)
                return;

            Label workInProgress = Find<Label>(cargoPage, "label1");
            if (workInProgress != null)
                workInProgress.Visible = false;

            EventHandler layout = delegate { LayoutCargoMarket(cargoPage); };
            cargoPage.Resize += layout;
            LayoutCargoMarket(cargoPage);
        }

        private static void LayoutCargoMarket(TabPage page)
        {
            int margin = 16;
            int gap = 12;
            int labelWidth = 70;
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
            Place(Find<ListBox>(page, "listBoxCargoMarketCargoListForCompany"), margin + labelWidth, 358, width - labelWidth,
                Math.Max(100, page.ClientSize.Height - 374));
        }

        private static T Find<T>(Control root, string name) where T : Control
        {
            Control[] controls = root.Controls.Find(name, true);
            return controls.Length == 0 ? null : controls[0] as T;
        }

        private static void Place(Control control, int x, int y, int width, int height)
        {
            if (control == null)
                return;

            control.SetBounds(x, y, Math.Max(1, width), Math.Max(1, height));
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        }

        private static void UIThreadException(object sender, ThreadExceptionEventArgs eventArgs)
        {
            ReportUnexpectedError(eventArgs.Exception, "Windows Forms error");
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
        {
            Exception exception = eventArgs.ExceptionObject as Exception
                ?? new InvalidOperationException("An unknown non-UI error terminated the application.");
            ReportUnexpectedError(exception, "Application error");
        }

        private static void ReportUnexpectedError(Exception exception, string caption)
        {
            TryWriteLog(exception.ToString());
            string message = "An unexpected error occurred. Details were written to errorlog.log.\r\n\r\n" +
                "Please report the problem at:\r\n" + Web_Utilities.IssuesUrl;

            try
            {
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
            }
        }

        private static void TryWriteLog(string message)
        {
            try
            {
                IO_Utilities.ErrorLogWriter(message);
            }
            catch
            {
            }
        }
    }
}
