using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace A2APaymentsApp.Models
{
    // Model of what values SignUpWithXeroUser object will hold and record into
    // These values are used to create a new user in the database as a new user signs up with Xero
    public class Organisation
    {
        [Key]
        [Required]
        public string TenantId { get; set; }
        
        // Unique tenant short code to be used when building Xero App Store Subscription URL
        [Required]
        public string TenantShortCode { get; set; }
        
        [Required]
        public string BankAccountNumber { get; set; }
        
        // the chart of accounts to record a payment to an invoice
        [Required]
        public string AccountIdForPayment { get; set; }
    }
}