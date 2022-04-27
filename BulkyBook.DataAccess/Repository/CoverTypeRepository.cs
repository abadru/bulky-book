using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository;

public class CoverTypeRepository: Repository<CoverType>, ICoverTypeRepository
{
    private readonly ApplicationDbContext _context;

    public CoverTypeRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public void Update(CoverType coverType)
    {
        _context.Update(coverType);
    }
}