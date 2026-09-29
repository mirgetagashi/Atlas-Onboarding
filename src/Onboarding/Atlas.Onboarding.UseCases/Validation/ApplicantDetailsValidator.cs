using System.Net.Mail;
using System.Text.RegularExpressions;
using Atlas.Markets;
using Atlas.Onboarding.Domain;
using Atlas.Onboarding.UseCases.Dtos.Requests;

namespace Atlas.Onboarding.UseCases.Validation;

public sealed record DetailsValidationResult(
    Dictionary<string, string[]> Errors,
    PersonalDetails? Details,
    PersonalIdentifier? Identifier)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// Checks the applicant's details against the market's rules and, if valid,
/// returns clean domain value objects. The domain never sees unvalidated input.
/// </summary>
public static class ApplicantDetailsValidator
{
    private const int MaxNameLength = 100;

    // E.164: "+" then 7 to 15 digits, first digit not zero.
    private static readonly Regex PhonePattern = new("^\\+[1-9][0-9]{6,14}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public static DetailsValidationResult Validate(ApplicantDetailsRequest request, MarketDefinition market, DateOnly today, int minimumAge)
    {
        var errors = new Dictionary<string, string[]>();

        var firstName = request.FirstName?.Trim();
        var lastName = request.LastName?.Trim();
        var email = request.Email?.Trim();
        var phone = request.Phone?.Replace(" ", string.Empty);

        RequireName(errors, "firstName", firstName);
        RequireName(errors, "lastName", lastName);

        if (request.DateOfBirth is not { } dateOfBirth)
        {
            errors["dateOfBirth"] = new[] { "Date of birth is required." };
        }
        else if (dateOfBirth > today)
        {
            errors["dateOfBirth"] = new[] { "Date of birth cannot be in the future." };
        }
        else if (AgeOn(today, dateOfBirth) < minimumAge)
        {
            errors["dateOfBirth"] = new[] { $"Applicant must be at least {minimumAge} years old." };
        }

        if (string.IsNullOrEmpty(email) || email.Length > 254 || !MailAddress.TryCreate(email, out _))
        {
            errors["email"] = new[] { "A valid email address is required." };
        }

        if (string.IsNullOrEmpty(phone) || !PhonePattern.IsMatch(phone))
        {
            errors["phone"] = new[] { "Phone must be in international format, e.g. +38344123456." };
        }

        string? identifierValue = null;
        if (request.Identifier?.Type is not { } identifierType)
        {
            var accepted = string.Join(", ", market.AcceptedIdentifiers.Select(i => i.Type));
            errors["identifier.type"] = new[] { $"Identifier type is required. Accepted in {market.Code}: {accepted}." };
        }
        else
        {
            var check = IdentifierValidator.Validate(market, identifierType, request.Identifier.Value);
            if (check.IsValid)
            {
                identifierValue = check.NormalizedValue;
            }
            else
            {
                errors["identifier.value"] = new[] { check.Error! };
            }
        }

        if (errors.Count > 0)
        {
            return new DetailsValidationResult(errors, null, null);
        }

        return new DetailsValidationResult(
            errors,
            new PersonalDetails(firstName!, lastName!, request.DateOfBirth!.Value, email!, phone!),
            new PersonalIdentifier(request.Identifier!.Type!.Value, identifierValue!));
    }

    private static void RequireName(Dictionary<string, string[]> errors, string field, string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaxNameLength)
        {
            errors[field] = new[] { $"Required, at most {MaxNameLength} characters." };
        }
    }

    private static int AgeOn(DateOnly today, DateOnly dateOfBirth)
    {
        var age = today.Year - dateOfBirth.Year;
        return dateOfBirth > today.AddYears(-age) ? age - 1 : age;
    }
}
