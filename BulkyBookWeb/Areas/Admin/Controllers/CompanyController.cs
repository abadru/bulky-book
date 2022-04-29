using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Controllers;
[Area("Admin")]

public class CompanyController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CompanyController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GET
    public IActionResult Index()
    {
        return View();
    }
    

    //GET
    public IActionResult Upsert(int? id)
    {
        Company company = new();

        if (id == null || id == 0)
        {
            // Create Product
            // ViewBag.CategoryList = categoryList;
            // ViewData["CoverTypeList"] = coverTypeList;
            return View(company);
        }
        else
        {
            //Update Product
            company = _unitOfWork.Company.GetFirstOrDefault(x => x.Id == id);

            return View(company);

        }

    }

    //POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Upsert(Company company)
    {
        if (ModelState.IsValid)
        {
          

            if (company.Id == 0)
            {
                _unitOfWork.Company.Add(company);
            }
            else
            {
                _unitOfWork.Company.Update(company);
            }
            _unitOfWork.Save();
            TempData["success"] = "Company added successfully";
            return RedirectToAction("Index");
        }

        return View(company);
    }

    //GET
    /*public IActionResult Delete(int? id)
    {
        if (id == null || id == 0)
        {
            return NotFound();
        }

        var product =  _unitOfWork.Product.GetFirstOrDefault(x => x.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }*/

    /*//POST
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeletePOST(int? id)
    {
        var product =  _unitOfWork.Product.GetFirstOrDefault(x => x.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        _unitOfWork.Product.Remove(product);
        _unitOfWork.Save();
        TempData["success"] = "Product deleted successfully";
        return RedirectToAction("Index");
    }*/

    #region API CALLS

    [HttpGet]
    public IActionResult GetAll()
    {
        var companies = _unitOfWork.Company.GetAll(null);

        return Json(new { data = companies });
    }
    
    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var company =  _unitOfWork.Company.GetFirstOrDefault(x => x.Id == id);

        if (company == null)
        {
            return Json(new { success = false, message = "Error while deleting company" });
        }
        _unitOfWork.Company.Remove(company);
        _unitOfWork.Save();
        
        return Json(new { success = true, message = "Delete successful" });

    }
    #endregion
}