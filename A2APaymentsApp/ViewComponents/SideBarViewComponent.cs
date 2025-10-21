using System.Threading.Tasks;
using A2APaymentsApp.IO;
using Microsoft.AspNetCore.Mvc;

namespace A2APaymentsApp.ViewComponents{
    public class SideBarViewComponent : ViewComponent
    {
        public Task<IViewComponentResult> InvokeAsync()
        {
            return Task.FromResult<IViewComponentResult>(View(LocalStorageTokenIO.Instance.TokenExists()));
        }
    }
}