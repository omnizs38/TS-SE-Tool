/* Copyright 2026 omnizs38 and contributors. Licensed under Apache-2.0. */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace TS_SE_Tool.Utilities
{
    // Per-file atomic publication with rollback on ordinary I/O failures. This is
    // deliberately NOT a cross-file transaction against power loss or game writes.
    internal static class SaveFileBatch
    {
        internal sealed class Entry
        {
            internal readonly string Path, Content, ExpectedHash;
            internal Entry(string path, string content, string expectedHash = null)
            { Path = System.IO.Path.GetFullPath(path); Content = content; ExpectedHash = expectedHash; }
        }
        private sealed class Pending
        {
            internal Entry Entry;
            internal string OriginalHash, WrittenHash, Stage, Backup;
        }
        internal static string Fingerprint(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                return Convert.ToHexString(SHA256.HashData(stream));
        }
        internal static void Write(Entry[] entries, Action<int> afterReplace = null)
        {
            if (entries == null || entries.Length == 0) throw new ArgumentException("No save files supplied.");
            if (entries.Any(e => e == null) || entries.Select(e => e.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != entries.Length)
                throw new ArgumentException("Save file paths must be unique.");
            var pending = new List<Pending>();
            var committed = new List<Pending>();
            try
            {
                foreach (Entry entry in entries)
                {
                    var file = new Pending { Entry = entry, OriginalHash = Fingerprint(entry.Path),
                        Backup = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(entry.Path),
                            System.IO.Path.GetFileNameWithoutExtension(entry.Path) + "_backup" + System.IO.Path.GetExtension(entry.Path)) };
                    pending.Add(file);
                    if (entry.ExpectedHash != null && entry.ExpectedHash != file.OriginalHash)
                        throw new IOException("The save changed since it was loaded: " + entry.Path);
                    if (entry.Content != null)
                    {
                        if (entry.Content.Length == 0) throw new InvalidDataException("Refusing to write an empty save: " + entry.Path);
                        file.Stage = entry.Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                        AtomicFile.WriteAllText(file.Stage, entry.Content);
                        file.WrittenHash = Fingerprint(file.Stage);
                    }
                }
                // Complete every backup before publishing any edited file.
                foreach (Pending file in pending)
                {
                    CopyAtomic(file.Entry.Path, file.Backup);
                    if (Fingerprint(file.Backup) != file.OriginalHash)
                        throw new IOException("The save changed while making backups: " + file.Entry.Path);
                }
                foreach (Pending file in pending)
                    if (Fingerprint(file.Entry.Path) != file.OriginalHash)
                        throw new IOException("The save changed before publication: " + file.Entry.Path);
                foreach (Pending file in pending.Where(f => f.Stage != null))
                {
                    if (Fingerprint(file.Entry.Path) != file.OriginalHash)
                        throw new IOException("The save changed during publication: " + file.Entry.Path);
                    File.Replace(file.Stage, file.Entry.Path, null);
                    committed.Add(file);
                    afterReplace?.Invoke(committed.Count);
                }
            }
            catch (Exception original)
            {
                var failures = new List<Exception> { original };
                foreach (Pending file in committed.AsEnumerable().Reverse())
                {
                    try
                    {
                        // Never overwrite a concurrent third-party change while recovering.
                        if (Fingerprint(file.Entry.Path) != file.WrittenHash)
                            throw new IOException("Rollback refused: another process changed " + file.Entry.Path);
                        if (Fingerprint(file.Backup) != file.OriginalHash)
                            throw new IOException("Rollback refused: backup was changed " + file.Backup);
                        CopyAtomic(file.Backup, file.Entry.Path);
                    }
                    catch (Exception rollback) { failures.Add(rollback); }
                }
                if (failures.Count > 1)
                    throw new AggregateException("Save failed and rollback was incomplete. Restore the matching *_backup.sii files before continuing.", failures);
                throw;
            }
            finally
            {
                foreach (Pending file in pending)
                    TryDelete(file.Stage);
            }
        }
        private static void CopyAtomic(string source, string target)
        {
            string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.Copy(source, temporary, false);
                using (FileStream stream = new FileStream(temporary, FileMode.Open, FileAccess.Write, FileShare.None)) stream.Flush(true);
                if (File.Exists(target)) File.Replace(temporary, target, null);
                else File.Move(temporary, target);
            }
            finally { TryDelete(temporary); }
        }
        private static void TryDelete(string path)
        {
            if (path == null) return;
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
