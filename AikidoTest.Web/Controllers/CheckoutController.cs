using System.Data.SqlClient;
using System.Web.Mvc;
using AikidoTest.Web.Models;
using AikidoTest.Web.Utils;

namespace AikidoTest.Web.Controllers
{
    public class CheckoutController : Controller
    {
        private const string ConnectionString =
            "Data Source=SQL01;Initial Catalog=AikidoTestDb;User ID=sa;Password=P@ssw0rd123!;";

        [HttpGet]
        public ActionResult Index()
        {
            return View(new CreditCardModel());
        }

        [HttpPost]
        public ActionResult Index(CreditCardModel card)
        {
            // PCI-DSS Req. 3.4 / 3.2: full PAN and CVV logged in plaintext.
            // The CVV in particular must never be persisted anywhere, logs included.
            AppLogger.Log($"Processing payment: Name={card.CardHolderName}, " +
                          $"PAN={card.CardNumber}, Exp={card.ExpiryMonth}/{card.ExpiryYear}, " +
                          $"CVV={card.Cvv}, Amount={card.Amount}");

            // CWE-327: weak DES encryption applied to the card number before storage.
            var encryptedPan = CryptoHelper.EncryptCardNumber(card.CardNumber ?? string.Empty);

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                // CWE-89: SQL Injection via string concatenation, plus PCI-DSS Req. 3.2
                // violation — the raw CVV is stored in the Orders table (never allowed,
                // even encrypted) alongside a plaintext PAN column for good measure.
                var query = "INSERT INTO Orders (CardHolderName, CardNumber, CardNumberEncrypted, Cvv, Amount) " +
                            "VALUES ('" + card.CardHolderName + "', '" + card.CardNumber + "', '" +
                            encryptedPan + "', '" + card.Cvv + "', " + card.Amount + ")";

                var command = new SqlCommand(query, connection);
                command.ExecuteNonQuery();
            }

            return RedirectToAction("Confirmation");
        }

        public ActionResult Confirmation()
        {
            return View();
        }
    }
}
