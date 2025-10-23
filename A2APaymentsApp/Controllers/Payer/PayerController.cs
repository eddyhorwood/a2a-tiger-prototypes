using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using A2APaymentsApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Xero.NetStandard.OAuth2.Config;
using A2APaymentsApp.Clients;
using Xero.NetStandard.OAuth2.Api;
using Xero.NetStandard.OAuth2.Model.Accounting;
using Organisation = A2APaymentsApp.Models.Organisation;

namespace A2APaymentsApp.Controllers.Payer
{
    /// <summary>
    /// Controller for handling payer-related operations in A2A payments
    /// </summary>
    public class PayerController : Controller
    {
        private readonly IOptions<XeroConfiguration> _xeroConfig;
        private readonly IAkahuClient _akahuClient;

        private readonly DatabaseService _databaseService;

        public PayerController(IOptions<XeroConfiguration> xeroConfig, IAkahuClient akahuClient, 
            DatabaseService databaseService)
        {
            _xeroConfig = xeroConfig;
            _akahuClient = akahuClient;
            _databaseService = databaseService;
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

            var orgData = await _databaseService.GetOrganisationByShortCode(shortCode);
            if (orgData == null)
            {
                ViewBag.Error = "Org doesn't exist";
                return View();
            }
            
            // TODO Validate invoice exists

            var request = new CreatePaymentRequest
            {
                Amount = amount.Value,
                // TODO add query string in Url.Action
                RedirectUri = Url.Action("Callback", "Payer", new { organisationShortCode = orgData.TenantShortCode, invoiceNumber = invoiceNo }, Request.Scheme),
                Payee = new PayeeDetails
                {
                    Name = "Paywaka Org", // TODO we should also store organisation name in db and use it here ??
                    AccountNumber = FormatNzBankAccountNumber(orgData.BankAccountNumber),
                    Particulars = "Invoice",
                    Code = invoiceNo, 
                    Reference = invoiceNo // TODO use payer name perhaps ?? we can get it from Get Invoice Xero API ??
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
        /// <param name="invoiceNumber"></param>
        /// <param name="status">Payment status</param>
        /// <param name="organisationShortCode"></param>
        /// <returns>Returns payment result view</returns>
        public async Task<IActionResult> Callback(
            [FromQuery] string paymentId,
            [FromQuery]string organisationShortCode,
            [FromQuery]string invoiceNumber,
            [FromQuery] string status)
        {
            if (string.IsNullOrEmpty(paymentId) 
                || string.IsNullOrEmpty(organisationShortCode) 
                || string.IsNullOrEmpty(invoiceNumber))
            {
                ViewBag.Error = "Invalid payment callback";
                return View();
            }
            
            // Poll Akahu API for payment status until terminal state
            const int maxPolls = 24; // Poll for up to 24 seconds (24 * 1 second)
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
                
                // Wait 1 second before next poll (if not terminal)
                if (pollCount < maxPolls)
                {
                    await Task.Delay(1000);
                }
                
            } while (pollCount < maxPolls);
            
            // Check if payment is successful (SENT status)
            if (paymentStatus.Status == "SENT")
            {
               var orgData = await _databaseService.GetOrganisationByShortCode(organisationShortCode);
               var accessToken = await RefreshAccessTokenAsync(orgData);

                var accountingApi = new AccountingApi();
                var invoicesResponse = await accountingApi.GetInvoicesAsync(accessToken, orgData.TenantId, invoiceNumbers: new List<string> { invoiceNumber });
                var invoice = invoicesResponse._Invoices.First();
                if (!invoice.InvoiceID.HasValue)
                {
                    ViewBag.Error = "Innvoice not found";
                    return View();
                }

                var invoiceId = invoice.InvoiceID.Value;
                var onlineInvoiceResponse = await accountingApi
                    .GetOnlineInvoiceAsync(accessToken, orgData.TenantId, invoiceId);
                var onlineInvoiceUrl = onlineInvoiceResponse._OnlineInvoices.First().OnlineInvoiceUrl;

                try
                {
                    await RecordPaymentToInvoice(accountingApi, orgData, invoiceId, paymentStatus.Amount, accessToken);
                }
                catch (Exception e)
                {
                    // TODO remove try catch 
                }
                
            
                return Redirect(onlineInvoiceUrl);
            }
            
            
            // For non-successful payments or timeout, show the callback view with status
            ViewBag.PaymentStatus = paymentStatus.Status;
            ViewBag.StatusReason = paymentStatus.StatusReason?.Message;
            ViewBag.PollTimeout = pollCount >= maxPolls;
            return View();
        }

        private async Task RecordPaymentToInvoice(AccountingApi accountingApi, Organisation orgData, Guid invoiceId, decimal amount, string accessToken)
        {
            var payment = new Payment
            {
                Invoice = new Invoice
                {
                    InvoiceID = invoiceId
                },
                Account = new Account
                {
                    AccountID = Guid.Parse(orgData.AccountIdForPayment)
                },
                Amount = amount,
                Date = DateTime.Today.Date
            };
            var payments = new Payments() { _Payments = new List<Payment> { payment } };

            await accountingApi.CreatePaymentsAsync(accessToken, orgData.TenantId, payments);
        }
        
        private async Task<string> GetOnlineInvoiceUrl(Organisation orgData, Guid invoiceId, string accessToken)
        {
            var accountingApi = new AccountingApi();
            var onlineInvoiceResponse = await accountingApi
                .GetOnlineInvoiceAsync(accessToken, orgData.TenantId, invoiceId);

            return onlineInvoiceResponse._OnlineInvoices.First().OnlineInvoiceUrl;
        }

        /// <summary>
        /// Formats a bank account number into New Zealand format (XX-XXXX-XXXXXXX-XX or XX-XXXX-XXXXXXX-XXX)
        /// </summary>
        /// <param name="accountNumber">Bank account number without dashes</param>
        /// <returns>Formatted bank account number with dashes</returns>
        private string FormatNzBankAccountNumber(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return accountNumber;

            // Remove any existing dashes or spaces
            string cleanNumber = accountNumber.Replace("-", "").Replace(" ", "");

            // NZ bank account format: XX-XXXX-XXXXXXX-XX (15 digits) or XX-XXXX-XXXXXXX-XXX (16 digits)
            if (cleanNumber.Length == 15)
            {
                return $"{cleanNumber.Substring(0, 2)}-{cleanNumber.Substring(2, 4)}-{cleanNumber.Substring(6, 7)}-{cleanNumber.Substring(13, 2)}";
            }
            else if (cleanNumber.Length == 16)
            {
                return $"{cleanNumber.Substring(0, 2)}-{cleanNumber.Substring(2, 4)}-{cleanNumber.Substring(6, 7)}-{cleanNumber.Substring(13, 3)}";
            }
            else
            {
                // Return original if length doesn't match expected NZ format
                return accountNumber;
            }
        }

        /// <summary>
        /// Retrieves a fresh access token for the organization using stored refresh token
        /// </summary>
        /// <param name="organisation"></param>
        /// <returns>Access token string</returns>
        private async Task<string> RefreshAccessTokenAsync(Organisation organisation)
        {
            // TODO only refresh if access token is expired
            
            var clientId = _xeroConfig.Value.ClientId;
            var clientSecret = _xeroConfig.Value.ClientSecret;
            var authString = $"{clientId}:{clientSecret}";
            var authBytes = Encoding.UTF8.GetBytes(authString);
            var base64AuthString = Convert.ToBase64String(authBytes);

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                base64AuthString
            );
            
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("refresh_token", organisation.RefreshToken)
            });
            
            var response = await 
                httpClient.PostAsync("https://identity.xero.com/connect/token", formContent);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                
                var newAccessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();
                var newRefreshToken = jsonDoc.RootElement.GetProperty("refresh_token").GetString();

                // save new refresh token to db
                await _databaseService.UpdateRefreshToken(organisation, 
                    newAccessToken, newRefreshToken);
                
                return newAccessToken;
            }

            throw new Exception("Failed to refresh access token.");
        }
    }
}