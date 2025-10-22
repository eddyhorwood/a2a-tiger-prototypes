using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using A2APaymentsApp.Models;
using System.Collections.Generic;
using System.Linq;
using Xero.NetStandard.OAuth2.Api;
using Microsoft.Extensions.Options;
using Xero.NetStandard.OAuth2.Config;
using System.Threading.Tasks;

namespace A2APaymentsApp.Controllers
{
    public class MerchantOnboardingController : ApiAccessorController<AccountingApi>
    {
        public MerchantOnboardingController(IOptions<XeroConfiguration> xeroConfig) : base(xeroConfig) {}

        public async Task<IActionResult> Index()
        {
            var model = new MerchantOnboardingModel();
            await PopulateDropdownData();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(MerchantOnboardingModel model)
        {
            if (ModelState.IsValid)
            {
                // TODO: Persist the values in the next iteration
                // For now, just display a success message
                TempData["SuccessMessage"] = "Merchant onboarding data received successfully!";
                ViewBag.SubmittedData = model;
                await PopulateDropdownData();
                return View(model);
            }

            await PopulateDropdownData();
            return View(model);
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
                    .Where(account => account.Type != Xero.NetStandard.OAuth2.Model.Accounting.AccountType.BANK)
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