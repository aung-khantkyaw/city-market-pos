using CityMarketPOS.Models;
using CityMarketPOS.Models.ViewModels;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class AccountController : Controller
{
    private readonly IAccountRepository _accountRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context; 
    private readonly IAuditLogRepository _auditLogRepo; 

    public AccountController(IAccountRepository accountRepo, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IAuditLogRepository auditLogRepo)
    {
        _accountRepo = accountRepo; 
        _userManager = userManager;
        _context = context;
        _auditLogRepo = auditLogRepo;
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
        var user = await _userManager.GetUserAsync(User);

        // User ရှိပြီး Cashier ဖြစ်ခဲ့ရင်
        if (user != null && await _userManager.IsInRoleAsync(user, "Cashier"))
        {
            // Active ဖြစ်နေတဲ့ Session ကို ရှာမယ်
            var activeSession = await _context.POSSessions
                .Include(s => s.Counter) // Audit log မှာ Counter Name ထည့်ဖို့
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.Status == "Active");

            if (activeSession != null)
            {
                // Session ကို Close လုပ်မယ်
                activeSession.Status = "Closed";
                activeSession.EndTime = DateTime.Now;

                _context.POSSessions.Update(activeSession);
                await _context.SaveChangesAsync();

                // HttpContext Session ကို Clear လုပ်မယ်
                HttpContext.Session.Clear();

                // Audit Log မှတ်မယ်
                await _auditLogRepo.LogAsync("POSSession", activeSession.Id.ToString(), "Close", user.Id, user.UserName, $"Ended POS session at counter: {activeSession.Counter?.Name} due to Logout");
            }
        }

        // Normal Logout Process
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