using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/customers")]
public class CustomersApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CustomersApiController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET /api/customers
    [HttpGet]
    public async Task<IActionResult> GetCustomers([FromQuery] string? search)
    {
        var query = _context.Customers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.CustomerName.Contains(search) || c.Email.Contains(search) || c.Phone.Contains(search));
        }

        var list = await query.Select(c => new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            CreatedDate = c.CreatedDate,
            CreatedBy = c.CreatedBy,
            AssignedTo = c.AssignedTo
        }).ToListAsync();

        return Ok(list);
    }

    // GET /api/customers/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomer(int id)
    {
        var c = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerId == id);
        if (c == null)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = $"Customer with ID {id} not found." });
        }

        var dto = new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            CreatedDate = c.CreatedDate,
            CreatedBy = c.CreatedBy,
            AssignedTo = c.AssignedTo
        };

        return Ok(dto);
    }

    // POST /api/customers
    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == dto.Email.ToLower()))
        {
            return Conflict(new ApiResponse<object> { Success = false, Message = "Customer with this email already exists." });
        }

        if (await _context.Customers.AnyAsync(c => c.Phone == dto.Phone))
        {
            return Conflict(new ApiResponse<object> { Success = false, Message = "Customer with this phone number already exists." });
        }

        var customer = new Customer
        {
            CustomerCode = "CUST-" + (await _context.Customers.CountAsync() + 1001),
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Status = dto.Status,
            AssignedTo = dto.AssignedTo,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name ?? "API"
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Customer (API)", customer.CustomerId.ToString(), null, customer.CustomerName, $"Customer {customer.CustomerName} created via REST API");

        var resultDto = new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            CreatedDate = customer.CreatedDate,
            CreatedBy = customer.CreatedBy,
            AssignedTo = customer.AssignedTo
        };

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, resultDto);
    }

    // PUT /api/customers/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = $"Customer with ID {id} not found." });
        }

        if (await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Email.ToLower() == dto.Email.ToLower()))
        {
            return Conflict(new ApiResponse<object> { Success = false, Message = "Another customer with this email already exists." });
        }

        if (await _context.Customers.AnyAsync(c => c.CustomerId != id && c.Phone == dto.Phone))
        {
            return Conflict(new ApiResponse<object> { Success = false, Message = "Another customer with this phone number already exists." });
        }

        var oldValues = $"Name: {customer.CustomerName}, Email: {customer.Email}, Status: {customer.Status}";

        customer.CustomerName = dto.CustomerName;
        customer.Email = dto.Email;
        customer.Phone = dto.Phone;
        customer.CompanyName = dto.CompanyName;
        customer.Address = dto.Address;
        customer.City = dto.City;
        customer.State = dto.State;
        customer.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.AssignedTo)) customer.AssignedTo = dto.AssignedTo;

        await _context.SaveChangesAsync();

        var newValues = $"Name: {customer.CustomerName}, Email: {customer.Email}, Status: {customer.Status}";
        await _auditService.LogAsync("Update", "Customer (API)", id.ToString(), oldValues, newValues, $"Customer {customer.CustomerName} updated via REST API");

        return Ok(new ApiResponse<object> { Success = true, Message = "Customer updated successfully." });
    }

    // DELETE /api/customers/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = $"Customer with ID {id} not found." });
        }

        var customerName = customer.CustomerName;
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Delete", "Customer (API)", id.ToString(), customerName, null, $"Customer {customerName} deleted via REST API");

        return Ok(new ApiResponse<object> { Success = true, Message = "Customer deleted successfully." });
    }
}
