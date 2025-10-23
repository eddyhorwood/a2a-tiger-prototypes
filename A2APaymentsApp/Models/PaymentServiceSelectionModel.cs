using System.ComponentModel.DataAnnotations;

namespace A2APaymentsApp.Models
{
    public class PaymentServiceSelectionModel
    {
        [Required]
        [Display(Name = "Payment Service")]
        public string PaymentServiceId { get; set; }
    }
}
