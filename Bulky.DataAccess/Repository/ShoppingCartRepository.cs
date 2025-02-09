using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository;

public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository
{
    public ShoppingCartRepository(ApplicationDbContext db) 
        : base(db)
    {
    }

    public void Update(ShoppingCart obj)
    {
        dbSet.Update(obj);
    }
}