using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Controllers;
[Area("Admin")]

public class ProductController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
    {
        _unitOfWork = unitOfWork;
        _webHostEnvironment = webHostEnvironment;
    }

    // GET
    public IActionResult Index()
    {
        return View();
    }
    

    //GET
    public IActionResult Upsert(int? id)
    {
        ProductVM productVM = new()
        {
            Product = new Product(),
            CategoryList = _unitOfWork.Category.GetAll(null)
                .Select(category => new SelectListItem
                {
                    Text = category.Name,
                    Value = category.Id.ToString()
                }),
            CoverTypeList = _unitOfWork.CoverType.GetAll(null)
                .Select(coverType => new SelectListItem
                {
                    Text = coverType.Name,
                    Value = coverType.Id.ToString()
                })
        };

        if (id == null || id == 0)
        {
            // Create Product
            // ViewBag.CategoryList = categoryList;
            // ViewData["CoverTypeList"] = coverTypeList;
            return View(productVM);
        }
        else
        {
            //Update Product
            productVM.Product = _unitOfWork.Product.GetFirstOrDefault(x => x.Id == id);

            return View(productVM);

        }

    }

    //POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Upsert(ProductVM productVM, IFormFile file)
    {
        if (ModelState.IsValid)
        {
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            if (file != null)
            {
                string fileName = Guid.NewGuid().ToString();
                var uploads = Path.Combine(wwwRootPath, @"images/products");
                var extension = Path.GetExtension(file.FileName);

                if (productVM.Product.ImageUrl != null)
                {
                    var oldImagePath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                using (var fileStream = new FileStream(Path.Combine(uploads, fileName+extension), FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                productVM.Product.ImageUrl = @"/images/products/" + fileName + extension;
            }

            if (productVM.Product.Id == 0)
            {
                _unitOfWork.Product.Add(productVM.Product);
            }
            else
            {
                _unitOfWork.Product.Update(productVM.Product);
            }
            _unitOfWork.Save();
            TempData["success"] = "Product added successfully";
            return RedirectToAction("Index");
        }

        return View(productVM);
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
        var productList = _unitOfWork.Product.GetAll(null,"Category,CoverType");

        return Json(new { data = productList });
    }
    
    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var product =  _unitOfWork.Product.GetFirstOrDefault(x => x.Id == id);

        if (product == null)
        {
            return Json(new { success = false, message = "Error while deleting product" });
        }
        string wwwRootPath = _webHostEnvironment.WebRootPath;

        
        var oldImagePath = Path.Combine(wwwRootPath, product.ImageUrl.TrimStart('\\'));
        if (System.IO.File.Exists(oldImagePath))
        {
            System.IO.File.Delete(oldImagePath);
        }

        _unitOfWork.Product.Remove(product);
        _unitOfWork.Save();
        
        return Json(new { success = true, message = "Delete successful" });

    }
    #endregion
}