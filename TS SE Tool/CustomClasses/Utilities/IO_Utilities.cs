/*
   Copyright 2016-2022 LIPtoH <liptoh.codebase@gmail.com>

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
*/
using System;
using System.Linq;
using System.Text;
using System.IO;

namespace TS_SE_Tool.Utilities
{
    class IO_Utilities
    {
        internal static void DirectoryCopy(string _sourceDirName, string _destDirName, bool _copySubDirs)
        {
            DirectoryCopy(_sourceDirName, _destDirName, _copySubDirs, null);
        }

        internal static void DirectoryCopy(string _sourceDirName, string _destDirName, bool _copySubDirs, string[] _fileList)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(_sourceDirName);
            if (!dirInfo.Exists)
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + _sourceDirName);

            if (!Directory.Exists(_destDirName))
                Directory.CreateDirectory(_destDirName);

            foreach (FileInfo file in dirInfo.GetFiles())
            {
                if (_fileList != null && !_fileList.Contains(file.Name))
                    continue;

                file.CopyTo(Path.Combine(_destDirName, file.Name), false);
            }

            if (_copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirInfo.GetDirectories())
                    DirectoryCopy(subdir.FullName, Path.Combine(_destDirName, subdir.Name), true, _fileList);
            }
        }

        internal static void LogWriter(string _message)
        {
            AppendLine("log.log", DateTime.Now + " " + _message);
        }

        internal static void ErrorLogWriter(string _error)
        {
            if (String.IsNullOrWhiteSpace(_error))
                return;

            // New save-format fields and blocks are preserved verbatim. They are useful
            // diagnostics, but are not application errors and must not pollute errorlog.log.
            if (_error.StartsWith("Save | Preserved field | ", StringComparison.Ordinal) ||
                _error.StartsWith("Save | New Data block | ", StringComparison.Ordinal))
            {
                CompatibilityLogWriter(_error);
                return;
            }

            try
            {
                using (StreamWriter writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), "errorlog.log"), true))
                {
                    writer.WriteLine(DateTime.Now + " | " + AssemblyData.AssemblyProduct + " - " + AssemblyData.AssemblyVersion + " | " +
                                     Globals.SelectedProfileName + " [ " + Globals.SelectedProfile + " ] >> " +
                                     Globals.SelectedSaveName + " [ " + Globals.SelectedSave + " ] ");
                    writer.WriteLine(_error + Environment.NewLine);
                }
            }
            catch
            {
            }
        }

        internal static void CompatibilityLogWriter(string _message)
        {
            AppendLine("compatibility.log", DateTime.Now + " | " + _message + Environment.NewLine);
        }

        private static void AppendLine(string _fileName, string _message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), _fileName), true))
                    writer.WriteLine(_message);
            }
            catch
            {
            }
        }

        internal static void WritePreviewTOBJ(string _path, string _name, string _pathToTGA)
        {
            WritePreviewTOBJ(_path + "\\" + _name + ".tobj", _pathToTGA);
        }

        internal static void WritePreviewTOBJ(string _pathToTOBJ, string _pathToTGA)
        {
            using (BinaryWriter binWriter = new BinaryWriter(File.Open(_pathToTOBJ, FileMode.Create)))
            {
                byte[] preview_tobj = new byte[] { 1, 10, 177, 112, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 3, 3, 2, 0, 2, 2, 2, 1, 0, 0, 0, 1, 0, 0 };
                binWriter.Write(preview_tobj);
                binWriter.Write((byte)_pathToTGA.Length);
                binWriter.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0 });
                binWriter.Write(Encoding.UTF8.GetBytes(_pathToTGA));
            }
        }
    }
}
