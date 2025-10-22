using Microsoft.AspNetCore.Mvc;

namespace A2APaymentsApp.Controllers.PublicLandingPage;

public class PublicLandingPageController : Controller
{
    
     // this UI will be used by Xero App Store visitors to add the app to their Xero organization
    public IActionResult Index()
    {
        return View();
    }
    
}