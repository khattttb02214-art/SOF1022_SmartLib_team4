using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartLib.Web.Areas.Admin.Controllers;

[Area("Admin")]

[Authorize(Roles = "ADMIN")]

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
