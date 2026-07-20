using System.Web.Mvc;
using System.Web.Routing;

namespace AikidoTest.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        protected void Application_Error()
        {
            // CWE-209: exception details (including stack trace) surfaced to the client
            // instead of being logged internally, on top of customErrors being disabled.
            var exception = Server.GetLastError();
            Response.Write("<pre>" + exception + "</pre>");
        }
    }
}
