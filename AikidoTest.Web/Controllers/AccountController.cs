using System;
using System.Data.SqlClient;
using System.Web.Mvc;
using AikidoTest.Web.Models;
using AikidoTest.Web.Utils;

namespace AikidoTest.Web.Controllers
{
    public class AccountController : Controller
    {
        private const string ConnectionString =
            "Data Source=SQL01;Initial Catalog=AikidoTestDb;User ID=sa;Password=P@ssw0rd123!;";

        [HttpGet]
        public ActionResult Login()
        {
            return View(new LoginModel());
        }

        [HttpPost]
        // CWE-352: no [ValidateAntiForgeryToken] on a state-changing POST action
        public ActionResult Login(LoginModel model)
        {
            // CWE-532: logging raw credentials (username + password) to disk
            AppLogger.Log($"Login attempt for user='{model.Username}' password='{model.Password}'");

            var hashedPassword = CryptoHelper.HashPassword(model.Password ?? string.Empty);

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                // CWE-89: SQL Injection — user-controlled values concatenated directly
                // into the command text instead of using parameters.
                var query = "SELECT UserId, Username, IsAdmin FROM Users WHERE Username = '"
                            + model.Username + "' AND PasswordHash = '" + hashedPassword + "'";

                var command = new SqlCommand(query, connection);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var isAdmin = reader["IsAdmin"] != DBNull.Value && (bool)reader["IsAdmin"];

                        // CWE-614 / CWE-1004: auth cookie issued without Secure/HttpOnly,
                        // consistent with the httpCookies config in Web.config.
                        Response.Cookies.Add(new System.Web.HttpCookie("AikidoAuth", model.Username)
                        {
                            HttpOnly = false,
                            Secure = false,
                            Expires = DateTime.UtcNow.AddDays(30)
                        });

                        if (isAdmin)
                        {
                            Response.Cookies.Add(new System.Web.HttpCookie("IsAdmin", "true"));
                        }

                        return RedirectToAction("Index", "Home");
                    }
                }
            }

            ViewBag.Error = "Invalid username or password.";
            return View(model);
        }
    }
}
