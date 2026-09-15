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
                Application.Run(new FormMain());
            }
            finally
            {
                TryWriteLog("--- END ---");
            }
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

            string message =
                "An unexpected error occurred. Details were written to errorlog.log.\r\n\r\n" +
                "Please report the problem at:\r\n" + Web_Utilities.IssuesUrl;

            try
            {
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                // The process may already be shutting down. Logging above is the fallback.
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
                // Error reporting must never throw a second exception.
            }
        }
    }
}
