using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository;

public class OrderDetailRepository: Repository<OrderDetail>, IOrderDetailRepository
{
    public OrderDetailRepository(ApplicationDbContext db) 
        : base(db)
    {
    }
    
    public void Update(OrderDetail obj)
    {
        dbSet.Update(obj);
    }
}