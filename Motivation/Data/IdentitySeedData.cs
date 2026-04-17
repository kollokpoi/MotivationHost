using Microsoft.AspNetCore.Identity;

namespace Motivation.Data
{
    public class IdentitySeedData
    {
        private const string adminUser = "Admin";
        private const string adminPassword = "Secret123$";

        public static async Task EnsurePopulated(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {

            var adminRole = await roleManager.FindByNameAsync("Admins");
            if (adminRole == null)
            {
                await roleManager.CreateAsync(new IdentityRole("Admins"));
            }

            var managerRole = await roleManager.FindByNameAsync("Managers");
            if (managerRole == null)
            {
                await roleManager.CreateAsync(new IdentityRole("Managers"));
            }

            var usersRole = await roleManager.FindByNameAsync("Users");
            if (usersRole == null)
            {
                await roleManager.CreateAsync(new IdentityRole("Users"));
            }

            var user = await userManager.FindByNameAsync(adminUser);
            if (user == null)
            {
                user = new IdentityUser { UserName = "Admin", Email = "admin@mail.com" };
                await userManager.CreateAsync(user, adminPassword);
                await userManager.AddToRoleAsync(user, "Admins");
            }

            user = await userManager.FindByNameAsync("Manager");
            if (user == null)
            {
                user = new IdentityUser { UserName = "Manager", Email = "manager@mail.com" };
                await userManager.CreateAsync(user, adminPassword);
                await userManager.AddToRoleAsync(user, "Managers");
            }
        }
    }
}
