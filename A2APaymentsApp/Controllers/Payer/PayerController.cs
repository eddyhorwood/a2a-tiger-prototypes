using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Xero.NetStandard.OAuth2.Config;
using A2APaymentsApp.Clients;

namespace A2APaymentsApp.Controllers.Payer
{
    /// <summary>
    /// Controller for handling payer-related operations in A2A payments
    /// </summary>
    public class PayerController : BaseXeroOAuth2Controller
    {
        private readonly IAkahuClient _akahuClient;

        public PayerController(IOptions<XeroConfiguration> xeroConfig, IAkahuClient akahuClient) : base(xeroConfig)
        {
            _akahuClient = akahuClient;
        }

        /// <summary>
        /// GET: /Payer
        /// Display payer dashboard or main page
        /// </summary>
        /// <returns>Returns the payer view</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// GET: /Payer/Payment
        /// Initiate payment process for payers
        /// </summary>
        /// <param name="invoiceNo">Invoice number to pay</param>
        /// <param name="currency">Payment currency</param>
        /// <param name="amount">Payment amount</param>
        /// <param name="shortCode">Short code for payment</param>
        /// <returns>Returns payment initiation view</returns>
        public async Task<IActionResult> Payment(
            [FromQuery] string invoiceNo,
            [FromQuery] string currency,
            [FromQuery] decimal? amount,
            [FromQuery] string shortCode)
        {
            // Validate payment parameters
            if (string.IsNullOrEmpty(invoiceNo) ||
                string.IsNullOrEmpty(currency) ||
                !amount.HasValue ||
                string.IsNullOrEmpty(shortCode))
            {
                ViewBag.Error = "Invalid payment parameters";
                return View();
            }

            // Set payment details in ViewBag
            ViewBag.InvoiceNo = invoiceNo;
            ViewBag.Currency = currency;
            ViewBag.Amount = amount.Value;
            ViewBag.ShortCode = shortCode;

            CreatePaymentRequest request = new CreatePaymentRequest
            {
                Amount = amount.Value,
                RedirectUri = Url.Action("Callback", "Payer", null, Request.Scheme),
                Payee = new PayeeDetails
                {
                    Name = "Test Merchant Ltd",
                    AccountNumber = "01-0001-0012345-00",
                    Particulars = "Invoice",
                    Code = invoiceNo,
                    Reference = shortCode
                }
            };

            // make a call to Akahu to create payment
            var paymentResponse = await _akahuClient.CreatePayment(request);

            // Redirect to Akahu's authorization URL to complete payment
            return Redirect(paymentResponse.AuthorisationUrl);
        }

        /// <summary>
        /// GET: /Payer/Callback
        /// Handle callback from payment provider (e.g., Akahu)
        /// </summary>
        /// <param name="paymentId">Payment ID from provider</param>
        /// <param name="status">Payment status</param>
        /// <returns>Returns payment result view</returns>
        public async Task<IActionResult> Callback(
            [FromQuery] string paymentId,
            [FromQuery] string status)
        {
            if (string.IsNullOrEmpty(paymentId))
            {
                ViewBag.Error = "Invalid payment callback";
                return View();
            }

            ViewBag.PaymentId = paymentId;
            ViewBag.Status = status;

            // Poll Akahu API for payment status until terminal state
            const int maxPolls = 24; // Poll for up to 2 minutes (24 * 5 seconds)
            int pollCount = 0;
            
            PollPaymentResponse paymentStatus;
            
            do
            {
                paymentStatus = await _akahuClient.PollPaymentStatus(paymentId);
                pollCount++;
                
                // Check if payment is in terminal state
                if (paymentStatus.Status == "SENT" || 
                    paymentStatus.Status == "FAILED" || 
                    paymentStatus.Status == "CANCELLED")
                {
                    break;
                }
                
                // Wait 5 seconds before next poll (if not terminal)
                if (pollCount < maxPolls)
                {
                    await Task.Delay(5000);
                }
                
            } while (pollCount < maxPolls);
            
            // Check if payment is successful (SENT status)
            if (paymentStatus.Status == "SENT")
            {
                // TODO: Create payment record in Xero if successful
                
                // Redirect for successful payments
                // TODO: Redirect to a online invoice
                return Redirect("https://xero.com");
            }

            // For non-successful payments or timeout, show the callback view with status
            ViewBag.PaymentStatus = paymentStatus.Status;
            ViewBag.StatusReason = paymentStatus.StatusReason?.Message;
            ViewBag.PollTimeout = pollCount >= maxPolls;
            return View();
        }
    }
}