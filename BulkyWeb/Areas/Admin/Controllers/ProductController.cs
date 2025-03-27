using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Sd.RoleAdmin)]
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
        
        string productPath = @"images/products/product-" + id;
        var finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

        if (Directory.Exists(finalPath))
        {
            string[] files = Directory.GetFiles(finalPath);
            foreach (var file in files)
            {
                System.IO.File.Delete(file);
            }
            Directory.Delete(finalPath);
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
            productVM.Product = _unitOfWork.Product.Get(u => u.Id == id, includeProperties: "ProductImages");
            return View(productVM);
        }
    }
    
    [HttpPost]
    public IActionResult Upsert(ProductVM productVm, List<IFormFile> files)
    {
        if (ModelState.IsValid)
        {
            if (productVm.Product.Id == 0)
            {
                _unitOfWork.Product.Add(productVm.Product);
            }
            else
            {
                _unitOfWork.Product.Update(productVm.Product);
            }
            _unitOfWork.Save();
            
            if (files != null)
            {
                
                var rootPath = _webHostEnvironment.WebRootPath;
                
                foreach (var file in files)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    string productPath = @"images/products/product-" + productVm.Product.Id;
                    var finalPath = Path.Combine(rootPath, productPath);

                    if (!Directory.Exists(finalPath))
                    {
                        Directory.CreateDirectory(finalPath);
                    }
                    
                    using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    ProductImage productImage = new()
                    {
                        ImageUrl =  productPath + "/" + fileName,
                        ProductId = productVm.Product.Id,
                    };

                    if (productVm.Product.ProductImages == null)
                    {
                        productVm.Product.ProductImages = new List<ProductImage> ();
                    }
                    
                    productVm.Product.ProductImages.Add(productImage);
                }
                
                _unitOfWork.Product.Update(productVm.Product);
                _unitOfWork.Save();
            }
            
            TempData["success"] = "Product created/updated successfully";
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

    public IActionResult DeleteImage(int imageId)
    {
        var imageToBeDeleted = _unitOfWork.ProductImage.Get(u => u.Id == imageId);
        var productId = imageToBeDeleted.ProductId;
        if (imageToBeDeleted != null)
        {
            if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
            {
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageToBeDeleted.ImageUrl.TrimStart(Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }
            
            _unitOfWork.ProductImage.Remove(imageToBeDeleted);
            _unitOfWork.Save();
            
            TempData["success"] = "Deleted successfully";
        }

        return RedirectToAction(nameof(Upsert), new { id = productId });
    }
}