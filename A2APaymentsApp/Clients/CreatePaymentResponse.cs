using System.Text.Json.Serialization;

namespace A2APaymentsApp.Clients
{
    public class CreatePaymentResponse
    {
        /// <summary>
        /// Example: one_off_payment_c01234567890123456789012345
        /// </summary>
        [JsonPropertyName("_payment")]
        public string Payment { get; set; }

        /// <summary>
        /// Example: https://payments.akahu.nz/?payment=one_off_payment_c01234567890123456789012345
        /// </summary>
        [JsonPropertyName("authorisation_url")]
        public string AuthorisationUrl { get; set; }
    }
}