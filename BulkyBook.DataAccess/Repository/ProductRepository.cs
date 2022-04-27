using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository;

public class ProductRepository :Repository<Product>, IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public void Update(Product product)
    {
        var productFormDb = _context.Products.FirstOrDefault(x => x.Id == product.Id);

        if (productFormDb != null)
        {
            productFormDb.Title = product.Title;
            productFormDb.ISBN = product.ISBN;
            productFormDb.Price = product.Price;
            productFormDb.Price50 = product.Price50;
            productFormDb.Price100 = product.Price100;
            productFormDb.Description = product.Description;
            productFormDb.CategoryId = product.CategoryId;
            productFormDb.Author = product.Author;
            productFormDb.CoverTypeId = product.CoverTypeId;

            if (productFormDb.ImageUrl != null)
            {
                productFormDb.ImageUrl = product.ImageUrl;
            }
        }
    }
}