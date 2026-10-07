using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers;

[Authorize]
public class CustomerController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserScopeService _userScope;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomerController(
        ApplicationDbContext context,
        IUserScopeService userScope,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userScope = userScope;
        _auditService = auditService;
        _userManager = userManager;
    }

    // GET: Customer
    public async Task<IActionResult> Index(string? searchName, string? searchEmail, string? searchPhone, string? searchCompany, string? statusFilter)
    {
        var query = _userScope.FilterCustomers(_context.Customers.Include(c => c.AssignedUser).AsNoTracking());

        if (!string.IsNullOrWhiteSpace(searchName))
        {
            query = query.Where(c => c.CustomerName.Contains(searchName));
        }

        if (!string.IsNullOrWhiteSpace(searchEmail))
        {
            query = query.Where(c => c.Email.Contains(searchEmail));
        }

        if (!string.IsNullOrWhiteSpace(searchPhone))
        {
            query = query.Where(c => c.Phone.Contains(searchPhone));
        }

        if (!string.IsNullOrWhiteSpace(searchCompany))
        {
            query = query.Where(c => c.CompanyName != null && c.CompanyName.Contains(searchCompany));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(c => c.Status == statusFilter);
        }

        ViewData["SearchName"] = searchName;
        ViewData["SearchEmail"] = searchEmail;
        ViewData["SearchPhone"] = searchPhone;
        ViewData["SearchCompany"] = searchCompany;
        ViewData["StatusFilter"] = statusFilter;

        var customers = await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
        return View(customers);
    }

    // GET: Customer/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var customer = await _userScope.FilterCustomers(_context.Customers
            .Include(c => c.AssignedUser)
            .Include(c => c.Leads)
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
            .Include(c => c.Activities))
            .FirstOrDefaultAsync(m => m.CustomerId == id);

        if (customer == null) return NotFound();

        return View(customer);
    }

    // GET: Customer/Create
    public async Task<IActionResult> Create()
    {
        await PopulateUsersDropdown();
        var model = new Customer
        {
            CustomerCode = "CUST-" + (await _context.Customers.CountAsync() + 1001),
            Status = "Active",
            AssignedTo = _userScope.GetCurrentUserId()
        };
        return View(model);
    }

    // POST: Customer/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        // Server-Side Duplicate Validation
        var existingEmail = await _context.Customers.AnyAsync(c => c.Email.ToLower() == customer.Email.ToLower());
        if (existingEmail)
        {
            ModelState.AddModelError(nameof(customer.Email), "A customer with this email address already exists.");
        }

        var existingPhone = await _context.Customers.AnyAsync(c => c.Phone == customer.Phone);
        if (existingPhone)
        {
            ModelState.AddModelError(nameof(customer.Phone), "A customer with this phone number already exists.");
        }

        if (string.IsNullOrWhiteSpace(customer.CustomerCode))
        {
            customer.CustomerCode = "CUST-" + (await _context.Customers.CountAsync() + 1001);
        }

        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdown(customer.AssignedTo);
            return View(customer);
        }

        customer.CreatedDate = DateTime.UtcNow;
        customer.CreatedBy = User.Identity?.Name;

        if (string.IsNullOrEmpty(customer.AssignedTo))
        {
            customer.AssignedTo = _userScope.GetCurrentUserId();
        }

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Customer", customer.CustomerId.ToString(), null, customer.CustomerName, $"Customer {customer.CustomerName} ({customer.CustomerCode}) created");

        TempData["Success"] = "Customer created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Customer/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var customer = await _userScope.FilterCustomers(_context.Customers)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null) return NotFound();

        await PopulateUsersDropdown(customer.AssignedTo);
        return View(customer);
    }

    // POST: Customer/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer customer)
    {
        if (id != customer.CustomerId) return NotFound();

        var existing = await _userScope.FilterCustomers(_context.Customers)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (existing == null) return NotFound();

        // Duplicate checks for other records
        var emailTaken = await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Email.ToLower() == customer.Email.ToLower());
        if (emailTaken)
        {
            ModelState.AddModelError(nameof(customer.Email), "Another customer with this email address already exists.");
        }

        var phoneTaken = await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Phone == customer.Phone);
        if (phoneTaken)
        {
            ModelState.AddModelError(nameof(customer.Phone), "Another customer with this phone number already exists.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdown(customer.AssignedTo);
            return View(customer);
        }

        var oldValues = $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}";

        existing.CustomerName = customer.CustomerName;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.CompanyName = customer.CompanyName;
        existing.Address = customer.Address;
        existing.City = customer.City;
        existing.State = customer.State;
        existing.Status = customer.Status;
        if (_userScope.CanAccessAllRecords() && !string.IsNullOrEmpty(customer.AssignedTo))
        {
            existing.AssignedTo = customer.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newValues = $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}";
        await _auditService.LogAsync("Update", "Customer", id.ToString(), oldValues, newValues, $"Customer {existing.CustomerName} updated");

        TempData["Success"] = "Customer updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Customer/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var customer = await _userScope.FilterCustomers(_context.Customers.Include(c => c.AssignedUser))
            .FirstOrDefaultAsync(m => m.CustomerId == id);

        if (customer == null) return NotFound();

        return View(customer);
    }

    // POST: Customer/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var customer = await _userScope.FilterCustomers(_context.Customers)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer != null)
        {
            var customerName = customer.CustomerName;
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Delete", "Customer", id.ToString(), customerName, null, $"Customer {customerName} deleted");
            TempData["Success"] = "Customer deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateUsersDropdown(string? selectedId = null)
    {
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedId);
    }
}
