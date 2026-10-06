// Minimal UI/logging adapters; the code under test is linked directly from production.
namespace TS_SE_Tool.Utilities
{
    internal static class IO_Utilities
    {
        internal static void LogWriter(string message) { }
        internal static void ErrorLogWriter(string message) { }
    }
}
namespace TS_SE_Tool
{
    internal enum SMStatus { Error }
    internal static class UpdateStatusBarMessage
    {
        internal static void ShowStatusMessage(SMStatus status, string key, string path) { }
    }
}
