using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QLNS.Models;

namespace QLNS.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login", "Auth");
        if (User.IsInRole("NhanVien") && !User.IsInRole("QuanLy") && !User.IsInRole("HR") && !User.IsInRole("Admin"))
            return RedirectToAction("Index", "Home", new { area = "Employee" });
        if (User.IsInRole("QuanLy") && !User.IsInRole("HR") && !User.IsInRole("Admin"))
            return RedirectToAction("Index", "NhanVien", new { area = "HR" });
        return RedirectToAction("Index", "Dashboard", new { area = "HR" });
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });
}
