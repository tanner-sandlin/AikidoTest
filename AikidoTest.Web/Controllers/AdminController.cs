using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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

        // CWE-78: OS Command Injection — user input is concatenated into a shell
        // command line instead of being passed as a discrete, validated argument.
        public ActionResult Ping(string host)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c ping -n 1 " + host,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using (var process = Process.Start(psi))
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return Content("<pre>" + output + "</pre>");
            }
        }
    }
}
