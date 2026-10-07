using Microsoft.AspNetCore.Identity;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles
        string[] roleNames = { "Admin", "Manager", "SalesExecutive" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. Seed Users
        var adminUser = await userManager.FindByEmailAsync("admin@acxiomcrm.com");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@acxiomcrm.com",
                Email = "admin@acxiomcrm.com",
                FullName = "System Administrator",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        var managerUser = await userManager.FindByEmailAsync("manager@acxiomcrm.com");
        if (managerUser == null)
        {
            managerUser = new ApplicationUser
            {
                UserName = "manager@acxiomcrm.com",
                Email = "manager@acxiomcrm.com",
                FullName = "Sarah Manager",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(managerUser, "Manager@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, "Manager");
            }
        }

        var salesUser = await userManager.FindByEmailAsync("sales@acxiomcrm.com");
        if (salesUser == null)
        {
            salesUser = new ApplicationUser
            {
                UserName = "sales@acxiomcrm.com",
                Email = "sales@acxiomcrm.com",
                FullName = "Alex Sales",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(salesUser, "Sales@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(salesUser, "SalesExecutive");
            }
        }

        var johnUser = await userManager.FindByEmailAsync("john@acxiomcrm.com");
        if (johnUser == null)
        {
            johnUser = new ApplicationUser
            {
                UserName = "john@acxiomcrm.com",
                Email = "john@acxiomcrm.com",
                FullName = "John Doe",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(johnUser, "Sales@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(johnUser, "SalesExecutive");
            }
        }

        // 3. Seed Customers
        if (!context.Customers.Any())
        {
            var customers = new List<Customer>
            {
                new Customer
                {
                    CustomerCode = "CUST-1001",
                    CustomerName = "Acme Technologies Pvt Ltd",
                    Email = "contact@acmetech.com",
                    Phone = "9876543210",
                    CompanyName = "Acme Technologies",
                    Address = "123 Business Bay, High St",
                    City = "Bengaluru",
                    State = "Karnataka",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-2),
                    CreatedBy = adminUser.UserName,
                    AssignedTo = salesUser.Id
                },
                new Customer
                {
                    CustomerCode = "CUST-1002",
                    CustomerName = "Global Logistics Solutions",
                    Email = "info@globallogistics.com",
                    Phone = "9876543211",
                    CompanyName = "Global Logistics",
                    Address = "45 Harbor View, Central Park",
                    City = "Mumbai",
                    State = "Maharashtra",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-1),
                    CreatedBy = managerUser.UserName,
                    AssignedTo = salesUser.Id
                },
                new Customer
                {
                    CustomerCode = "CUST-1003",
                    CustomerName = "Apex Financial Corp",
                    Email = "operations@apexfin.com",
                    Phone = "9876543212",
                    CompanyName = "Apex Financial",
                    Address = "78 Financial Tower, Ring Road",
                    City = "Hyderabad",
                    State = "Telangana",
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow.AddDays(-15),
                    CreatedBy = adminUser.UserName,
                    AssignedTo = johnUser.Id
                }
            };

            await context.Customers.AddRangeAsync(customers);
            await context.SaveChangesAsync();
        }

        // 4. Seed Leads
        if (!context.Leads.Any())
        {
            var cust1 = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1001");
            var leads = new List<Lead>
            {
                new Lead
                {
                    LeadCode = "LEAD-1001",
                    LeadName = "Vikram Sharma",
                    Email = "vikram@novainsights.com",
                    Phone = "9123456780",
                    CompanyName = "Nova Insights",
                    Source = "Website",
                    Status = "New",
                    Priority = "High",
                    ExpectedValue = 50000m,
                    CreatedDate = DateTime.UtcNow.AddDays(-10),
                    AssignedTo = salesUser.Id
                },
                new Lead
                {
                    LeadCode = "LEAD-1002",
                    LeadName = "Anita Rao",
                    Email = "anita@zenithcloud.io",
                    Phone = "9123456781",
                    CompanyName = "Zenith Cloud",
                    Source = "Referral",
                    Status = "Contacted",
                    Priority = "Medium",
                    ExpectedValue = 75000m,
                    CreatedDate = DateTime.UtcNow.AddDays(-8),
                    AssignedTo = salesUser.Id
                },
                new Lead
                {
                    LeadCode = "LEAD-1003",
                    LeadName = "Rohit Verma",
                    Email = "rohit@matrixai.com",
                    Phone = "9123456782",
                    CompanyName = "Matrix AI Systems",
                    Source = "Cold Call",
                    Status = "Qualified",
                    Priority = "High",
                    ExpectedValue = 120000m,
                    CreatedDate = DateTime.UtcNow.AddDays(-5),
                    AssignedTo = johnUser.Id
                },
                new Lead
                {
                    LeadCode = "LEAD-1004",
                    LeadName = "Pooja Patel",
                    Email = "pooja@bluecrest.com",
                    Phone = "9123456783",
                    CompanyName = "Blue Crest Holdings",
                    Source = "Social Media",
                    Status = "Converted",
                    Priority = "High",
                    ExpectedValue = 90000m,
                    CreatedDate = DateTime.UtcNow.AddDays(-20),
                    AssignedTo = salesUser.Id,
                    CustomerId = cust1?.CustomerId
                },
                new Lead
                {
                    LeadCode = "LEAD-1005",
                    LeadName = "David Chen",
                    Email = "david@chenenterprises.com",
                    Phone = "9123456784",
                    CompanyName = "Chen Enterprises",
                    Source = "Partner",
                    Status = "Lost",
                    Priority = "Low",
                    ExpectedValue = 30000m,
                    CreatedDate = DateTime.UtcNow.AddDays(-25),
                    AssignedTo = johnUser.Id
                }
            };

            await context.Leads.AddRangeAsync(leads);
            await context.SaveChangesAsync();
        }

        // 5. Seed Opportunities
        if (!context.Opportunities.Any())
        {
            var cust1 = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1001");
            var cust2 = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1002");
            var cust3 = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1003");
            var lead3 = context.Leads.FirstOrDefault(l => l.LeadCode == "LEAD-1003");

            var opportunities = new List<Opportunity>
            {
                new Opportunity
                {
                    OpportunityName = "Cloud Migration Enterprise Plan",
                    CustomerId = cust1?.CustomerId,
                    Amount = 150000m,
                    Stage = "Proposal",
                    Probability = 60,
                    ExpectedCloseDate = DateTime.Today.AddDays(20),
                    Status = "Open",
                    Notes = "Client reviewing technical proposal and pricing",
                    CreatedDate = DateTime.UtcNow.AddDays(-15),
                    AssignedTo = salesUser.Id
                },
                new Opportunity
                {
                    OpportunityName = "ERP Integration Module",
                    CustomerId = cust2?.CustomerId,
                    Amount = 85000m,
                    Stage = "Negotiation",
                    Probability = 80,
                    ExpectedCloseDate = DateTime.Today.AddDays(10),
                    Status = "Open",
                    Notes = "Final contract negotiation with procurement team",
                    CreatedDate = DateTime.UtcNow.AddDays(-12),
                    AssignedTo = salesUser.Id
                },
                new Opportunity
                {
                    OpportunityName = "Security Audit & Compliance Package",
                    CustomerId = cust3?.CustomerId,
                    LeadId = lead3?.LeadId,
                    Amount = 45000m,
                    Stage = "Qualification",
                    Probability = 30,
                    ExpectedCloseDate = DateTime.Today.AddDays(45),
                    Status = "Open",
                    Notes = "Initial requirements gathering scheduled",
                    CreatedDate = DateTime.UtcNow.AddDays(-3),
                    AssignedTo = johnUser.Id
                },
                new Opportunity
                {
                    OpportunityName = "Annual Support Retainer 2026",
                    CustomerId = cust1?.CustomerId,
                    Amount = 120000m,
                    Stage = "Won",
                    Probability = 100,
                    ExpectedCloseDate = DateTime.Today.AddDays(-5),
                    Status = "Won",
                    Notes = "Contract signed and purchase order received",
                    CreatedDate = DateTime.UtcNow.AddDays(-40),
                    AssignedTo = salesUser.Id
                },
                new Opportunity
                {
                    OpportunityName = "Custom Mobile Application",
                    CustomerId = cust2?.CustomerId,
                    Amount = 60000m,
                    Stage = "Lost",
                    Probability = 0,
                    ExpectedCloseDate = DateTime.Today.AddDays(-10),
                    Status = "Lost",
                    Notes = "Client decided to build in-house",
                    CreatedDate = DateTime.UtcNow.AddDays(-30),
                    AssignedTo = johnUser.Id
                }
            };

            await context.Opportunities.AddRangeAsync(opportunities);
            await context.SaveChangesAsync();
        }

        // 6. Seed FollowUps
        if (!context.FollowUps.Any())
        {
            var cust1 = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1001");
            var lead1 = context.Leads.FirstOrDefault(l => l.LeadCode == "LEAD-1001");
            var opp1 = context.Opportunities.FirstOrDefault();

            var followUps = new List<FollowUp>
            {
                new FollowUp
                {
                    CustomerId = cust1?.CustomerId,
                    RelatedType = "Customer",
                    RelatedId = cust1?.CustomerId,
                    Subject = "Quarterly Business Review Discussion",
                    FollowUpDate = DateTime.Today.AddDays(3),
                    FollowUpType = "Meeting",
                    Status = "Planned",
                    Remarks = "Prepare presentation slide deck on usage stats",
                    AssignedTo = salesUser.Id,
                    CreatedDate = DateTime.UtcNow
                },
                new FollowUp
                {
                    LeadId = lead1?.LeadId,
                    RelatedType = "Lead",
                    RelatedId = lead1?.LeadId,
                    Subject = "Demo Call for Nova Insights",
                    FollowUpDate = DateTime.Today.AddDays(1),
                    FollowUpType = "Call",
                    Status = "Planned",
                    Remarks = "Walk through the platform features and architecture",
                    AssignedTo = salesUser.Id,
                    CreatedDate = DateTime.UtcNow
                },
                new FollowUp
                {
                    OpportunityId = opp1?.OpportunityId,
                    RelatedType = "Opportunity",
                    RelatedId = opp1?.OpportunityId,
                    Subject = "Send Proposal Follow-up Email",
                    FollowUpDate = DateTime.Today.AddDays(5),
                    FollowUpType = "Email",
                    Status = "Planned",
                    Remarks = "Check with decision maker on approval status",
                    AssignedTo = salesUser.Id,
                    CreatedDate = DateTime.UtcNow
                }
            };

            await context.FollowUps.AddRangeAsync(followUps);
            await context.SaveChangesAsync();
        }

        // 7. Seed Activities
        if (!context.Activities.Any())
        {
            var cust1 = context.Customers.FirstOrDefault(c => c.CustomerCode == "CUST-1001");
            var lead1 = context.Leads.FirstOrDefault(l => l.LeadCode == "LEAD-1001");

            var activities = new List<Activity>
            {
                new Activity
                {
                    ActivityType = "Meeting",
                    Subject = "Introductory Discovery Call",
                    Description = "Met with executive team to understand CRM requirements and pain points",
                    ActivityDate = DateTime.Today.AddDays(-5),
                    CustomerId = cust1?.CustomerId,
                    AssignedTo = salesUser.Id,
                    Status = "Completed",
                    CreatedDate = DateTime.UtcNow.AddDays(-5)
                },
                new Activity
                {
                    ActivityType = "Email",
                    Subject = "Sent Product Brochure and Datasheet",
                    Description = "Shared overview documentation and pricing tier structures",
                    ActivityDate = DateTime.Today.AddDays(-3),
                    LeadId = lead1?.LeadId,
                    AssignedTo = salesUser.Id,
                    Status = "Completed",
                    CreatedDate = DateTime.UtcNow.AddDays(-3)
                }
            };

            await context.Activities.AddRangeAsync(activities);
            await context.SaveChangesAsync();
        }

        // 8. Seed Initial Audit Logs
        if (!context.AuditLogs.Any())
        {
            var auditLogs = new List<AuditLog>
            {
                new AuditLog
                {
                    UserId = adminUser.Id,
                    UserName = adminUser.UserName,
                    Action = "Create",
                    EntityName = "Customer",
                    RecordId = "CUST-1001",
                    NewValue = "Acme Technologies Pvt Ltd",
                    Details = "Initial customer record created during onboarding",
                    CreatedDate = DateTime.UtcNow.AddMonths(-2),
                    IpAddress = "127.0.0.1"
                },
                new AuditLog
                {
                    UserId = salesUser.Id,
                    UserName = salesUser.UserName,
                    Action = "Convert",
                    EntityName = "Lead",
                    RecordId = "LEAD-1004",
                    OldValue = "Status: Qualified",
                    NewValue = "Status: Converted",
                    Details = "Lead converted to customer successfully",
                    CreatedDate = DateTime.UtcNow.AddDays(-20),
                    IpAddress = "127.0.0.1"
                }
            };

            await context.AuditLogs.AddRangeAsync(auditLogs);
            await context.SaveChangesAsync();
        }
    }
}
