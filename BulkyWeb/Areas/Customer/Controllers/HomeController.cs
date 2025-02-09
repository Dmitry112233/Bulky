using System.Diagnostics;
using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Customer.Controllers;

[Area("Customer")]
public class HomeController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var productList = _unitOfWork.Product.GetAll(includeProperties:"Category").ToList();
        return View(productList);
    }
    
    public IActionResult Details(int productId)
    {
        ShoppingCart cart = new ShoppingCart()
        {
            Product = _unitOfWork.Product.Get(u => u.Id == productId, "Category"),
            Count = 1,
            ProductId = productId
        };
        
        return View(cart);
    }
    
    [HttpPost]
    [Authorize]
    public IActionResult Details(ShoppingCart shoppingCart)
    {
        if (ModelState.IsValid)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.ApplicationUserId = userId;

            var shoppingCartInDb = _unitOfWork.ShoppingCart
                .Get(u => u.ProductId == shoppingCart.ProductId && u.ApplicationUserId == userId);

            if (shoppingCartInDb == null)
            {
                _unitOfWork.ShoppingCart.Add(shoppingCart);
            }
            else
            {
                shoppingCartInDb.Count += shoppingCart.Count;
                _unitOfWork.ShoppingCart.Update(shoppingCartInDb);
            }

            TempData["success"] = "Cart updated successfully";

            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}