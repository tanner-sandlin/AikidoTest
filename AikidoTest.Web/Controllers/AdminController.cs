using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace AikidoTest.Web.Controllers
{
    public class AdminController : Controller
    {
        [HttpPost]
        // CWE-611: XXE — XmlDocument is loaded with default settings, which on this
        // target framework leaves external entity/DTD resolution enabled, so a
        // crafted <!DOCTYPE> can read local files or trigger SSRF.
        public ActionResult ImportConfig(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var value = doc.SelectSingleNode("//value")?.InnerText;
            return Content("Imported: " + value);
        }

        // CWE-502: Insecure Deserialization — an attacker-controlled cookie is
        // fed straight into BinaryFormatter, which can be abused to achieve RCE
        // via known gadget chains.
        public ActionResult RestoreSession()
        {
            var cookie = Request.Cookies["sessionBackup"];
            if (cookie == null)
            {
                return Content("No session backup cookie present.");
            }

            var bytes = Convert.FromBase64String(cookie.Value);
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(bytes))
            {
                var restored = formatter.Deserialize(stream);
                return Content("Restored: " + restored);
            }
        }

        // Fixed CWE-78: no shell is invoked (cmd.exe /c is gone, so shell
        // metacharacters like & | ; ` $() can no longer be interpreted), and
        // host is validated against a strict hostname/IPv4 allow-list before
        // it is ever used, so only a well-formed host can reach Process.Start.
        private static readonly Regex ValidHostPattern =
            new Regex(@"^[a-zA-Z0-9](?:[a-zA-Z0-9\-\.]{0,252})?[a-zA-Z0-9]$|^[a-zA-Z0-9]$", RegexOptions.Compiled);

        public ActionResult Ping(string host)
        {
            if (string.IsNullOrWhiteSpace(host) || !ValidHostPattern.IsMatch(host))
            {
                return new HttpStatusCodeResult(400, "Invalid host.");
            }

            var systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(systemDirectory, "PING.EXE"),
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            psi.ArgumentList.Add("-n");
            psi.ArgumentList.Add("1");
            psi.ArgumentList.Add(host);

            using (var process = Process.Start(psi))
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return Content("<pre>" + HttpUtility.HtmlEncode(output) + "</pre>");
            }
        }
    }
}
