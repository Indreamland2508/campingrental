using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using Microsoft.AspNetCore.Authorization;
using System.Net;
namespace BAOCAOWEBNANGCAO.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class ProductsController : Controller
    {
        private readonly CampingDbContext _context;

        public ProductsController(CampingDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index(string search, int page = 1)
        {
            int pageSize = 5;
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            int totalItems = await query.CountAsync();
            var products = await query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Search = search;
            ViewBag.TotalItems = totalItems;

            return View(products);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,PricePerDay,Quantity,CategoryId")] Product product, IFormFile? ImageFile)
        {
            if (ImageFile != null && ImageFile.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                product.ImageUrl = "/images/products/" + fileName;
            }

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Products/Edit/5
        [HttpGet] // Đảm bảo hàm này nhận GET để hiện giao diện
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,PricePerDay,Quantity,CategoryId,ImageUrl")] Product product, IFormFile? imageFile)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        string filePath = Path.Combine(uploadsFolder, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }
                        // Cập nhật đường dẫn ảnh mới
                        product.ImageUrl = "/images/products/" + fileName;
                    }
                    // Nếu imageFile null, ImageUrl cũ từ hidden input trong View sẽ được giữ lại nhờ Bind

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi lưu: " + ex.Message);
                }
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}