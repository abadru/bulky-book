using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.DataAccess.DbInitializer;

public class DbInitializer : IDbInitializer
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public DbInitializer(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public void Initilize()
    {
        //Apply pending migrations
        try
        {
            if (_context.Database.GetPendingMigrations().Count() > 0)
            {
                _context.Database.Migrate();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        //Create roles
        if (!_roleManager.RoleExistsAsync(SD.Role_User_Admin).GetAwaiter().GetResult())
        {
            _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Admin)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Comp)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Indi)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.Role_User_Employee)).GetAwaiter().GetResult();
            
            // Create Admin User
            _userManager.CreateAsync(new ApplicationUser
            {
                Email = "admin@dotnetmastery.com",
                UserName = "admin@dotnetmastery.com",
                Name = "admin@dotnetmastery.com",
                PhoneNumber = "2838933",
                StreetAddress = "Av 133",
                City = "Maputo",
                State = "Maputo",
                PostalCode = "11345",
            }, "Pa$$w0rd1").GetAwaiter().GetResult();

            var user = _context.ApplicationUsers
                .FirstOrDefaultAsync(x => x.Email == "admin@dotnetmastery.com").GetAwaiter().GetResult();
            
            _userManager.AddToRoleAsync(user, SD.Role_User_Admin).GetAwaiter().GetResult();
            ;
        }
        
        return;
    }
}