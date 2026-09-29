using Microsoft.Extensions.Configuration;

namespace Atlas.Markets.Tests;

/// <summary>Tests the real markets.json, so a wrong edit to the configuration fails the build.</summary>
public sealed class MarketRulesTests
{
    private static readonly IMarketCatalog Catalog = MarketCatalog.FromConfiguration(
        new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "markets.json"))
            .Build());

    private static MarketDefinition Market(string code)
    {
        Assert.True(Catalog.TryGet(MarketCode.Parse(code), out var market));
        return market;
    }

    [Fact]
    public void All_six_markets_are_configured()
    {
        var codes = Catalog.All.Select(m => m.Code.Value).OrderBy(c => c);
        Assert.Equal(new[] { "MA", "MB", "MC", "MD", "ME", "MF" }, codes);
    }

    [Fact]
    public void Only_MD_requires_activation_in_a_branch()
    {
        var inBranch = Catalog.All.Where(m => m.Activation == ActivationMode.InBranch).Select(m => m.Code.Value);
        Assert.Equal(new[] { "MD" }, inBranch);
    }

    [Theory]
    [InlineData("MA", "0403991450016")]
    [InlineData("MB", "0403991450016")]
    [InlineData("MB", "0403 991-450016")] // spaces and dashes are ignored
    [InlineData("MC", "12345678A")]
    [InlineData("MC", "ab34567cz")] // lower case is normalized
    [InlineData("MD", "1234567890")]
    [InlineData("ME", "1234567890")]
    [InlineData("MF", "AB12345")]
    public void Valid_national_ids_are_accepted(string market, string value)
    {
        var check = IdentifierValidator.Validate(Market(market), IdentifierType.NationalId, value);
        Assert.True(check.IsValid, check.Error);
    }

    [Theory]
    [InlineData("MB", "040399145001")] // 12 digits
    [InlineData("MB", "04039914500160")] // 14 digits
    [InlineData("MB", "04039914500AB")]
    [InlineData("MC", "123456789")] // position 9 must be a letter
    [InlineData("MC", "1234567A")] // too short
    [InlineData("MD", "123456789")]
    [InlineData("ME", "12345678901")]
    [InlineData("MA", "")]
    public void Invalid_national_ids_are_rejected(string market, string value)
    {
        var check = IdentifierValidator.Validate(Market(market), IdentifierType.NationalId, value);
        Assert.False(check.IsValid);
    }

    [Fact]
    public void MF_accepts_a_passport_for_residents_without_a_personal_number()
    {
        var check = IdentifierValidator.Validate(Market("MF"), IdentifierType.Passport, "p1234567");
        Assert.True(check.IsValid, check.Error);
        Assert.Equal("P1234567", check.NormalizedValue);
    }

    [Theory]
    [InlineData("MA")]
    [InlineData("MB")]
    [InlineData("MC")]
    [InlineData("MD")]
    [InlineData("ME")]
    public void Other_markets_do_not_accept_a_passport_instead_of_a_national_id(string market)
    {
        var check = IdentifierValidator.Validate(Market(market), IdentifierType.Passport, "P1234567");
        Assert.False(check.IsValid);
    }

    [Theory]
    [InlineData("mb", "MB")]
    [InlineData(" MD ", "MD")]
    public void Market_codes_are_normalized(string input, string expected)
    {
        Assert.True(MarketCode.TryParse(input, out var code));
        Assert.Equal(expected, code.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("M1")]
    [InlineData("MBX")]
    public void Malformed_market_codes_are_refused(string? input) =>
        Assert.False(MarketCode.TryParse(input, out _));
}
