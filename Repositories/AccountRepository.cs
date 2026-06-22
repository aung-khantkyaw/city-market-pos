using CityMarketPOS.Models;
using CityMarketPOS.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class AccountRepository : IAccountRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountRepository(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    public async Task<SignInResult> LoginAsync(LoginViewModel model)
    {
        return await _signInManager.PasswordSignInAsync(
            model.UserName, model.Password, isPersistent: false, lockoutOnFailure: false);
    }

    public async Task<IdentityResult> CreateUserAsync(RegisterViewModel model)
    {
        var user = new ApplicationUser
        {
            UserName = model.UserName,
            FullName = model.FullName
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            if (await _roleManager.RoleExistsAsync(model.Role))
            {
                await _userManager.AddToRoleAsync(user, model.Role);
            }
        }
        return result;
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<IEnumerable<StaffListViewModel>> GetAllStaffAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var staffList = new List<StaffListViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            staffList.Add(new StaffListViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                UserName = user.UserName,
                Role = roles.FirstOrDefault() ?? "No Role"
            });
        }
        return staffList;
    }

    public async Task<EditUserViewModel> GetStaffByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new EditUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName,
            Role = roles.FirstOrDefault()
        };
    }

    public async Task<bool> UpdateStaffAsync(EditUserViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return false;

        user.FullName = model.FullName;
        user.UserName = model.UserName;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);
            return true;
        }
        return false;
    }

    public async Task<bool> DeleteStaffAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return false;

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }
}