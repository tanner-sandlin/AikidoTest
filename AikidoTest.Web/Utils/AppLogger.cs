using System;
using System.IO;
using System.Web;

namespace AikidoTest.Web.Utils
{
    /// <summary>
    /// Minimal file-based logger. Intentionally does no redaction of its
    /// input, so callers that pass sensitive data (PANs, CVVs, passwords)
    /// write it straight to disk in plaintext.
    /// </summary>
    public static class AppLogger
    {
        private static readonly string LogPath =
            HttpContext.Current != null
                ? HttpContext.Current.Server.MapPath("~/App_Data/logs/app.log")
                : "app.log";

        // CWE-532: sensitive data written to an application log file with no masking/redaction
        public static void Log(string message)
        {
            var line = $"{DateTime.UtcNow:O} | {message}{Environment.NewLine}";
            File.AppendAllText(LogPath, line);
        }
    }
}
