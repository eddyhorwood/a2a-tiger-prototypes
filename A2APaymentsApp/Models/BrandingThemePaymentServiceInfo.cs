namespace A2APaymentsApp.Models
{
    using System.Collections.Generic;

    public class SimplePaymentServiceInfo
    {
        public string PaymentServiceId { get; set; }
        public string PaymentServiceName { get; set; }
    }

    public class BrandingThemePaymentServiceInfo
    {
        public string BrandingThemeId { get; set; }
        public string BrandingThemeName { get; set; }
        public List<SimplePaymentServiceInfo> PaymentServices { get; set; } = new List<SimplePaymentServiceInfo>();
    }
}
