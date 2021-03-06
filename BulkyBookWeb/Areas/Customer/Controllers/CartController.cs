using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace BulkyBookWeb.Controllers;

[Area("Customer")]
[Authorize]
public class CartController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;

    public ShoppingCartVM ShoppingCartVm { get; set; }
    public double OrderTotal { get; set; }

    public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
    {
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
    }

    // GET
    public IActionResult Index()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

        ShoppingCartVm = new ShoppingCartVM
        {
            ListCart = _unitOfWork.ShoppingCart
                .GetAll(x => x.ApplicationUserId == claim.Value, "Product"),
            OrderHeader = new()
        };
        foreach (var cart in ShoppingCartVm.ListCart)
        {
            cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50,
                cart.Product.Price100);
            ShoppingCartVm.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }

        return View(ShoppingCartVm);
    }

    public IActionResult Summary()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

        ShoppingCartVm = new ShoppingCartVM
        {
            ListCart = _unitOfWork.ShoppingCart
                .GetAll(x => x.ApplicationUserId == claim.Value, "Product"),
            OrderHeader = new()
        };

        ShoppingCartVm.OrderHeader.ApplicationUser =
            _unitOfWork.ApplicationUser.GetFirstOrDefault(x => x.Id == claim.Value);

        ShoppingCartVm.OrderHeader.Name = ShoppingCartVm.OrderHeader.ApplicationUser.Name;
        ShoppingCartVm.OrderHeader.PhoneNumber = ShoppingCartVm.OrderHeader.ApplicationUser.PhoneNumber;
        ShoppingCartVm.OrderHeader.StreetAddress = ShoppingCartVm.OrderHeader.ApplicationUser.StreetAddress;
        ShoppingCartVm.OrderHeader.City = ShoppingCartVm.OrderHeader.ApplicationUser.City;
        ShoppingCartVm.OrderHeader.State = ShoppingCartVm.OrderHeader.ApplicationUser.State;
        ShoppingCartVm.OrderHeader.PostalCode = ShoppingCartVm.OrderHeader.ApplicationUser.PostalCode;

        foreach (var cart in ShoppingCartVm.ListCart)
        {
            cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50,
                cart.Product.Price100);
            ShoppingCartVm.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }

        return View(ShoppingCartVm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Summary")]
    public IActionResult SummaryPOST(ShoppingCartVM shoppingCartVm)
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
        shoppingCartVm.ListCart = _unitOfWork.ShoppingCart
            .GetAll(x => x.ApplicationUserId == claim.Value, "Product");

   
        shoppingCartVm.OrderHeader.OrderDate = DateTime.Now;
        shoppingCartVm.OrderHeader.ApplicationUserId = claim.Value;

        foreach (var cart in shoppingCartVm.ListCart)
        {
            cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50,
                cart.Product.Price100);
            shoppingCartVm.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }
        
        ApplicationUser applicationUser = _unitOfWork.ApplicationUser
            .GetFirstOrDefault(x => x.Id == claim.Value);

        if (applicationUser.CompanyId.GetValueOrDefault() == 0)
        {
            shoppingCartVm.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            shoppingCartVm.OrderHeader.OrderStatus = SD.StatusPending;
        }
        else
        {
            shoppingCartVm.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
            shoppingCartVm.OrderHeader.OrderStatus = SD.StatusApproved;
        }


        _unitOfWork.OrderHeader.Add(shoppingCartVm.OrderHeader);
        _unitOfWork.Save();

        foreach (var cart in shoppingCartVm.ListCart)
        {
            OrderDetail orderDetail = new()
            {
                ProductId = cart.ProductId,
                OrderId = shoppingCartVm.OrderHeader.Id,
                Price = cart.Price,
                Count = cart.Count
            };

            _unitOfWork.OrderDetail.Add(orderDetail);
            _unitOfWork.Save();
        }

        if (applicationUser.CompanyId.GetValueOrDefault() == 0)
        {
            //Stripe Settings
            var domain = "https://localhost:7210/";
            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={shoppingCartVm.OrderHeader.Id}",
                CancelUrl = domain + "customer/cart/index"
            };

            foreach (var item in shoppingCartVm.ListCart)
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
            _unitOfWork.OrderHeader.UpdateStripePaymentID(shoppingCartVm.OrderHeader.Id, session.Id,
                session.PaymentIntentId);
            _unitOfWork.Save();
        
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }
        else
        {
            return RedirectToAction("OrderConfirmation", "Cart", new { id = shoppingCartVm.OrderHeader.Id });
        }
    }

    public IActionResult OrderConfirmation(int id)
    {
        var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == id, includeProperties: "ApplicationUser");

        if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
        {
            //Check the stripe status
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.ToString() == "paid")
            {
                _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                _unitOfWork.Save();
            }
        }

        _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - Bulky Book", 
            "<p>New Order Created</p>");
        HttpContext.Session.Clear();

        var shoppingCarts =
            _unitOfWork.ShoppingCart
                .GetAll(x => x.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
        _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
       _unitOfWork.Save();

       return View(id);
    }

    public IActionResult Plus(int cartId)
    {
        var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
        _unitOfWork.ShoppingCart.IncrementCount(cart, 1);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Minus(int cartId)
    {
        var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);

        if (cart.Count <= 1)
        {
            _unitOfWork.ShoppingCart.Remove(cart);
            var count = _unitOfWork.ShoppingCart.GetAll(filter: x => x.ApplicationUserId == cart.ApplicationUserId)
                .ToList().Count() - 1;
            HttpContext.Session.SetInt32(SD.SessionCart, count );
        }
        else
        {
            _unitOfWork.ShoppingCart.DecrementCount(cart, 1);
        }

        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }


    public IActionResult Remove(int cartId)
    {
        var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
        _unitOfWork.ShoppingCart.Remove(cart);
        
        _unitOfWork.Save();

        var count = _unitOfWork.ShoppingCart.GetAll(filter: x => x.ApplicationUserId == cart.ApplicationUserId).ToList()
            .Count();
        HttpContext.Session.SetInt32(SD.SessionCart, count - 1);
        return RedirectToAction(nameof(Index));
    }

    private double GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
    {
        if (quantity <= 50)
        {
            return price;
        }
        else
        {
            if (quantity <= 100)
            {
                return price50;
            }

            return price100;
        }
    }
}