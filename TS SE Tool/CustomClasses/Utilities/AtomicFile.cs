/*
   Copyright 2026 omnizs38 and contributors.
   Licensed under the Apache License, Version 2.0.
*/
using System;
using System.IO;
using System.Text;

namespace TS_SE_Tool.Utilities
{
    internal static class AtomicFile
    {
        internal static void WriteAllText(string path, string content)
        {
            Write(path, writer => writer.Write(content));
        }

        internal static void Write(string path, Action<StreamWriter> write)
        {
            if (write == null) throw new ArgumentNullException(nameof(write));
            string target = Path.GetFullPath(path);
            string temporary = Path.Combine(Path.GetDirectoryName(target),
                "." + Path.GetFileName(target) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (FileStream stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true))
                    {
                        write(writer);
                        writer.Flush();
                    }
                    stream.Flush(true);
                }

                // Never delete the original first. If replacement is unsupported or
                // denied, fail with the original intact rather than falling back to copy/delete.
                if (File.Exists(target)) File.Replace(temporary, target, null);
                else File.Move(temporary, target);
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (IOException) { /* Cleanup must not hide the write failure. */ }
                catch (UnauthorizedAccessException) { /* Leave the original exception intact. */ }
            }
        }
    }
}
