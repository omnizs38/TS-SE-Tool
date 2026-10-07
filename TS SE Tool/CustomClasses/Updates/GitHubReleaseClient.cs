/* Copyright 2026 omnizs38 and contributors. Apache-2.0. */
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
        private static readonly HttpClient Client = CreateClient();
        internal static Version CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);

        internal static async Task<GitHubReleaseInfo> GetLatestStableAsync(CancellationToken token)
        {
            using (HttpResponseMessage response = await Client.GetAsync(ReleasesApi, token).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return ParseReleasesJson(await response.Content.ReadAsStringAsync(token).ConfigureAwait(false));
            }
        }

        internal static GitHubReleaseInfo ParseReleasesJson(string json)
        {
            using (JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 }))
            {
                if (document.RootElement.ValueKind != JsonValueKind.Array) return null;
                GitHubReleaseInfo newest = null;
                foreach (JsonElement release in document.RootElement.EnumerateArray())
                {
                    if (release.ValueKind != JsonValueKind.Object || Flag(release, "draft") || Flag(release, "prerelease")) continue;
                    string tag = Text(release, "tag_name");
                    if (!TryParseSemanticTag(tag, out Version version)) continue;
                    if (newest != null && version.CompareTo(newest.Version) <= 0) continue;
                    GitHubReleaseInfo info = new GitHubReleaseInfo { TagName = tag, Version = version, Name = Text(release, "name"), Url = Text(release, "html_url"), Notes = Text(release, "body") };
                    string installer = "TS-SE-Tool-" + tag.Substring(1) + "-setup.exe";
                    if (release.TryGetProperty("assets", out JsonElement assets) && assets.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement asset in assets.EnumerateArray())
                        {
                            if (asset.ValueKind != JsonValueKind.Object) continue;
                            string name = Text(asset, "name"); string url = Text(asset, "browser_download_url");
                            if (!IsTrustedAsset(url, tag, name)) continue;
                            if (name == installer) { info.InstallerName = name; info.InstallerUrl = url; }
                            else if (name == "SHA256SUMS.txt") info.ChecksumsUrl = url;
                        }
                    }
                    newest = info;
                }
                return newest;
            }
        }

        private static bool IsTrustedAsset(string value, string tag, string name)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri) || uri.Scheme != Uri.UriSchemeHttps || uri.Host != "github.com" || !uri.IsDefaultPort || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0) return false;
            return uri.AbsolutePath == "/omnizs38/TS-SE-Tool/releases/download/" + Uri.EscapeDataString(tag) + "/" + Uri.EscapeDataString(name);
        }

        internal static async Task<byte[]> DownloadAsync(string url, CancellationToken token)
        {
            using (MemoryStream buffer = new MemoryStream())
            {
                await DownloadToStreamAsync(url, buffer, 1024 * 1024, token).ConfigureAwait(false);
                return buffer.ToArray();
            }
        }

        internal static async Task DownloadToFileAsync(string url, string path, CancellationToken token)
        {
            using (FileStream file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, true))
            {
                await DownloadToStreamAsync(url, file, 256L * 1024 * 1024, token).ConfigureAwait(false);
                await file.FlushAsync(token).ConfigureAwait(false);
                file.Flush(true);
            }
        }

        private static async Task DownloadToStreamAsync(string url, Stream target, long limit, CancellationToken token)
        {
            using (HttpResponseMessage response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentLength > limit) throw new InvalidDataException("The update exceeds the allowed download size.");
                using (Stream source = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false))
                {
                    byte[] bytes = new byte[65536]; long total = 0; int read;
                    while ((read = await source.ReadAsync(bytes, token).ConfigureAwait(false)) != 0)
                    {
                        total += read;
                        if (total > limit) throw new InvalidDataException("The update exceeds the allowed download size.");
                        await target.WriteAsync(bytes.AsMemory(0, read), token).ConfigureAwait(false);
                    }
                }
            }
        }

        internal static bool IsNewer(GitHubReleaseInfo release) => release?.Version != null && release.Version.CompareTo(CurrentVersion) > 0;
        internal static bool TryParseSemanticTag(string tag, out Version version) => ReleaseValidation.TryParseSemanticTag(tag, out version);
        private static string Text(JsonElement item, string key) => item.TryGetProperty(key, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : string.Empty;
        private static bool Flag(JsonElement item, string key) => item.TryGetProperty(key, out JsonElement value) && value.ValueKind == JsonValueKind.True;
        private static HttpClient CreateClient()
        {
            HttpClient client = new HttpClient { Timeout = TimeSpan.FromMinutes(20), MaxResponseContentBufferSize = 8 * 1024 * 1024 };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TS-SE-Tool/" + CurrentVersion + " (+https://github.com/omnizs38/TS-SE-Tool)");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }
    }
}
