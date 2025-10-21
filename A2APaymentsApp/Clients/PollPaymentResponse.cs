using System;
using System.Text.Json.Serialization;

namespace A2APaymentsApp.Clients
{
    public class PollPaymentResponse
    {
        /// <summary>
        /// Example: one_off_payment_c01234567890123456789012345
        /// </summary>
        public string _id { get; set; }

        /// <summary>
        /// Example: SUBMITTED
        /// </summary>
        public string Status { get; set; }

        [JsonPropertyName("status_reason")]
        public StatusReason StatusReason { get; set; }

        /// <summary>
        /// Example: 2025-10-20T20:24:11.663Z
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Example: 2025-10-20T20:24:11.663Z
        /// </summary>
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Example: 2025-10-20T20:24:11.663Z
        /// </summary>
        [JsonPropertyName("submitted_at")]
        public DateTime SubmittedAt { get; set; }

        /// <summary>
        /// Example: 2025-10-20T20:24:11.663Z
        /// </summary>
        [JsonPropertyName("terminal_at")]
        public DateTime TerminalAt { get; set; }

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

    public class StatusReason
    {
        /// <summary>
        /// Example: PENDING
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Example: The payment was submitted to the bank.
        /// </summary>
        public string Message { get; set; }
    }
}