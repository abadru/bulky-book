using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository;

public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
{
    private readonly ApplicationDbContext _context;

  
    public OrderHeaderRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public void Update(OrderHeader orderHeader)
    {
        _context.OrderHeaders.Update(orderHeader);
    }

    public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
    {
        var orderHeader = _context.OrderHeaders.FirstOrDefault(x => x.Id == id);

        if (orderHeader != null)
        {
            orderHeader.OrderStatus = orderStatus;

            if (paymentStatus != null)
            {
                orderHeader.PaymentStatus = paymentStatus;
            }
        }
    }
    
    public void UpdateStripePaymentID(int id, string sesssionId, string paymentItentId)
    {
        var orderHeader = _context.OrderHeaders.FirstOrDefault(x => x.Id == id);

        if (orderHeader != null)
        {
            orderHeader.SessionId = sesssionId;
            orderHeader.PaymentIntentId = paymentItentId;
            
        }
    }
}