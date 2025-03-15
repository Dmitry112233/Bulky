using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Utility;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.ViewComponents;

public class ShoppingCartViewComponent : ViewComponent
{
    private readonly IUnitOfWork _unitOfWork;

    public ShoppingCartViewComponent(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
        
        if (claim != null)
        {
            if (HttpContext.Session.GetInt32(Sd.SessionCart) == null)
            {
                HttpContext.Session.SetInt32(Sd.SessionCart, _unitOfWork.ShoppingCart
                    .GetAll(u => u.ApplicationUserId == claim.Value).Count());
            }

            return View(HttpContext.Session.GetInt32(Sd.SessionCart));
        }
        else
        {
            HttpContext.Session.Clear();
            return View(0);
        }
    }
}