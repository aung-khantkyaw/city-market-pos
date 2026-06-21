using CityMarketPOS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class AccountController : Controller
{
    private readonly IAccountRepository _accountRepo;

    public AccountController(IAccountRepository accountRepo)
    {
        _accountRepo = accountRepo;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login() => View();

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _accountRepo.LoginAsync(model);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home"); 
            }
            ModelState.AddModelError("", "Invalid Username or Password!");
        }
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = "Manager")]
    public IActionResult CreateUser() => View();

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> CreateUser(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _accountRepo.CreateUserAsync(model);

            if (result.Succeeded)
            {
                TempData["Success"] = "User Account Create Successfully.";
                return RedirectToAction("CreateUser", "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _accountRepo.LogoutAsync();
        return RedirectToAction("Login", "Account");
    }

    public async Task<IActionResult> Index()
    {
        var staff = await _accountRepo.GetAllStaffAsync();
        return View(staff);
    }

    public async Task<IActionResult> EditUser(string id)
    {
        var staff = await _accountRepo.GetStaffByIdAsync(id);
        if (staff == null) return NotFound();
        return View(staff);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var success = await _accountRepo.UpdateStaffAsync(model);
        if (success)
        {
            TempData["Success"] = "User updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", "Failed to update user.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var success = await _accountRepo.DeleteStaffAsync(id);
        if (success)
        {
            TempData["Success"] = "User deleted successfully!";
        }
        else
        {
            TempData["Error"] = "Failed to delete user.";
        }
        return RedirectToAction(nameof(Index));
    }
}