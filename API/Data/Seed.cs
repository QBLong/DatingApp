using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class Seed
    {
        public static async Task SeedUser(DataContext dataContext) {
            if (await dataContext.Users.AnyAsync()) {
                return;
            }

            var userDatas = await File.ReadAllTextAsync("Data/UserSeedData.json");
            var options = new JsonSerializerOptions{ PropertyNameCaseInsensitive = true };

            var users = JsonSerializer.Deserialize<List<AppUser>>(userDatas);
            foreach(var user in users) {
                using (var hmac = new HMACSHA512()) {
                    user.UserName = user.UserName.ToLower();
                    user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes("Pa$$w0rd"));
                    user.PasswordSalt = hmac.Key;

                    dataContext.Add(user);
                }
            }

            await dataContext.SaveChangesAsync();
        }
    }
}