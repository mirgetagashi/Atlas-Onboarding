using System.Text.Json;

namespace Atlas.Onboarding.UseCases.Shared;

/// <summary>Converts enums to and from the API names, e.g. IdentityDocument and "IDENTITY_DOCUMENT".</summary>
public static class ApiNames
{
    public static string Of<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        JsonNamingPolicy.SnakeCaseUpper.ConvertName(value.ToString());

    public static bool TryParse<TEnum>(string? raw, out TEnum value)
        where TEnum : struct, Enum
    {
        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            if (string.Equals(Of(candidate), raw, StringComparison.OrdinalIgnoreCase))
            {
                value = candidate;
                return true;
            }
        }

        value = default;
        return false;
    }
}
