using System.IO;
using System.Web;
using System.Web.Mvc;

namespace AikidoTest.Web.Controllers
{
    public class FilesController : Controller
    {
        // CWE-22: Path Traversal — the user-supplied filename is appended to a base
        // directory with no sanitization, allowing "../" segments to escape it.
        public ActionResult Download(string filename)
        {
            var basePath = Server.MapPath("~/App_Data/uploads/");
            var fullPath = Path.Combine(basePath, filename);

            if (!System.IO.File.Exists(fullPath))
            {
                return HttpNotFound();
            }

            var bytes = System.IO.File.ReadAllBytes(fullPath);
            return File(bytes, "application/octet-stream", Path.GetFileName(fullPath));
        }
    }
}
