/*
   Original work copyright 2016-2022 LIPtoH and contributors.
   Maintenance modifications copyright 2026 omnizs38 and contributors.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
*/
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace TS_SE_Tool.Updates
{
    internal sealed class GitHubReleaseInfo
    {
        internal string TagName { get; set; }
        internal string Name { get; set; }
        internal string Url { get; set; }
        internal string Notes { get; set; }
        internal Version Version { get; set; }
    }

    internal static class GitHubReleaseClient
    {
        private const string ReleasesApi = "https://api.github.com/repos/omnizs38/TS-SE-Tool/releases?per_page=20";
        private static readonly Regex SemanticTag = new Regex(
            @"^v(?<major>\d+)\.(?<minor>\d+)(?:\.(?<patch>\d+))?(?:\.(?<revision>\d+))?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly HttpClient Client = CreateClient();

        internal static Version CurrentVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0); }
        }

        internal static async Task<GitHubReleaseInfo> GetLatestStableAsync(CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await Client.GetAsync(ReleasesApi, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                object[] releases = new JavaScriptSerializer().DeserializeObject(json) as object[];
                if (releases == null) return null;

                GitHubReleaseInfo newest = null;
                foreach (object item in releases)
                {
                    Dictionary<string, object> release = item as Dictionary<string, object>;
                    if (release == null || GetBoolean(release, "draft") || GetBoolean(release, "prerelease")) continue;

                    string tag = GetString(release, "tag_name");
                    Version version;
                    if (!TryParseSemanticTag(tag, out version)) continue;
                    if (newest == null || version.CompareTo(newest.Version) > 0)
                    {
                        newest = new GitHubReleaseInfo
                        {
                            TagName = tag,
                            Name = GetString(release, "name"),
                            Url = GetString(release, "html_url"),
                            Notes = GetString(release, "body"),
                            Version = version
                        };
                    }
                }
                return newest;
            }
        }

        internal static bool IsNewer(GitHubReleaseInfo release)
        {
            return release != null && release.Version.CompareTo(CurrentVersion) > 0;
        }

        private static HttpClient CreateClient()
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(12);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TS-SE-Tool/1.61.1 (+https://github.com/omnizs38/TS-SE-Tool)");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        private static bool TryParseSemanticTag(string tag, out Version version)
        {
            version = null;
            Match match = SemanticTag.Match(tag ?? string.Empty);
            if (!match.Success) return false;
            int major = int.Parse(match.Groups["major"].Value);
            int minor = int.Parse(match.Groups["minor"].Value);
            int patch = match.Groups["patch"].Success ? int.Parse(match.Groups["patch"].Value) : 0;
            int revision = match.Groups["revision"].Success ? int.Parse(match.Groups["revision"].Value) : 0;
            version = new Version(major, minor, patch, revision);
            return true;
        }

        private static string GetString(Dictionary<string, object> source, string key)
        {
            object value;
            return source.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : string.Empty;
        }

        private static bool GetBoolean(Dictionary<string, object> source, string key)
        {
            object value;
            return source.TryGetValue(key, out value) && value is bool && (bool)value;
        }
    }
}
