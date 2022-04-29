using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Controllers;
[Area("Admin")]

public class CoverTypeController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CoverTypeController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GET
    public IActionResult Index()
    {
        IEnumerable<CoverType> coverTypes = _unitOfWork.CoverType.GetAll(null);
        return View(coverTypes);
    }

    //GET
    public IActionResult Create()
    {
        return View();
    }

    //POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(CoverType coverType)
    {
       

        if (ModelState.IsValid)
        {
            _unitOfWork.CoverType.Add(coverType);
            _unitOfWork.Save();
            TempData["success"] = "Cover type created successfully";
            return RedirectToAction("Index");
        }

        return View(coverType);
    }

    //GET
    public IActionResult Edit(int? id)
    {
        if (id == null || id == 0)
        {
            return NotFound();
        }

        var coverType =  _unitOfWork.CoverType.GetFirstOrDefault(x => x.Id == id);

        if (coverType == null)
        {
            return NotFound();
        }

        return View(coverType);
    }

    //POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(CoverType coverType)
    {
        if (ModelState.IsValid)
        {
            _unitOfWork.CoverType.Update(coverType);
            _unitOfWork.Save();
            TempData["success"] = "Cover type updated successfully";
            return RedirectToAction("Index");
        }

        return View(coverType);
    }

    //GET
    public IActionResult Delete(int? id)
    {
        if (id == null || id == 0)
        {
            return NotFound();
        }

        var coverType =  _unitOfWork.CoverType.GetFirstOrDefault(x => x.Id == id);

        if (coverType == null)
        {
            return NotFound();
        }

        return View(coverType);
    }

    //POST
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeletePOST(int? id)
    {
        var coverType =  _unitOfWork.CoverType.GetFirstOrDefault(x => x.Id == id);

        if (coverType == null)
        {
            return NotFound();
        }

        _unitOfWork.CoverType.Remove(coverType);
        _unitOfWork.Save();
        TempData["success"] = "Category type deleted successfully";
        return RedirectToAction("Index");
    }
}