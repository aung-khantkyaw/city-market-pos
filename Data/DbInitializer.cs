using CityMarketPOS.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

public static class DbInitializer
{
    public static async Task SeedRolesAndAdminAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        string[] roleNames = { "Manager", "Inventory", "Cashier" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        string adminUsername = "manager1";
        var defaultManager = await userManager.FindByNameAsync(adminUsername);
        if (defaultManager == null)
        {
            var managerUser = new ApplicationUser
            {
                UserName = adminUsername,
                Email = "manager@citymarket.com",
                FullName = "City Market Manager",
                EmailConfirmed = true
            };

            var createPowerUser = await userManager.CreateAsync(managerUser, "Manager@123");
            if (createPowerUser.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, "Manager");
            }
        }

        string inventoryUsername = "inventory1";
        var defaultInventory = await userManager.FindByNameAsync(inventoryUsername);
        if (defaultInventory == null)
        {
            var inventoryUser = new ApplicationUser
            {
                UserName = inventoryUsername,
                Email = "inventory@citymarket.com",
                FullName = "City Market Inventory",
                EmailConfirmed = true
            };

            var createInventoryUser = await userManager.CreateAsync(inventoryUser, "Inventory@123");
            if (createInventoryUser.Succeeded)
            {
                await userManager.AddToRoleAsync(inventoryUser, "Inventory");
            }
        }

        string cashierUsername = "cashier1";
        var defaultCashier = await userManager.FindByNameAsync(cashierUsername);
        if (defaultCashier == null)
        {
            var cashierUser = new ApplicationUser
            {
                UserName = cashierUsername,
                Email = "cashier@citymarket.com",
                FullName = "City Market Cashier",
                EmailConfirmed = true
            };

            var createCashierUser = await userManager.CreateAsync(cashierUser, "Cashier@123");
            if (createCashierUser.Succeeded)
            {
                await userManager.AddToRoleAsync(cashierUser, "Cashier");
            }
        }
    }

    public static void SeedCounters(ApplicationDbContext context)
    {
        if (!context.Counters.Any())
        {
            var counters = new[]
            {
                new Counter
                {
                    Name = "Counter 1",
                    Location = "Main Entrance",
                    Status = "Active",
                    AssignedUserId = null,
                    AssignedUserName = null,
                    Description = "Main entrance counter",
                    CreatedDate = DateTime.Now,
                    CreatedByUserId = "System",
                    CreatedByUserName = "System"
                },
                new Counter
                {
                    Name = "Counter 2",
                    Location = "Back Entrance",
                    Status = "Active",
                    AssignedUserId = null,
                    AssignedUserName = null,
                    Description = "Back entrance counter",
                    CreatedDate = DateTime.Now,
                    CreatedByUserId = "System",
                    CreatedByUserName = "System"
                },
                new Counter
                {
                    Name = "Counter 3",
                    Location = "Express Lane",
                    Status = "Active",
                    AssignedUserId = null,
                    AssignedUserName = null,
                    Description = "Express checkout counter",
                    CreatedDate = DateTime.Now,
                    CreatedByUserId = "System",
                    CreatedByUserName = "System"
                }
            };

            context.Counters.AddRange(counters);
            context.SaveChanges();
        }
    }
}