using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace BulkyBookWeb.Controllers;

[Area("Admin")]
[Authorize]
public class OrderController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    [BindProperty]
    public OrderVM OrderVM { get; set; }
    public OrderController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    // GET
    public IActionResult Index()
    {
        return View();
    }
    
    // GET
    public IActionResult Details(int orderId)
    {
        OrderVM = new()
        {
            OrderHeader = _unitOfWork.OrderHeader
                .GetFirstOrDefault(x => x.Id == orderId, "ApplicationUser"),
            OrderDetails = _unitOfWork.OrderDetail
                .GetAll(x => x.OrderId == orderId, "Product"),
        };
        return View(OrderVM);
    }
    
    // GET
    [ActionName("Details")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DetailsPayNow()
    {
        OrderVM.OrderHeader = _unitOfWork.OrderHeader
            .GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id, "ApplicationUser");
        OrderVM.OrderDetails = _unitOfWork.OrderDetail
            .GetAll(x => x.OrderId == OrderVM.OrderHeader.Id, "Product");
        
        //Stripe Settings
        var domain = "https://localhost:7210/";
        var options = new SessionCreateOptions
        {
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment",
            SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
            CancelUrl = domain + $"admin/order/details?orderId={OrderVM.OrderHeader.Id}"
        };

        foreach (var item in OrderVM.OrderDetails)
        {
            var sessionLineItem = new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(item.Price * 100),
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.Product.Title,
                    },
                },
                Quantity = item.Count
            };

            options.LineItems.Add(sessionLineItem);
        }

        var service = new SessionService();
        Session session = service.Create(options);
        _unitOfWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id,
            session.PaymentIntentId);
        _unitOfWork.Save();
        
        Response.Headers.Add("Location", session.Url);
        return new StatusCodeResult(303);
    }
    
    public IActionResult PaymentConfirmation(int orderHeaderId)
    {
        var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == orderHeaderId, tracked:false);

        if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            //Check the stripe status
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.ToString() == "paid")
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                _unitOfWork.Save();
            }
        }

        return View(orderHeaderId);
    }
    
    // POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SD.Role_User_Admin+ "," + SD.Role_User_Employee)]
    public IActionResult UpdateOrderDetails()
    {
        var orderHeaderFromDB = _unitOfWork.OrderHeader
            .GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id, 
                "ApplicationUser", false);
        orderHeaderFromDB.Name = OrderVM.OrderHeader.Name;
        orderHeaderFromDB.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
        orderHeaderFromDB.StreetAddress = OrderVM.OrderHeader.StreetAddress;
        orderHeaderFromDB.City = OrderVM.OrderHeader.City;
        orderHeaderFromDB.State = OrderVM.OrderHeader.State;
        orderHeaderFromDB.PostalCode = OrderVM.OrderHeader.PostalCode;

        if (OrderVM.OrderHeader.Carrier != null)
        {
            orderHeaderFromDB.Carrier = OrderVM.OrderHeader.Carrier;
        }
        
        if (OrderVM.OrderHeader.TrackingNumber != null)
        {
            orderHeaderFromDB.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
        }
        
        _unitOfWork.OrderHeader.Update(orderHeaderFromDB);
        _unitOfWork.Save();
        TempData["success"] = "Order updated successfully";
        return RedirectToAction("Details", "Order", new { orderId = orderHeaderFromDB.Id });
    }
    
    // POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SD.Role_User_Admin+ "," + SD.Role_User_Employee)]
    public IActionResult StartProcessing()
    {
        _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
        _unitOfWork.Save();
        TempData["success"] = "Order status updated successfully";
        return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
    }
    
    // POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SD.Role_User_Admin+ "," + SD.Role_User_Employee)]
    public IActionResult ShipOrder()
    {
        var orderHeader = _unitOfWork.OrderHeader
            .GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id, 
                "ApplicationUser", false);
        orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
        orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
        orderHeader.OrderStatus = SD.StatusShipped;
        orderHeader.ShippingDate = DateTime.Now;
        if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
        }
        _unitOfWork.OrderHeader.Update(orderHeader);
        _unitOfWork.Save();
        TempData["success"] = "Order shipped successfully";
        return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
    }
    
    // POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SD.Role_User_Admin+ "," + SD.Role_User_Employee)]
    public IActionResult CancelOrder()
    {
        var orderHeader = _unitOfWork.OrderHeader
            .GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id, 
                "ApplicationUser", false);

        if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
        {
            var options = new RefundCreateOptions
            {
                Reason = RefundReasons.RequestedByCustomer,
                PaymentIntent = orderHeader.PaymentIntentId,
            };

            var service = new RefundService();
            Refund refund = service.Create(options);
            _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusRefunded);
        }
        else
        {
            _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCanceled);
        }
        
        _unitOfWork.Save();
        TempData["success"] = "Order cancelled successfully";
        return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
    }

    
    #region API CALLS
    [HttpGet]
    public IActionResult GetAll(string status)
    {
        IEnumerable<OrderHeader> orderHeaders;
            

        if (User.IsInRole(SD.Role_User_Admin) || User.IsInRole(SD.Role_User_Employee))
        {
            orderHeaders = _unitOfWork.OrderHeader
                .GetAll(null,"ApplicationUser");
        }
        else
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            orderHeaders = _unitOfWork.OrderHeader
                .GetAll(x => x.ApplicationUserId == claim.Value,"ApplicationUser");
        }
        
        switch (status)
        {
            case "pending":
                orderHeaders = orderHeaders.Where(x => x.PaymentStatus == SD.PaymentStatusDelayedPayment); break;
            case "inprocess":
                orderHeaders = orderHeaders.Where(x => x.OrderStatus == SD.StatusInProcess); break;
            case "completed":
                orderHeaders = orderHeaders.Where(x => x.OrderStatus == SD.StatusShipped); break;
            case "approved":
                orderHeaders = orderHeaders.Where(x => x.OrderStatus == SD.StatusApproved); break;

            default:
              break;

        }
        
        return Json(new { data = orderHeaders });
    }
    #endregion

}