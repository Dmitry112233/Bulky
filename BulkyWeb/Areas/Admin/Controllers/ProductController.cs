using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
//[Authorize(Roles = Sd.RoleAdmin)]
public class ProductController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private IWebHostEnvironment _webHostEnvironment;

    public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
    {
        _unitOfWork = unitOfWork;
        _webHostEnvironment = webHostEnvironment;
    }

    public IActionResult Index()
    {
        var productList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
        return View(productList);
    }

    #region API CALLS
    
    public IActionResult GetAll()
    {
        var productList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
        return Json(new {data = productList});
    }
    
    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        Product? productToDelete = _unitOfWork.Product.Get(u => u.Id == id);

        if (productToDelete == null)
        {
            return Json(new {success = false, message = "Error while deleting"});
        }
        
        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, productToDelete.ImageUrl.TrimStart(Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(oldImagePath))
        {
            System.IO.File.Delete(oldImagePath);
        }
        
        _unitOfWork.Product.Remove(productToDelete);
        _unitOfWork.Save();
       
        return Json(new {success = true, message = "Delete Successful"});
    }
    
    #endregion
    
    public IActionResult Upsert(int? id)
    {
        var productVM = new ProductVM()
        {
            Product = new Product(),
            CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem()
            {
                Text = u.Name,
                Value = u.Id.ToString()
            })
        };
        if (id == 0 || id == null)
        {
            return View(productVM); 
        }
        else
        {
            productVM.Product = _unitOfWork.Product.Get(u => u.Id == id);
            return View(productVM);
        }
    }
    
    [HttpPost]
    public IActionResult Upsert(ProductVM productVm, IFormFile? file)
    {
        if (ModelState.IsValid)
        {
            if (file != null)
            {
                var rootPath = _webHostEnvironment.WebRootPath;
                var productFolder = Path.Combine("images", "product");
                var fullImagePath = Path.Combine(rootPath, productFolder);
                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

                if (!string.IsNullOrEmpty(productVm.Product.ImageUrl))
                {
                    var oldImagePath = Path.Combine(rootPath, productVm.Product.ImageUrl.TrimStart(Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
                
                using (var fileStream = new FileStream(Path.Combine(fullImagePath, fileName), FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                productVm.Product.ImageUrl = Path.Combine(Path.DirectorySeparatorChar + productFolder, fileName);
            }

            if (productVm.Product.Id == 0)
            {
                _unitOfWork.Product.Add(productVm.Product);
            }
            else
            {
                _unitOfWork.Product.Update(productVm.Product);
            }
            _unitOfWork.Save();
            TempData["success"] = "Product created successfully";
            return RedirectToAction("Index");
        }
        else
        {
            productVm.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem()
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });
            return View(productVm);
        }
    }
}