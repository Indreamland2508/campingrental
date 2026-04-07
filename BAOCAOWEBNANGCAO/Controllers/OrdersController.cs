using BAOCAOWEBNANGCAO.Data;
using BAOCAOWEBNANGCAO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BAOCAOWEBNANGCAO.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class OrdersController : Controller
    {
        private readonly CampingDbContext _context;

        public OrdersController(CampingDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách sắp xếp đơn mới nhất lên đầu
            return View(await _context.Orders.OrderByDescending(o => o.OrderDate).ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // --- BỎ QUA CREATE VÌ ĐƠN HÀNG DO KHÁCH TẠO TỪ GIỎ HÀNG CHỨ ADMIN KHÔNG TỰ TẠO ---

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OrderDate,CustomerName,CustomerPhone,CustomerEmail,ShippingAddress,Note,RentalStartDate,RentalEndDate,TotalAmount,DepositAmount,RemainingAmount,PaymentStatus,Status")] Order order)
        {
            if (id != order.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        // GET & POST Delete (Giữ nguyên của Châu)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders.FirstOrDefaultAsync(m => m.Id == id);
            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null) _context.Orders.Remove(order);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }

        // --- CẬP NHẬT TRẠNG THÁI GIAO HÀNG & THANH TOÁN ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string paymentStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            // Cập nhật trạng thái Giao hàng (Ví dụ: Đang giao, Đã trả đồ)
            if (!string.IsNullOrEmpty(status))
            {
                order.Status = status;
            }

            // Cập nhật trạng thái Tiền nong (Ví dụ: Đã nhận cọc, Đã thanh toán nốt)
            if (!string.IsNullOrEmpty(paymentStatus))
            {
                order.PaymentStatus = paymentStatus;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}