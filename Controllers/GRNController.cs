using CityMarketPOS.Models;
using CityMarketPOS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CityMarketPOS.Controllers
{
    [Authorize(Roles = "Manager,Inventory")]
    public class GRNController : Controller
    {
        private readonly IGRNRepository _grnRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogRepository _auditLogRepo;

        public GRNController(IGRNRepository grnRepo, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IAuditLogRepository auditLogRepo)
        {
            _grnRepo = grnRepo;
            _userManager = userManager;
            _context = context;
            _auditLogRepo = auditLogRepo;
        }

        public async Task<IActionResult> Index(string poNumber, DateTime? receivedDate)
        {
            var query = _context.GRNs
                .Include(g => g.PurchaseOrder)
                .Include(g => g.GRNDetails)
                .AsQueryable();

            if (!string.IsNullOrEmpty(poNumber))
            {
                query = query.Where(g => g.PurchaseOrder != null && g.PurchaseOrder.PONumber.Contains(poNumber));
            }

            if (receivedDate.HasValue)
            {
                query = query.Where(g => g.ReceivedDate.Date == receivedDate.Value.Date);
            }

            var grns = await query.ToListAsync();
            return View(grns);
        }

        public async Task<IActionResult> Details(int id)
        {
            var grn = await _grnRepo.GetByIdAsync(id);
            if (grn == null) return NotFound();
            return View(grn);
        }

        public async Task<IActionResult> DetailsByPO(int poId)
        {
            var grns = await _grnRepo.GetByPurchaseOrderIdAsync(poId);

            if (grns == null || !grns.Any())
                return NotFound("No GRN found for this PO.");

            if (grns.Count() == 1)
            {
                return RedirectToAction("Details", new { id = grns.First().Id });
            }

            ViewBag.PageTitle = "GRN History For " + grns.First().PurchaseOrder?.PONumber;

            return View("Index", grns);
        }

        public async Task<IActionResult> Create(int? poId)
        {
            if (poId == null) return RedirectToAction("Index", "PurchaseOrder");

            var po = await _grnRepo.GetPurchaseOrderWithDetailsAsync(poId.Value);
            if (po == null) return NotFound();

            var receivedQtys = await _grnRepo.GetReceivedQuantitiesByPOAsync(poId.Value);
            ViewBag.ReceivedQuantities = receivedQtys;

            return View(po);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int poId, List<int> ProductId, List<int> ReceivedQty, List<DateTime?> ExpiryDate, List<decimal> SellingPrice, string Remarks)
        {
            string finalRemarks = string.IsNullOrWhiteSpace(Remarks) ? "-" : Remarks;

            var poExists = await _context.PurchaseOrders.AnyAsync(p => p.Id == poId);

            if (!poExists) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            string userId = user?.Id ?? "System";

            var grn = new GRN
            {
                GRNNumber = "GRN-" + DateTime.Now.ToString("yyMMddHHmm"),
                PurchaseOrderId = poId,
                Remarks = finalRemarks,
                ReceivedByUserId = userId
            };

            // Updated Repository Call: removed code inputs
            await _grnRepo.ConfirmGRNAndUpdateStockAsync(grn, ProductId, ReceivedQty, ExpiryDate, SellingPrice, poId);

            await _auditLogRepo.LogAsync("GRN", grn.Id.ToString(), "Create", userId, user?.UserName ?? "System", $"Created GRN: {grn.GRNNumber} for PO ID: {poId} with {ProductId.Count} items");

            return RedirectToAction(nameof(Index));
        }
    }
}