namespace AikidoTest.Web.Models
{
    public class CreditCardModel
    {
        public string CardHolderName { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }

        // PCI-DSS Req. 3.2: CVV/CVV2 must never be stored after authorization.
        // Kept here and persisted anyway so the SAST/PCI scan has a concrete
        // "sensitive auth data retained" finding to detect.
        public string Cvv { get; set; }

        public decimal Amount { get; set; }
    }
}
