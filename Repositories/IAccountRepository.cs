using CityMarketPOS.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

public interface IAccountRepository
{
    Task<SignInResult> LoginAsync(LoginViewModel model);
    Task<IdentityResult> CreateUserAsync(RegisterViewModel model);
    Task LogoutAsync();
    Task<IEnumerable<StaffListViewModel>> GetAllStaffAsync();
    Task<EditUserViewModel> GetStaffByIdAsync(string id);
    Task<bool> UpdateStaffAsync(EditUserViewModel model);
    Task<bool> DeleteStaffAsync(string id);
}