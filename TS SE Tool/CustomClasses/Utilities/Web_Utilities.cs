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
using System.Diagnostics;

namespace TS_SE_Tool.Utilities
{
    public sealed class Web_Utilities
    {
        internal static readonly Web_Utilities External = new Web_Utilities();

        internal const string RepositoryUrl = "https://github.com/omnizs38/TS-SE-Tool";
        internal const string ReleasesUrl = RepositoryUrl + "/releases";
        internal const string LatestReleaseUrl = ReleasesUrl + "/latest";
        internal const string IssuesUrl = RepositoryUrl + "/issues";

        // Compatibility names used by existing WinForms event handlers.
        internal readonly string linkYoutubeTutorial = RepositoryUrl + "#features";
        internal readonly string linkHelpDeveloper = IssuesUrl;
        internal readonly string linkMailDeveloper = IssuesUrl;
        internal readonly string linkSCSforum = RepositoryUrl;
        internal readonly string linkTMPforum = RepositoryUrl;
        internal readonly string linkGithub = RepositoryUrl;
        internal readonly string linkGithubReleases = ReleasesUrl;
        internal readonly string linkGithubReleasesLatest = LatestReleaseUrl;

        private Web_Utilities()
        {
        }

        internal bool TryOpenUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp))
            {
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = parsed.AbsoluteUri,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception exception)
            {
                IO_Utilities.ErrorLogWriter("Could not open URL: " + exception);
                return false;
            }
        }
    }
}
