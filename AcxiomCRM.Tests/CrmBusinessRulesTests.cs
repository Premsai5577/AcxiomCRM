using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Xunit;

namespace AcxiomCRM.Tests;

public class CrmBusinessRulesTests
{
    private IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void Opportunity_AmountZeroOrNegative_FailsValidation()
    {
        var opp = new Opportunity
        {
            OpportunityName = "Test Opp",
            Amount = 0m,
            Probability = 50,
            ExpectedCloseDate = DateTime.Today.AddDays(10),
            Stage = "Proposal"
        };

        var results = ValidateModel(opp);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Opportunity Amount must be greater than 0"));
    }

    [Fact]
    public void Opportunity_ProbabilityOutOfRange_FailsValidation()
    {
        var oppHigh = new Opportunity
        {
            OpportunityName = "High Prob Opp",
            Amount = 1000m,
            Probability = 101,
            ExpectedCloseDate = DateTime.Today.AddDays(10),
            Stage = "Proposal"
        };

        var resultsHigh = ValidateModel(oppHigh);
        Assert.Contains(resultsHigh, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Probability must be between 0 and 100"));

        var oppLow = new Opportunity
        {
            OpportunityName = "Low Prob Opp",
            Amount = 1000m,
            Probability = -5,
            ExpectedCloseDate = DateTime.Today.AddDays(10),
            Stage = "Proposal"
        };

        var resultsLow = ValidateModel(oppLow);
        Assert.Contains(resultsLow, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Probability must be between 0 and 100"));
    }

    [Fact]
    public void Opportunity_ExpectedCloseDateInPast_FailsValidationForActiveOpportunity()
    {
        var opp = new Opportunity
        {
            OpportunityName = "Past Close Opp",
            Amount = 50000m,
            Probability = 50,
            ExpectedCloseDate = DateTime.Today.AddDays(-1),
            Stage = "Proposal"
        };

        var results = ValidateModel(opp);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Expected Close Date cannot be in the past"));
    }

    [Fact]
    public void Opportunity_WeightedPipeline_IsCalculatedCorrectly()
    {
        var opp = new Opportunity
        {
            OpportunityName = "Weighted Test",
            Amount = 100000m,
            Probability = 40,
            ExpectedCloseDate = DateTime.Today.AddDays(15),
            Stage = "Proposal"
        };

        Assert.Equal(40000m, opp.WeightedPipeline);
    }

    [Fact]
    public void FollowUp_DateEarlierThanToday_FailsValidationForPlannedStatus()
    {
        var followUp = new FollowUp
        {
            Subject = "Catch up call",
            FollowUpType = "Call",
            Status = "Planned",
            FollowUpDate = DateTime.Today.AddDays(-1)
        };

        var results = ValidateModel(followUp);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Follow-up date cannot be earlier than today"));
    }

    [Fact]
    public void FollowUp_TodayOrFutureDate_PassesValidation()
    {
        var followUp = new FollowUp
        {
            Subject = "Future Meeting",
            FollowUpType = "Meeting",
            Status = "Planned",
            FollowUpDate = DateTime.Today.AddDays(5)
        };

        var results = ValidateModel(followUp);
        Assert.DoesNotContain(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Follow-up date cannot be earlier than today"));
    }

    [Fact]
    public void Customer_InvalidPhone_FailsValidation()
    {
        var customer = new Customer
        {
            CustomerName = "Valid Corp",
            Email = "valid@corp.com",
            Phone = "12345", // not 10 digits
            Status = "Active"
        };

        var results = ValidateModel(customer);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("10-digit number"));
    }

    [Fact]
    public void Customer_InvalidEmail_FailsValidation()
    {
        var customer = new Customer
        {
            CustomerName = "Valid Corp",
            Email = "invalid-email-string",
            Phone = "9876543210",
            Status = "Active"
        };

        var results = ValidateModel(customer);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("valid email address"));
    }

    [Fact]
    public void Customer_ValidFields_PassesValidation()
    {
        var customer = new Customer
        {
            CustomerName = "Acme Global",
            Email = "sales@acmeglobal.com",
            Phone = "9876543210",
            Status = "Active"
        };

        var results = ValidateModel(customer);
        Assert.Empty(results);
    }
}
