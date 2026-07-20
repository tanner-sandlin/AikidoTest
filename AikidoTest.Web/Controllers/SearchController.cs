using System.Data.SqlClient;
using System.Web.Mvc;

namespace AikidoTest.Web.Controllers
{
    public class SearchController : Controller
    {
        private const string ConnectionString =
            "Data Source=SQL01;Initial Catalog=AikidoTestDb;User ID=sa;Password=P@ssw0rd123!;";

        public ActionResult Index(string q)
        {
            // CWE-79: reflected XSS — the raw, unencoded query string is echoed back
            // into the response via ViewBag and rendered with Html.Raw in the view.
            ViewBag.RawQuery = q;
            ViewBag.Results = RunSearch(q);
            return View();
        }

        private System.Collections.Generic.List<string> RunSearch(string term)
        {
            var results = new System.Collections.Generic.List<string>();

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                // CWE-89: SQL Injection via string concatenation in a LIKE clause.
                var query = "SELECT ProductName FROM Products WHERE ProductName LIKE '%" + term + "%'";
                var command = new SqlCommand(query, connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(reader["ProductName"].ToString());
                    }
                }
            }

            return results;
        }
    }
}
