using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using A2APaymentsApp.Models;
using System.Collections.Generic;
using System.Linq;
using Xero.NetStandard.OAuth2.Api;
using Microsoft.Extensions.Options;
using Xero.NetStandard.OAuth2.Config;
using System.Threading.Tasks;
using A2APaymentsApp.Services;

namespace A2APaymentsApp.Controllers
{
    public class MerchantOnboardingController : ApiAccessorController<AccountingApi>
    {

        private readonly DatabaseService _databaseService;

        public MerchantOnboardingController(
            IOptions<XeroConfiguration> xeroConfig,
            DatabaseService databaseService) : base(xeroConfig) 
        {
            _databaseService = databaseService;
        }

        public async Task<IActionResult> Index()
        {

            var model = new MerchantOnboardingModel();
            await PopulateOrganisationDetails();
            await PopulateDropdownData();
            await PopulatePaymentServices();
            return View(model);
        }

        private async Task PopulateOrganisationDetails()
        {
            var xeroOrg = await Api.GetOrganisationsAsync(XeroToken.AccessToken, TenantId);
            if (xeroOrg?._Organisations != null && xeroOrg._Organisations.Count > 0)
            {
                ViewBag.OrganisationName = xeroOrg._Organisations[0].Name;
                ViewBag.OrganisationLegalName = xeroOrg._Organisations[0].LegalName;
                ViewBag.OrganisationCountryCode = xeroOrg._Organisations[0].CountryCode;

            }
        }

        [HttpPost]
        public async Task<IActionResult> Index(MerchantOnboardingModel model)
        {
            var existingOrg = await _databaseService.GetOrganisationByTenantId(TenantId);
            if (existingOrg != null)
            {
                existingOrg.BankAccountNumber = model.BankAccountId;
                existingOrg.AccountIdForPayment = model.ChartOfAccountCode;
                existingOrg.AccessToken = XeroToken.AccessToken;
                existingOrg.RefreshToken = XeroToken.RefreshToken;

                await _databaseService.UpdateOrganisation(existingOrg);

            }
            else
            {
                var xeroOrg = await Api.GetOrganisationsAsync(XeroToken.AccessToken, TenantId);

                var newOrg = new Organisation
                {
                    TenantId = TenantId,
                    TenantShortCode = xeroOrg._Organisations[0].ShortCode, // assume first organisation
                    BankAccountNumber = model.BankAccountId,
                    AccountIdForPayment = model.ChartOfAccountCode,
                    AccessToken = XeroToken.AccessToken,
                    RefreshToken = XeroToken.RefreshToken
                };

                await _databaseService.AddOrganisation(newOrg);
            }

            if (ModelState.IsValid)
            {

                return View(model);
            }

            await PopulateDropdownData();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SelectPaymentService(PaymentServiceSelectionModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownData();
                await PopulatePaymentServices();
                return View("Index", new MerchantOnboardingModel());
            }

            // For now just acknowledge selection; persistence could be added later.
            TempData["SuccessMessage"] = "Payment service selected.";

            return RedirectToAction("Index");
        }

        private async Task PopulatePaymentServices()
        {
            var paymentServices = await Api.GetPaymentServicesAsync(XeroToken.AccessToken, TenantId);
            if (paymentServices?._PaymentServices != null)
            {
                ViewBag.PaymentServices = paymentServices._PaymentServices
                    .Select(ps => new SelectListItem
                    {
                        Value = ps.PaymentServiceID.ToString(),
                        Text = ps.PaymentServiceName
                    })
                    .ToList();
            }
            else
            {
                ViewBag.PaymentServices = new List<SelectListItem>();
            }
        }

        private async Task PopulateDropdownData()
        {
            var accounts = await Api.GetAccountsAsync(XeroToken.AccessToken, TenantId);

            if (accounts?._Accounts != null)
            {
                // Filter bank accounts (Type == AccountType.BANK)
                ViewBag.BankAccounts = accounts._Accounts
                    .Where(account => account.Type == Xero.NetStandard.OAuth2.Model.Accounting.AccountType.BANK)
                    .Select(account => new SelectListItem
                    {
                        Value = account.BankAccountNumber.ToString(),
                        Text = $"{account.Name} ({account.BankAccountNumber})"
                    })
                    .ToList();

                // Filter chart of accounts (everything except bank accounts)
                ViewBag.ChartOfAccountCodes = accounts._Accounts
                    .Where(account => account.Type == Xero.NetStandard.OAuth2.Model.Accounting.AccountType.BANK 
                    || (account != null && account.EnablePaymentsToAccount != null && account.EnablePaymentsToAccount == true) )
                    .Select(account => new SelectListItem
                    {
                        Value = account.Code,
                        Text = $"{account.Code} - {account.Name}"
                    })
                    .ToList();
            }
            else
            {
                // Fallback to empty lists if API call fails
                ViewBag.BankAccounts = new List<SelectListItem>();
                ViewBag.ChartOfAccountCodes = new List<SelectListItem>();
            }
            
            var brandingThemes = await Api.GetBrandingThemesAsync(XeroToken.AccessToken, TenantId);

            if (brandingThemes?._BrandingThemes != null)
            {
                ViewBag.BrandingThemes = brandingThemes._BrandingThemes
                    .Select(theme => new SelectListItem
                    {
                        Value = theme.BrandingThemeID.ToString(),
                        Text = theme.Name
                    })
                    .ToList();
            }
            else
            {
                // Fallback to empty list if API call fails
                ViewBag.BrandingThemes = new List<SelectListItem>();
            }
        }
    }
}