using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Mvc;
using AikidoTest.Web.Models;

namespace AikidoTest.Web.Controllers
{
    public class ReviewsController : Controller
    {
        private const string ConnectionString =
            "Data Source=SQL01;Initial Catalog=AikidoTestDb;User ID=sa;Password=P@ssw0rd123!;";

        public ActionResult Index(int productId)
        {
            var reviews = new List<ReviewModel>();

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    "SELECT ReviewId, ProductId, Author, Body, AvatarUrl, AttachmentPath, CreatedUtc " +
                    "FROM Reviews WHERE ProductId = @productId ORDER BY CreatedUtc DESC", connection);
                command.Parameters.AddWithValue("@productId", productId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reviews.Add(new ReviewModel
                        {
                            ReviewId = (int)reader["ReviewId"],
                            ProductId = (int)reader["ProductId"],
                            Author = reader["Author"].ToString(),
                            Body = reader["Body"].ToString(),
                            AvatarUrl = reader["AvatarUrl"] as string,
                            AttachmentPath = reader["AttachmentPath"] as string,
                            CreatedUtc = (DateTime)reader["CreatedUtc"]
                        });
                    }
                }
            }

            ViewBag.ProductId = productId;
            return View(reviews);
        }

        [HttpGet]
        public ActionResult Create(int productId)
        {
            ViewBag.ProductId = productId;
            return View(new ReviewModel { ProductId = productId });
        }

        [HttpPost]
        // CWE-352: no [ValidateAntiForgeryToken] on a state-changing POST action, consistent
        // with the rest of the app.
        public ActionResult Create(ReviewModel model, HttpPostedFileBase attachment)
        {
            // CWE-918: Server-Side Request Forgery — the "avatar" URL is fetched
            // server-side with no validation of scheme/host, so an attacker can point
            // it at internal/metadata endpoints (e.g. http://169.254.169.254/...,
            // file://, or an internal-only admin URL) and have the server make the
            // request on their behalf.
            if (!string.IsNullOrWhiteSpace(model.AvatarUrl))
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        var avatarBytes = client.DownloadData(model.AvatarUrl);
                        var avatarDir = Server.MapPath("~/Uploads/avatars/");
                        Directory.CreateDirectory(avatarDir);
                        var avatarFile = Path.Combine(avatarDir, Guid.NewGuid() + ".img");
                        System.IO.File.WriteAllBytes(avatarFile, avatarBytes);
                    }
                }
                catch
                {
                    // best-effort avatar fetch; ignore failures
                }
            }

            string attachmentPath = null;
            if (attachment != null && attachment.ContentLength > 0)
            {
                // CWE-434: Unrestricted Upload of File with Dangerous Type — the
                // attacker-supplied filename (including its extension) is trusted
                // as-is and the file is written straight into a directory served
                // directly by IIS, with no extension/content-type allow-list and no
                // content inspection. Uploading a ".aspx"/".config" file here can
                // lead to remote code execution.
                var uploadDir = Server.MapPath("~/Uploads/attachments/");
                Directory.CreateDirectory(uploadDir);
                var savedPath = Path.Combine(uploadDir, Path.GetFileName(attachment.FileName));
                attachment.SaveAs(savedPath);
                attachmentPath = "/Uploads/attachments/" + Path.GetFileName(attachment.FileName);
            }

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    "INSERT INTO Reviews (ProductId, Author, Body, AvatarUrl, AttachmentPath, CreatedUtc) " +
                    "VALUES (@productId, @author, @body, @avatarUrl, @attachmentPath, @createdUtc)", connection);
                command.Parameters.AddWithValue("@productId", model.ProductId);
                command.Parameters.AddWithValue("@author", model.Author ?? string.Empty);
                command.Parameters.AddWithValue("@body", model.Body ?? string.Empty);
                command.Parameters.AddWithValue("@avatarUrl", (object)model.AvatarUrl ?? DBNull.Value);
                command.Parameters.AddWithValue("@attachmentPath", (object)attachmentPath ?? DBNull.Value);
                command.Parameters.AddWithValue("@createdUtc", DateTime.UtcNow);
                command.ExecuteNonQuery();
            }

            return RedirectToAction("Index", new { productId = model.ProductId });
        }

        [HttpPost]
        // CWE-862 / CWE-639: Broken Access Control (Insecure Direct Object Reference) —
        // any caller can delete any review by guessing/incrementing reviewId. There is
        // no check that the current user (from the "AikidoAuth" cookie) is the review's
        // author or an admin, and the action isn't even guarded by requiring a logged-in
        // user at all.
        public ActionResult Delete(int reviewId, int productId)
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var command = new SqlCommand("DELETE FROM Reviews WHERE ReviewId = @reviewId", connection);
                command.Parameters.AddWithValue("@reviewId", reviewId);
                command.ExecuteNonQuery();
            }

            return RedirectToAction("Index", new { productId });
        }
    }
}
