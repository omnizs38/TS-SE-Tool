/*
   Added during the 2026 save-format investigation (ATS/ETS2 savefile version 97).
   Preserves original attributes while applying values edited by TS SE Tool.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TS_SE_Tool.Utilities;

namespace TS_SE_Tool.Save.Items
{
    internal static class OriginalBlockMerge
    {
        private static readonly Regex BlockHeader = new Regex(@"^(?<tag>[A-Za-z_][A-Za-z_0-9]*)\s*:\s*(?<name>\S+)\s*\{\s*$", RegexOptions.Compiled);
        private static readonly Regex ArrayIndex = new Regex(@"\[\d+\]$", RegexOptions.Compiled);

        private static string TagOf(string line)
        {
            int colon = line.IndexOf(':');
            if (colon <= 0) return null;
            string tag = line.Substring(0, colon).Trim();
            return tag.Length == 0 ? null : tag;
        }

        private static string BaseTagOf(string tag) { return ArrayIndex.Replace(tag, ""); }

        internal static string Apply(string generatedText, Dictionary<string, List<string>> originalBodies)
        {
            if (originalBodies == null || originalBodies.Count == 0 || string.IsNullOrEmpty(generatedText)) return generatedText;
            string[] lines = generatedText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            StringBuilder output = new StringBuilder();
            HashSet<string> droppedTags = new HashSet<string>(StringComparer.Ordinal);
            int mergedBlocks = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                Match header = BlockHeader.Match(lines[i].Trim());
                if (!header.Success)
                {
                    output.AppendLine(lines[i]);
                    continue;
                }

                List<string> generatedBody = new List<string>();
                int j = i + 1;
                while (j < lines.Length && lines[j].Trim() != "}")
                {
                    generatedBody.Add(lines[j]);
                    j++;
                }
                bool closed = j < lines.Length;
                string name = header.Groups["name"].Value;
                output.AppendLine(lines[i]);

                List<string> originalBody;
                if (originalBodies.TryGetValue(name, out originalBody))
                {
                    mergedBlocks++;
                    foreach (string line in MergeBody(header.Groups["tag"].Value, originalBody, generatedBody, droppedTags)) output.AppendLine(line);
                }
                else
                {
                    foreach (string line in generatedBody) output.AppendLine(line);
                }
                if (closed) output.AppendLine(lines[j]);
                i = closed ? j : lines.Length;
            }

            if (droppedTags.Count > 0)
            {
                IO_Utilities.LogWriter("Save write | " + droppedTags.Count + " compatibility-only attributes were omitted because the loaded save does not contain them:" + Environment.NewLine + string.Join(Environment.NewLine, droppedTags.OrderBy(x => x)));
            }
            IO_Utilities.LogWriter("Save write | original-content merge applied to " + mergedBlocks + " blocks");
            string result = output.ToString();
            if (result.EndsWith(Environment.NewLine)) result = result.Substring(0, result.Length - Environment.NewLine.Length);
            return result;
        }

        private static List<string> MergeBody(string blockTag, List<string> originalBody, List<string> generatedBody, ISet<string> droppedTags)
        {
            Dictionary<string, string> generated = new Dictionary<string, string>();
            List<string> generatedOrder = new List<string>();
            foreach (string line in generatedBody)
            {
                string tag = TagOf(line);
                if (tag == null || generated.ContainsKey(tag)) continue;
                generated.Add(tag, line);
                generatedOrder.Add(tag);
            }

            HashSet<string> originalBaseTags = new HashSet<string>();
            foreach (string line in originalBody)
            {
                string tag = TagOf(line);
                if (tag != null) originalBaseTags.Add(BaseTagOf(tag));
            }

            List<string> merged = new List<string>();
            HashSet<string> consumed = new HashSet<string>();
            foreach (string line in originalBody)
            {
                string tag = TagOf(line);
                if (tag != null && generated.ContainsKey(tag))
                {
                    merged.Add(generated[tag]);
                    consumed.Add(tag);
                }
                else if (tag != null || line.Trim().Length > 0)
                {
                    merged.Add(line);
                }
            }

            foreach (string tag in generatedOrder)
            {
                if (consumed.Contains(tag)) continue;
                if (originalBaseTags.Contains(BaseTagOf(tag))) merged.Add(generated[tag]);
                else droppedTags.Add(blockTag + " | " + tag);
            }
            return merged;
        }
    }
}
