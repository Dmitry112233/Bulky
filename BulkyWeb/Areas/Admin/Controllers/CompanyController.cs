using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Sd.RoleAdmin)]
public class CompanyController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CompanyController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public IActionResult Index()
    {
        var companyList = _unitOfWork.Company.GetAll().ToList();
        return View(companyList);
    }
    
    #region API CALLS
    
    public IActionResult GetAll()
    {
        var companyList = _unitOfWork.Company.GetAll().ToList();
        return Json(new {data = companyList});
    }
    
    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        Company? companyToDelete = _unitOfWork.Company.Get(u => u.Id == id);

        if (companyToDelete == null)
        {
            return Json(new {success = false, message = "Error while deleting"});
        }
        
        _unitOfWork.Company.Remove(companyToDelete);
        _unitOfWork.Save();
       
        return Json(new {success = true, message = "Delete Successful"});
    }
    
    #endregion
    
     public IActionResult Upsert(int? id)
    {
        if (id == 0 || id == null)
        {
            return View(new Company()); 
        }
        else
        {
            Company companyObj= _unitOfWork.Company.Get(u => u.Id == id);
            return View(companyObj);
        }
    }
    
    [HttpPost]
    public IActionResult Upsert(Company companyObj)
    {
        if (ModelState.IsValid)
        {
            if (companyObj.Id == 0)
            {
                _unitOfWork.Company.Add(companyObj);
            }
            else
            {
                _unitOfWork.Company.Update(companyObj);
            }
            _unitOfWork.Save();
            TempData["success"] = "Company created successfully";
            return RedirectToAction("Index");
        }
        else
        {
            return View(companyObj);
        }
    }
}