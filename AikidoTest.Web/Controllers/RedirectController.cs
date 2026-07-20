using System.Web.Mvc;

namespace AikidoTest.Web.Controllers
{
    public class RedirectController : Controller
    {
        // CWE-601: Open Redirect — the destination URL comes straight from the
        // query string with no allow-list / same-origin check before redirecting.
        public ActionResult Go(string url)
        {
            return Redirect(url);
        }
    }
}
