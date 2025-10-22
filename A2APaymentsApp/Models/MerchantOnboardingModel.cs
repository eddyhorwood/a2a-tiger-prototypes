using System.ComponentModel.DataAnnotations;

namespace A2APaymentsApp.Models
{
    public class MerchantOnboardingModel
    {
        [Required]
        [Display(Name = "Bank Account")]
        public string BankAccountId { get; set; }

        [Required]
        [Display(Name = "Branding Theme")]
        public string BrandingThemeId { get; set; }

        [Required]
        [Display(Name = "Chart of Account Code")]
        public string ChartOfAccountCode { get; set; }
    }
}
