using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.DbInitializer;

public class DbInitializer : IDbInitializer
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _db;

    public DbInitializer(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext db)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _db = db;
    }

    public void Initialize()
    {
        try
        {
            if (_db.Database.GetPendingMigrations().Count() > 0)
            {
                _db.Database.Migrate();
            }
        }
        catch (Exception e)
        {
            
        }
        
        if (!_roleManager.RoleExistsAsync(Sd.RoleCustomer).GetAwaiter().GetResult())
        {
            _roleManager.CreateAsync(new IdentityRole(Sd.RoleCustomer)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(Sd.RoleCompany)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(Sd.RoleAdmin)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(Sd.RoleEmployee)).GetAwaiter().GetResult();
            
            _userManager.CreateAsync(new ApplicationUser()
            {
                UserName = "admin@dotnetmastery.com",
                Email = "admin@dotnetmastery.com",
                Name = "Admin Name",
                PhoneNumber = "123123123",
                StreetAddress = "Test street",
                State = "State",
                PostalCode = "222003",
                City = "Chicago"
            }, "Qwerty123!@#").GetAwaiter().GetResult();
            
            
            ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "admin@dotnetmastery.com");
            _userManager.AddToRoleAsync(user, Sd.RoleAdmin).GetAwaiter().GetResult();
        }
        
        return;
    }
}