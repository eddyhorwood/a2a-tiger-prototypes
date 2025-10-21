using System;
using System.Diagnostics;
using A2APaymentsApp.IO;
using A2APaymentsApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace A2APaymentsApp.Controllers
{
    public class HomeController : Controller
    {
        // this UI will be used by Merchants
        public IActionResult Index([FromQuery] Guid? tenantId)
        {
            var tokenIO = LocalStorageTokenIO.Instance;
            
            if (tenantId != null)
                tokenIO.StoreTenantId(tenantId.ToString());
            
            return View(tokenIO.TokenExists());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
