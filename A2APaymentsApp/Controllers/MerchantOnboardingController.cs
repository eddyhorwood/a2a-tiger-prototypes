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

        private string EffectiveTenantId => !string.IsNullOrEmpty(Request.Query["tenantId"]) ? Request.Query["tenantId"].ToString() : TenantId;

        // New unified initializer
        private async Task InitializeViewData(string tenantId)
        {
            ViewBag.TenantId = tenantId;
            await PopulateOrganisationDetails(tenantId);
            await PopulateDropdownData(tenantId); // now only bank/account dropdowns
            await PopulatePaymentServices(tenantId);
            await PopulateBrandingThemes(tenantId); // branding themes info (not a dropdown)
        }

        public async Task<IActionResult> Index()
        {
            var effectiveTid = EffectiveTenantId;
            var model = new MerchantOnboardingModel();
            var existingOrg = await _databaseService.GetOrganisationByTenantId(effectiveTid);
            if (existingOrg != null)
            {
                model.BankAccountId = existingOrg.BankAccountNumber;
                model.ChartOfAccountCode = existingOrg.AccountIdForPayment;
            }
            await InitializeViewData(effectiveTid);
            return View(model);
        }

        private async Task PopulateOrganisationDetails(string tenantId)
        {
            var xeroOrg = await Api.GetOrganisationsAsync(XeroToken.AccessToken, tenantId);
            if (xeroOrg?._Organisations != null && xeroOrg._Organisations.Count > 0)
            {
                ViewBag.OrganisationName = xeroOrg._Organisations[0].Name;
                ViewBag.OrganisationLegalName = xeroOrg._Organisations[0].LegalName;
                ViewBag.OrganisationCountryCode = xeroOrg._Organisations[0].CountryCode;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index(MerchantOnboardingModel model, string tenantId)
        {
            var effectiveTid = !string.IsNullOrEmpty(tenantId) ? tenantId : EffectiveTenantId;
            var existingOrg = await _databaseService.GetOrganisationByTenantId(effectiveTid);
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
                var xeroOrg = await Api.GetOrganisationsAsync(XeroToken.AccessToken, effectiveTid);
                var newOrg = new Organisation
                {
                    TenantId = effectiveTid,
                    TenantShortCode = xeroOrg._Organisations[0].ShortCode,
                    BankAccountNumber = model.BankAccountId,
                    AccountIdForPayment = model.ChartOfAccountCode,
                    AccessToken = XeroToken.AccessToken,
                    RefreshToken = XeroToken.RefreshToken
                };
                await _databaseService.AddOrganisation(newOrg);
            }
            // Always reinitialize view data so dropdowns/org info reflect updated selections
            await InitializeViewData(effectiveTid);
            // Success message after saving details
            TempData["SuccessMessage"] = "Configuration saved.";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SelectPaymentService(PaymentServiceSelectionModel model)
        {
            if (!ModelState.IsValid)
            {
                await InitializeViewData(EffectiveTenantId);
                return View("Index", new MerchantOnboardingModel());
            }
            TempData["SuccessMessage"] = "Payment service selected.";
            return RedirectToAction("Index", new { tenantId = EffectiveTenantId });
        }

        private async Task PopulatePaymentServices(string tenantId)
        {
            var paymentServices = await Api.GetPaymentServicesAsync(XeroToken.AccessToken, tenantId);
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

        private async Task PopulateDropdownData(string tenantId)
        {
            var existingOrg = await _databaseService.GetOrganisationByTenantId(tenantId);
            var currentBankAccountId = existingOrg?.BankAccountNumber ?? string.Empty;
            var currentAccountId = existingOrg?.AccountIdForPayment ?? string.Empty;
            var accounts = await Api.GetAccountsAsync(XeroToken.AccessToken, tenantId);
            if (accounts?._Accounts != null)
            {
                ViewBag.BankAccounts = accounts._Accounts
                    .Where(account => account.Type == Xero.NetStandard.OAuth2.Model.Accounting.AccountType.BANK)
                    .Select(account => new SelectListItem
                    {
                        Value = account.BankAccountNumber?.ToString(),
                        Text = $"{account.Name} ({account.BankAccountNumber})",
                        Selected = !string.IsNullOrEmpty(currentBankAccountId) && string.Equals(account.BankAccountNumber?.ToString(), currentBankAccountId)
                    })
                    .ToList();
                ViewBag.ChartOfAccountCodes = accounts._Accounts
                    .Where(account => account.Type == Xero.NetStandard.OAuth2.Model.Accounting.AccountType.BANK
                        || (account.EnablePaymentsToAccount != null && account.EnablePaymentsToAccount == true))
                    .Select(account => new SelectListItem
                    {
                        Value = account.AccountID.ToString(),
                        Text = $"{account.Name} ({account.BankAccountNumber}) - Code: {account.Code}",
                        Selected = !string.IsNullOrEmpty(currentAccountId) && string.Equals(account.Code, currentAccountId)
                    })
                    .ToList();
            }
            else
            {
                ViewBag.BankAccounts = new List<SelectListItem>();
                ViewBag.ChartOfAccountCodes = new List<SelectListItem>();
            }
        }

        private async Task PopulateBrandingThemes(string tenantId)
        {
            var brandingThemes = await Api.GetBrandingThemesAsync(XeroToken.AccessToken, tenantId);
            if (brandingThemes?._BrandingThemes != null)
            {
                var list = new List<BrandingThemePaymentServiceInfo>();
                foreach (var theme in brandingThemes._BrandingThemes)
                {
                    var info = new BrandingThemePaymentServiceInfo
                    {
                        BrandingThemeId = theme.BrandingThemeID?.ToString() ?? string.Empty,
                        BrandingThemeName = theme.Name
                    };
                    // Only attempt to fetch payment services if the BrandingThemeID is present
                    if (theme.BrandingThemeID != null)
                    {
                        try
                        {
                            var btPaymentServices = await Api.GetBrandingThemePaymentServicesAsync(XeroToken.AccessToken, tenantId, theme.BrandingThemeID.Value);
                            if (btPaymentServices?._PaymentServices != null)
                            {
                                info.PaymentServices.AddRange(btPaymentServices._PaymentServices.Select(ps => new SimplePaymentServiceInfo
                                {
                                    PaymentServiceId = ps.PaymentServiceID?.ToString() ?? string.Empty,
                                    PaymentServiceName = ps.PaymentServiceName
                                }));
                            }
                        }
                        catch
                        {
                            // swallow errors for individual theme payment services to avoid blocking page
                        }
                    }
                    list.Add(info);
                }
                ViewBag.BrandingThemePaymentServices = list;
            }
            else
            {
                ViewBag.BrandingThemePaymentServices = new List<BrandingThemePaymentServiceInfo>();
            }
        }
    }
}