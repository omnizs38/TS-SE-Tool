/*
   Copyright 2026 omnizs38 and contributors.
   Licensed under the Apache License, Version 2.0.
*/
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace TS_SE_Tool.Updates
{
    // Keep update validation independent of HTTP, JSON serializers, and WinForms
    // so the same production rules can be verified on legacy and modern runtimes.
    internal static class ReleaseValidation
    {
        private static readonly Regex SemanticTag = new Regex(@"^v(?<major>\d+)\.(?<minor>\d+)(?:\.(?<patch>\d+))?(?:\.(?<revision>\d+))?\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        internal static bool TryParseSemanticTag(string tag, out Version version)
        {
            version = null;
            Match match = SemanticTag.Match(tag ?? string.Empty);
            if (!match.Success) return false;
            // A malformed release must not abort all update checks.
            return Version.TryParse(match.Groups["major"].Value + "." + match.Groups["minor"].Value + "."
                + (match.Groups["patch"].Success ? match.Groups["patch"].Value : "0") + "."
                + (match.Groups["revision"].Success ? match.Groups["revision"].Value : "0"), out version);
        }

        internal static string FindExpectedHash(string checksums, string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            string expected = null;
            foreach (string line in (checksums ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                Match match = Regex.Match(line.Trim(), @"^(?<hash>[a-fA-F0-9]{64})[ \t]+\*?(?<name>.+)$", RegexOptions.CultureInvariant);
                if (!match.Success || !string.Equals(match.Groups["name"].Value, fileName, StringComparison.Ordinal)) continue;
                string hash = match.Groups["hash"].Value;
                // Ambiguous checksums are not a safe basis for installing executable code.
                if (expected != null && !string.Equals(expected, hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The update has conflicting SHA-256 checksums.");
                expected = hash;
            }
            return expected;
        }
    }
}
