/*
   Original work copyright 2016-2022 LIPtoH and contributors.
   Maintenance modifications copyright 2026 omnizs38 and contributors.
   Licensed under the Apache License, Version 2.0.
*/
using System;
using System.Collections.Generic;
using System.Linq;
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
        internal string InstallerName { get; set; }
        internal string InstallerUrl { get; set; }
        internal string ChecksumsUrl { get; set; }
    }

    internal static class GitHubReleaseClient
    {
        private const string ReleasesApi = "https://api.github.com/repos/omnizs38/TS-SE-Tool/releases?per_page=20";
        private static readonly Regex SemanticTag = new Regex(@"^v(?<major>\d+)\.(?<minor>\d+)(?:\.(?<patch>\d+))?(?:\.(?<revision>\d+))?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly HttpClient Client = CreateClient();

        internal static Version CurrentVersion { get { return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0); } }

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
                        newest = new GitHubReleaseInfo { TagName = tag, Name = GetString(release, "name"), Url = GetString(release, "html_url"), Notes = GetString(release, "body"), Version = version };
                        ReadAssets(release, newest);
                    }
                }
                return newest;
            }
        }

        internal static async Task<byte[]> DownloadAsync(string url, CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await Client.GetAsync(url, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }

        internal static bool IsNewer(GitHubReleaseInfo release) { return release != null && release.Version.CompareTo(CurrentVersion) > 0; }

        private static void ReadAssets(Dictionary<string, object> release, GitHubReleaseInfo info)
        {
            object raw;
            object[] assets = release.TryGetValue("assets", out raw) ? raw as object[] : null;
            if (assets == null) return;
            foreach (Dictionary<string, object> asset in assets.OfType<Dictionary<string, object>>())
            {
                string name = GetString(asset, "name");
                string url = GetString(asset, "browser_download_url");
                if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && name.IndexOf("setup", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    info.InstallerName = name;
                    info.InstallerUrl = url;
                }
                else if (string.Equals(name, "SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase)) info.ChecksumsUrl = url;
            }
        }

        private static HttpClient CreateClient()
        {
            HttpClient client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TS-SE-Tool/1.62 (+https://github.com/omnizs38/TS-SE-Tool)");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        private static bool TryParseSemanticTag(string tag, out Version version)
        {
            version = null;
            Match match = SemanticTag.Match(tag ?? string.Empty);
            if (!match.Success) return false;
            version = new Version(int.Parse(match.Groups["major"].Value), int.Parse(match.Groups["minor"].Value), match.Groups["patch"].Success ? int.Parse(match.Groups["patch"].Value) : 0, match.Groups["revision"].Success ? int.Parse(match.Groups["revision"].Value) : 0);
            return true;
        }

        private static string GetString(Dictionary<string, object> source, string key) { object value; return source.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : string.Empty; }
        private static bool GetBoolean(Dictionary<string, object> source, string key) { object value; return source.TryGetValue(key, out value) && value is bool && (bool)value; }
    }
}
