using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Sd.RoleAdmin)]
public class UserController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUnitOfWork _unitOfWork;

    public UserController(ApplicationDbContext db, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _unitOfWork = unitOfWork;
    }
    
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult RoleManagment(string userId)
    {
        RoleManagmentVM roleVm = new RoleManagmentVM()
        {
            ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId, "Company"),
            RoleList = _roleManager.Roles.Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Name
            }),
            CompanyList = _unitOfWork.Company.GetAll().Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Id.ToString()
            })
        };

        roleVm.ApplicationUser.Role = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == userId)).GetAwaiter().GetResult().FirstOrDefault();
        
        return View(roleVm);
    }
    
    [HttpPost]
    public IActionResult RoleManagment(RoleManagmentVM roleManagmentVm)
    {
        string oldRole = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == roleManagmentVm.ApplicationUser.Id)).GetAwaiter().GetResult().FirstOrDefault();

        ApplicationUser applicationUser =
            _unitOfWork.ApplicationUser.Get(u => u.Id == roleManagmentVm.ApplicationUser.Id);
        
        if (!(roleManagmentVm.ApplicationUser.Role == oldRole))
        {
            if (roleManagmentVm.ApplicationUser.Role == Sd.RoleCompany)
            {
                applicationUser.CompanyId = roleManagmentVm.ApplicationUser.CompanyId;
            }

            if (oldRole == Sd.RoleCompany)
            {
                applicationUser.CompanyId = null;
            }

            _unitOfWork.ApplicationUser.Update(applicationUser);
            _unitOfWork.Save();

            _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
            _userManager.AddToRoleAsync(applicationUser, roleManagmentVm.ApplicationUser.Role).GetAwaiter().GetResult();
            
        }
        else
        {
            if (oldRole == Sd.RoleCompany && applicationUser.CompanyId != roleManagmentVm.ApplicationUser.CompanyId)
            {
                applicationUser.CompanyId = roleManagmentVm.ApplicationUser.CompanyId;
                _unitOfWork.ApplicationUser.Update(applicationUser);
                _unitOfWork.Save();
            }
        }
        
        return RedirectToAction(nameof(Index));
    }
    
    #region API CALLS
    
    public IActionResult GetAll()
    {
        var applicationUsers = _unitOfWork.ApplicationUser.GetAll(includeProperties: "Company").ToList();
        applicationUsers.ForEach(u=> { u.Company ??= new Company() { Name = "" }; });
        
        applicationUsers.ForEach(u =>
        {
            u.Role = _userManager.GetRolesAsync(u).GetAwaiter().GetResult().FirstOrDefault();
            if (u.Company == null)
            {
                u.Company = new Company()
                {
                    Name = ""
                };
            }
        });
        return Json(new {data = applicationUsers});
    }
    
    [HttpPost]
    public IActionResult LockUnlock([FromBody]string id)
    {
        var userFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == id);
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

        _unitOfWork.ApplicationUser.Update(userFromDb);
        _unitOfWork.Save();
        return Json(new {success = true, message = "Operation Successful"});
    }
    
    #endregion
    
}