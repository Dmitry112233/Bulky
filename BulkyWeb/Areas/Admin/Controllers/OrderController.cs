using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class OrderController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    
    [BindProperty]
    public OrderVM OrderVm { get; set; }

    public OrderController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public IActionResult Index()
    {
        return View();
    }
    
    public IActionResult Details(int orderId)
    {
        OrderVm  = new OrderVM()
        {
            OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
            OrderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
        };
            
        return View(OrderVm);
    }
    
    [HttpPost]
    [Authorize(Roles = Sd.RoleAdmin + "," + Sd.RoleEmployee)]
    public IActionResult UpdateOrderDetail()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVm.OrderHeader.Id);
        orderHeaderFromDb.Name = OrderVm.OrderHeader.Name;
        orderHeaderFromDb.PhoneNumber = OrderVm.OrderHeader.PhoneNumber;
        orderHeaderFromDb.StreetAddress = OrderVm.OrderHeader.StreetAddress;
        orderHeaderFromDb.City = OrderVm.OrderHeader.City;
        orderHeaderFromDb.State = OrderVm.OrderHeader.State;
        orderHeaderFromDb.PostalCode = OrderVm.OrderHeader.PostalCode;
        if (!string.IsNullOrEmpty(OrderVm.OrderHeader.Carrier))
        {
            orderHeaderFromDb.Carrier = OrderVm.OrderHeader.Carrier;
        }
        if (!string.IsNullOrEmpty(OrderVm.OrderHeader.TrackingNumber))
        {
            orderHeaderFromDb.Carrier = OrderVm.OrderHeader.TrackingNumber;
        }
        
        _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
        _unitOfWork.Save();

        TempData["Success"] = "Order Details Updated Successfully.";
        
        return RedirectToAction(nameof(Details), new {orderId = orderHeaderFromDb.Id});
    }

    [HttpPost]
    [Authorize(Roles = Sd.RoleAdmin + "," + Sd.RoleEmployee)]
    public IActionResult StartProcessing()
    {
        _unitOfWork.OrderHeader.UpdateStatus(OrderVm.OrderHeader.Id, Sd.StatusInProgress);
        _unitOfWork.Save();
       
        TempData["Success"] = "Order Details Updated Successfully.";
        
        return RedirectToAction(nameof(Details), new {orderId = OrderVm.OrderHeader.Id});
    }
    
    [HttpPost]
    [Authorize(Roles = Sd.RoleAdmin + "," + Sd.RoleEmployee)]
    public IActionResult ShipOrder()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVm.OrderHeader.Id);

        orderHeaderFromDb.TrackingNumber = OrderVm.OrderHeader.TrackingNumber;
        orderHeaderFromDb.Carrier = OrderVm.OrderHeader.Carrier;
        orderHeaderFromDb.OrderStatus = Sd.StatusShipped;
        orderHeaderFromDb.ShippingDate = DateTime.Now;

        if (orderHeaderFromDb.PaymentStatus == Sd.PaymentStatusDelayedPayment)
        {
            orderHeaderFromDb.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
        }

        _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
        _unitOfWork.Save();
       
        TempData["Success"] = "Order Shipped Successfully.";
        
        return RedirectToAction(nameof(Details), new {orderId = OrderVm.OrderHeader.Id});
    }
    
    [HttpPost]
    [Authorize(Roles = Sd.RoleAdmin + "," + Sd.RoleEmployee)]
    public IActionResult CancelOrder()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVm.OrderHeader.Id);

        if (orderHeaderFromDb.PaymentStatus == Sd.PaymentStatusApproved)
        {
            var options = new RefundCreateOptions()
            {
                Reason = RefundReasons.RequestedByCustomer,
                PaymentIntent = orderHeaderFromDb.PaymentIntentId
            };

            var service = new RefundService();
            var refund = service.Create(options);
            
            _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, Sd.StatusCanceled, Sd.StatusRefunded);
        }
        else
        {
            _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, Sd.StatusCanceled, Sd.StatusCanceled);
        }
        
        _unitOfWork.Save();
       
        TempData["Success"] = "Order canceled Successfully.";
        
        return RedirectToAction(nameof(Details), new {orderId = OrderVm.OrderHeader.Id});
    }

    [ActionName("Details")]
    [HttpPost]
    [Authorize(Roles = Sd.RoleAdmin + "," + Sd.RoleEmployee)]
    public IActionResult DetailsPayNow()
    {
        OrderVm.OrderHeader =
            _unitOfWork.OrderHeader.Get(u => u.Id == OrderVm.OrderHeader.Id, includeProperties: "ApplicationUser");
        OrderVm.OrderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == OrderVm.OrderHeader.Id,
            includeProperties: "Product");
        
        //stripe logic
        var domain = "http://localhost:5025/";
        var options = new SessionCreateOptions
        {
            SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVm.OrderHeader.Id}",
            CancelUrl = domain + $"admin/order/details?orderId={OrderVm.OrderHeader.Id}",
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment",
        };

        foreach (var item in OrderVm.OrderDetails)
        {
            var sessionListItem = new SessionLineItemOptions()
            {
                PriceData = new SessionLineItemPriceDataOptions()
                {
                    UnitAmount = (long)(item.Price * 100),
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions()
                    {
                        Name = item.Product.Title
                    }
                },
                Quantity = item.Count
            };
            options.LineItems.Add(sessionListItem);
        }
            
        var service = new SessionService();
        Session session = service.Create(options);
            
        _unitOfWork.OrderHeader.UpdateStripePaymentId(OrderVm.OrderHeader.Id, session.Id, session.PaymentIntentId);
        _unitOfWork.Save();
            
        Response.Headers.Append("Location", session.Url);
        return new StatusCodeResult(303);
    }
    
    public IActionResult PaymentConfirmation(int orderHeaderId)
    {
        OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);
        if (orderHeader.PaymentStatus == Sd.PaymentStatusDelayedPayment)
        {
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus == "paid")
            {
                _unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, Sd.PaymentStatusApproved);
                _unitOfWork.Save();
            }
        }
        
        return View(orderHeaderId);
    }
    
    #region API CALLS
    
    public IActionResult GetAll(string status)
    {
        IEnumerable<OrderHeader> orderHeaders;

        if (User.IsInRole(Sd.RoleAdmin) || User.IsInRole(Sd.RoleEmployee))
        {
            orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
        }
        else
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            
            orderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
        }
        
        switch (status)
        {
            case "pending" : orderHeaders = orderHeaders.Where(u => u.PaymentStatus == Sd.PaymentStatusDelayedPayment);
                break;
            case "inprogress" : orderHeaders = orderHeaders.Where(u => u.OrderStatus == Sd.StatusInProgress);
                break;
            case "completed" : orderHeaders = orderHeaders.Where(u => u.OrderStatus == Sd.StatusShipped);
                break;
            case "approved" : orderHeaders = orderHeaders.Where(u => u.OrderStatus == Sd.StatusApproved);
                break;
        }
        
        return Json(new {data = orderHeaders});
    }
    
    #endregion
}