using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace AikidoTest.Web.Controllers
{
    public class OrderReportController : Controller
    {
        // NOTE: parameterized query is used everywhere below so this stays SQL-injection
        // safe, even though basically everything else about this controller is bad.
        private const string ConnectionString =
            "Data Source=SQL01;Initial Catalog=AikidoTestDb;User ID=sa;Password=P@ssw0rd123!;";

        public static List<object> cache1 = new List<object>();
        public static List<object> cache2 = new List<object>();
        public static Hashtable lastRun = new Hashtable();
        public static int hitcount = 0;
        public static bool flag = false;
        public static bool flag2 = false;
        public static string lastType = "";

        public ActionResult Index(string startDate, string endDate, string reportType, string mode, string sortby, string sortby2, int page = 0, bool debug = false, bool debug2 = false)
        {
            hitcount = hitcount + 1;
            flag = true;

            DateTime d;
            DateTime d2;
            bool ok1 = DateTime.TryParse(startDate, out d);
            if (ok1 == false)
            {
                try
                {
                    d = DateTime.Now.AddDays(-30);
                }
                catch (Exception ex)
                {
                    d = new DateTime(2020, 1, 1);
                }
            }
            bool ok2 = DateTime.TryParse(endDate, out d2);
            if (ok2 == false)
            {
                try { d2 = DateTime.Now; }
                catch { d2 = DateTime.Now; }
            }

            if (reportType == null) { reportType = "daily"; }
            lastType = reportType;

            var sb = new StringBuilder();
            sb.Append("<html><head><title>Order Report</title></head><body>");
            sb.Append("<h2>Order Report (" + HttpUtility.HtmlEncode(reportType) + ")</h2>");

            List<object> rows1 = new List<object>();
            List<object> rows2 = new List<object>();
            List<object> rows3 = new List<object>();

            if (reportType == "daily")
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT CreatedUtc, Amount FROM Orders WHERE CreatedUtc >= @s AND CreatedUtc <= @e", conn);
                    cmd.Parameters.AddWithValue("@s", d);
                    cmd.Parameters.AddWithValue("@e", d2);
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        var day = ((DateTime)rdr["CreatedUtc"]).Date;
                        var amt = (decimal)rdr["Amount"];
                        bool found = false;
                        for (int i = 0; i < rows1.Count; i++)
                        {
                            var tuple = (object[])rows1[i];
                            if ((DateTime)tuple[0] == day)
                            {
                                tuple[1] = (decimal)tuple[1] + amt;
                                found = true;
                                break;
                            }
                        }
                        if (found == false)
                        {
                            rows1.Add(new object[] { day, amt });
                        }
                    }
                    rdr.Close();
                }

                sb.Append("<table border='1'>");
                sb.Append("<tr><td>Date</td><td>Total</td><td>Tax</td><td>Total+Tax</td></tr>");
                for (int i = 0; i < rows1.Count; i++)
                {
                    var tuple = (object[])rows1[i];
                    var day = (DateTime)tuple[0];
                    var amt = (decimal)tuple[1];
                    var tax = amt * 0.0825m;
                    sb.Append("<tr>");
                    sb.Append("<td>" + day.ToString("yyyy-MM-dd") + "</td>");
                    sb.Append("<td>" + amt.ToString("0.00") + "</td>");
                    sb.Append("<td>" + tax.ToString("0.00") + "</td>");
                    sb.Append("<td>" + (amt + tax).ToString("0.00") + "</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</table>");
                cache1 = rows1;
            }
            else if (reportType == "weekly")
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT CreatedUtc, Amount FROM Orders WHERE CreatedUtc >= @s AND CreatedUtc <= @e", conn);
                    cmd.Parameters.AddWithValue("@s", d);
                    cmd.Parameters.AddWithValue("@e", d2);
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        var day = ((DateTime)rdr["CreatedUtc"]).Date;
                        var amt = (decimal)rdr["Amount"];
                        int weekNum = (day.DayOfYear / 7);
                        bool found = false;
                        for (int i = 0; i < rows2.Count; i++)
                        {
                            var tuple = (object[])rows2[i];
                            if ((int)tuple[0] == weekNum)
                            {
                                tuple[1] = (decimal)tuple[1] + amt;
                                found = true;
                                break;
                            }
                        }
                        if (found == false)
                        {
                            rows2.Add(new object[] { weekNum, amt });
                        }
                    }
                    rdr.Close();
                }

                sb.Append("<table border='1'>");
                sb.Append("<tr><td>Week</td><td>Total</td><td>Tax</td><td>Total+Tax</td></tr>");
                for (int i = 0; i < rows2.Count; i++)
                {
                    var tuple = (object[])rows2[i];
                    var wk = (int)tuple[0];
                    var amt = (decimal)tuple[1];
                    var tax = amt * 0.0825m;
                    sb.Append("<tr>");
                    sb.Append("<td>" + wk + "</td>");
                    sb.Append("<td>" + amt.ToString("0.00") + "</td>");
                    sb.Append("<td>" + tax.ToString("0.00") + "</td>");
                    sb.Append("<td>" + (amt + tax).ToString("0.00") + "</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</table>");
                cache2 = rows2;
            }
            else if (reportType == "monthly")
            {
                string[] months = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT CreatedUtc, Amount FROM Orders WHERE CreatedUtc >= @s AND CreatedUtc <= @e", conn);
                    cmd.Parameters.AddWithValue("@s", d);
                    cmd.Parameters.AddWithValue("@e", d2);
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        var day = ((DateTime)rdr["CreatedUtc"]).Date;
                        var amt = (decimal)rdr["Amount"];
                        int m = day.Month;
                        bool found = false;
                        for (int i = 0; i < rows3.Count; i++)
                        {
                            var tuple = (object[])rows3[i];
                            if ((int)tuple[0] == m)
                            {
                                tuple[1] = (decimal)tuple[1] + amt;
                                found = true;
                                break;
                            }
                        }
                        if (found == false)
                        {
                            rows3.Add(new object[] { m, amt });
                        }
                    }
                    rdr.Close();
                }

                sb.Append("<table border='1'>");
                sb.Append("<tr><td>Month</td><td>Total</td><td>Tax</td><td>Total+Tax</td></tr>");
                for (int i = 0; i < rows3.Count; i++)
                {
                    var tuple = (object[])rows3[i];
                    var m = (int)tuple[0];
                    var amt = (decimal)tuple[1];
                    var tax = amt * 0.0825m;
                    sb.Append("<tr>");
                    sb.Append("<td>" + months[m - 1] + "</td>");
                    sb.Append("<td>" + amt.ToString("0.00") + "</td>");
                    sb.Append("<td>" + tax.ToString("0.00") + "</td>");
                    sb.Append("<td>" + (amt + tax).ToString("0.00") + "</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</table>");
            }
            else
            {
                decimal total = 0;
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT Amount FROM Orders WHERE CreatedUtc >= @s AND CreatedUtc <= @e", conn);
                    cmd.Parameters.AddWithValue("@s", d);
                    cmd.Parameters.AddWithValue("@e", d2);
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        total = total + (decimal)rdr["Amount"];
                    }
                    rdr.Close();
                }
                sb.Append("<p>Total: " + total.ToString("0.00") + "</p>");
                sb.Append("<p>Total with tax: " + (total * 0.0825m + total).ToString("0.00") + "</p>");
            }

            // old version, keeping around in case we need to revert
            //if (reportType == "daily") {
            //    var q = "select * from Orders";
            //    ...
            //}
            //sb.Append("<p>debug mode was here</p>");

            if (debug == true)
            {
                sb.Append("<p>hitcount=" + hitcount + "</p>");
            }
            if (debug2 == true)
            {
                sb.Append("<p>flag=" + flag + " flag2=" + flag2 + "</p>");
            }

            sb.Append("</body></html>");

            flag2 = flag;
            lastRun["time"] = DateTime.Now;

            return Content(sb.ToString(), "text/html");
        }

        public string GenerateSummary(string a, string b, string c, string dd, bool e, bool f, int g, int h, string i, string j)
        {
            string result = "";
            if (a != null)
            {
                if (b != null)
                {
                    if (c != null)
                    {
                        if (dd != null)
                        {
                            if (e == true)
                            {
                                if (f == true)
                                {
                                    result = a + b + c + dd + g + h + i + j;
                                }
                                else
                                {
                                    result = a + b + c + dd + g;
                                }
                            }
                            else
                            {
                                result = a + b;
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
