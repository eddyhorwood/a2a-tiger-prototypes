using System.Text.Json.Serialization;

namespace A2APaymentsApp.Clients
{
    public class CreatePaymentRequest
    {
        public PayeeDetails Payee { get; set; }

        /// <summary>
        /// Example: 12.34
        /// </summary>
        public decimal Amount { get; set; }
        
        /// <summary>
        /// Example: https://myapp.com/payments/complete
        /// </summary>
        [JsonPropertyName("redirect_uri")]
        public string RedirectUri { get; set; }
    }

    public class PayeeDetails
    {
        /// <summary>
        /// Example: John Smith
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Example: 12-3456-7890123-00
        /// </summary>
        [JsonPropertyName("account_number")]
        public string AccountNumber { get; set; }
        public string Particulars { get; set; }
        public string Code { get; set; }
        public string Reference { get; set; }
    }
}