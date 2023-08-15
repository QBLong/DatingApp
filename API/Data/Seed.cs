using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class Seed
    {
        public static async Task ClearConnections(DataContext dataContext) {
            dataContext.Connections.RemoveRange(dataContext.Connections);

            await dataContext.SaveChangesAsync();
        }
        public static async Task SeedUser(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager) {
            if (await userManager.Users.AnyAsync()) {
                return;
            }

            var userDatas = await File.ReadAllTextAsync("Data/UserSeedData.json");
            // var options = new JsonSerializerOptions{ PropertyNameCaseInsensitive = true };

            var users = JsonSerializer.Deserialize<List<AppUser>>(userDatas);

            var roles = new List<AppRole> {
                new AppRole {Name = "Member"},
                new AppRole {Name = "Admin"},
                new AppRole {Name = "Moderator"},
            };

            foreach (var role in roles) {
                await roleManager.CreateAsync(role);
            }

            foreach(var user in users) {
                user.UserName = user.UserName.ToLower();
                user.Photos.First().IsApproved = true;
                user.Created = DateTime.SpecifyKind(user.Created, DateTimeKind.Utc);
                user.LastActive = DateTime.SpecifyKind(user.LastActive, DateTimeKind.Utc);

                await userManager.CreateAsync(user, "Pa$$w0rd");
                await userManager.AddToRoleAsync(user, "Member");
            }

            var admin = new AppUser {
                UserName = "admin"
            };

            await userManager.CreateAsync(admin, "Pa$$w0rd");
            await userManager.AddToRolesAsync(admin, new[] {"Admin", "Moderator"});
        }
    }
}