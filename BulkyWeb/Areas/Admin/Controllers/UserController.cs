using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Sd.RoleAdmin)]
public class UserController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public UserController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }
    
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult RoleManagment(string userId)
    {
        string roleId = _db.UserRoles.FirstOrDefault(u => u.UserId == userId).RoleId;

        RoleManagmentVM roleVm = new RoleManagmentVM()
        {
            ApplicationUser = _db.ApplicationUsers.Include(u => u.Company).FirstOrDefault(u => u.Id == userId),
            RoleList = _db.Roles.Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Name
            }),
            CompanyList = _db.Companies.Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Id.ToString()
            })
        };

        roleVm.ApplicationUser.Role = _db.Roles.FirstOrDefault(u => u.Id == roleId).Name;
        
        return View(roleVm);
    }
    
    [HttpPost]
    public IActionResult RoleManagment(RoleManagmentVM roleManagmentVm)
    {
        string roleId = _db.UserRoles.FirstOrDefault(u => u.UserId == roleManagmentVm.ApplicationUser.Id).RoleId;
        string oldRole = _db.Roles.FirstOrDefault(u => u.Id == roleId).Name;

        if (!(roleManagmentVm.ApplicationUser.Role == oldRole))
        {
            ApplicationUser applicationUser =
                _db.ApplicationUsers.FirstOrDefault(u => u.Id == roleManagmentVm.ApplicationUser.Id);
            if (roleManagmentVm.ApplicationUser.Role == Sd.RoleCompany)
            {
                applicationUser.CompanyId = roleManagmentVm.ApplicationUser.CompanyId;
            }

            if (oldRole == Sd.RoleCompany)
            {
                applicationUser.CompanyId = null;
            }

            _db.SaveChanges();

            _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
            _userManager.AddToRoleAsync(applicationUser, roleManagmentVm.ApplicationUser.Role).GetAwaiter().GetResult();
            
        }
        
        return RedirectToAction(nameof(Index));
    }
    
    #region API CALLS
    
    public IActionResult GetAll()
    {
        var applicationUsers = _db.ApplicationUsers.Include(u => u.Company).ToList();
        applicationUsers.ForEach(u=> { u.Company ??= new Company() { Name = "" }; });

        var userRoles = _db.UserRoles.ToList();
        var roles = _db.Roles.ToList();
        
        applicationUsers.ForEach(u =>
        {
            var roleId = userRoles.FirstOrDefault(x => x.UserId == u.Id)?.RoleId;
            u.Role = roles.FirstOrDefault(i => i.Id == roleId).Name;
        });
        return Json(new {data = applicationUsers});
    }
    
    [HttpPost]
    public IActionResult LockUnlock([FromBody]string id)
    {
        var userFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);
        if (userFromDb == null)
        {
            return Json(new {success = true, message = "Error while Locking/Unlocking"});
        }

        if (userFromDb.LockoutEnd != null && userFromDb.LockoutEnd > DateTime.Now)
        {
            userFromDb.LockoutEnd = DateTime.Now;
        }
        else
        {
            userFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
        }

        _db.SaveChanges();
        return Json(new {success = true, message = "Operation Successful"});
    }
    
    #endregion
    
}