using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace BulkyWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
	//[Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IWebHostEnvironment _webHostEnvironment;
		public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
		{
			_unitOfWork = unitOfWork;
			_webHostEnvironment = webHostEnvironment;
		}

		public IActionResult Index()
		{
			List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties : "Category").ToList();

			return View(objProductList);
		}

		// Upsert = Update + Insert
		public IActionResult Upsert(int? id)
		{
			// To populate Category List dropdown values
			ProductVM productVM = new()
			{
				CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
				{
					Text = u.Name,
					Value = u.Id.ToString()
				}),
				Product = new Product()
			};

			if(id == null || id == 0)
			{
				// Create Functionality
				return View(productVM);
			}
			else
			{
				// Update Functionality
				productVM.Product = _unitOfWork.Product.Get(u => u.Id == id);
				return View(productVM);
			}
		}

		[HttpPost]
		public IActionResult Upsert(ProductVM productVM, IFormFile? file)
		{
			if(ModelState.IsValid) 
			{
				// Uplading image
				string wwwRootpath = _webHostEnvironment.WebRootPath;
				if(file != null) 
				{
					string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
					string productPath = Path.Combine(wwwRootpath, @"images\product");

					// While updating product, if product ImageURL is not empty then we will first delete that ImageURL and add new one
					// And for creating product as the ImageURL is empty it will simple add new one.

					if(!string.IsNullOrEmpty(productVM.Product.ImageURL))
					{
						var oldImagePath = Path.Combine(wwwRootpath, productVM.Product.ImageURL.TrimStart('\\'));

						if(System.IO.File.Exists(oldImagePath)) 
						{
							System.IO.File.Delete(oldImagePath);
						}
					}

					using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
					{
						file.CopyTo(fileStream);
					}
					productVM.Product.ImageURL = @"\images\product\" + fileName;
				}

				// Insert or Update Product
				if(productVM.Product.Id == 0)
				{
					_unitOfWork.Product.Add(productVM.Product);
				}
				else
				{
					_unitOfWork.Product.Update(productVM.Product);
				}
				
				_unitOfWork.Save();
				TempData["success"] = "Product added successfully!";
				return RedirectToAction("Index");
			}
			else
			{
				// If there are any validation errors, then it will redirect to the same page.
				// But the dropdown values are not populated that time and it will throw an exception.
				// So we use following code to populate the dropdown in case exception occurs.

				productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
				{
					Text = u.Name,
					Value = u.Id.ToString()
				});
				return View(productVM);
			}
		}

		// Edit product GET method
		//public IActionResult Edit(int? id)
		//{
		//	if(id == null || id == 0)
		//	{
		//		return NotFound();
		//	}
		//	Product? product = _unitOfWork.Product.Get(u => u.Id == id);

		//	if(product == null) 
		//	{
		//		return NotFound();
		//	}
		//	return View(product);
		//}

		////Edit product POST method
		//[HttpPost]
		//public IActionResult Edit(Product obj)
		//{
		//	if(ModelState.IsValid)
		//	{
		//		_unitOfWork.Product.Update(obj);
		//		_unitOfWork.Save();
		//		TempData["success"] = "Product updated successfully!";
		//		return RedirectToAction("Index");
		//	}
		//	return View();
		//}

		// Delete product GET method
		// Commented because later it added in API CALLS region below

		//public IActionResult Delete(int? id) 
		//{
		//	if(id == null || id == 0)
		//	{
		//		return NotFound();
		//	}
		//	Product product = _unitOfWork.Product.Get(u => u.Id == id);

		//	if (product == null)
		//	{
		//		return NotFound();
		//	}
		//	return View(product);
		//}

		// Delete product POST method
		//[HttpPost, ActionName("Delete")]
		//public IActionResult DeletePOST(int? id) 
		//{
		//	Product obj = _unitOfWork.Product.Get(u => u.Id == id);
		//	if(obj == null)
		//	{
		//		return NotFound();
		//	}
		//	_unitOfWork.Product.Remove(obj);
		//	_unitOfWork.Save();
		//	TempData["success"] = "Product deleted successfully!";
		//	return RedirectToAction("Index");
		//}


		#region API CALLS

		[HttpGet]
		public IActionResult GetAll()
		{
			List<Product> objProductList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
			return Json(new {data = objProductList});
		}

		[HttpDelete]
		public IActionResult Delete(int? id) 
		{
			var productToBeDeleted = _unitOfWork.Product.Get(u => u.Id == id);
			if(productToBeDeleted== null) 
			{
				return Json(new { success = false, message = "Error while deleting!" });
			}

			var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, productToBeDeleted.ImageURL.TrimStart('\\'));

			if (System.IO.File.Exists(oldImagePath))
			{
				System.IO.File.Delete(oldImagePath);
			}

			_unitOfWork.Product.Remove(productToBeDeleted);
			_unitOfWork.Save();

			return Json(new { success = true, message = "Product deleted successfully." });
		}

		#endregion
	}
}
