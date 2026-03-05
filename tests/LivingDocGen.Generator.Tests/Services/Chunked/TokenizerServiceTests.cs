using System.Collections.Generic;
using Xunit;
using LivingDocGen.Generator.Services.Chunked;

namespace LivingDocGen.Generator.Tests.Services.Chunked;

public class TokenizerServiceTests
{
    private readonly TokenizerService _sut = new TokenizerService();

    [Fact]
    public void Tokenize_NullOrEmpty_ReturnsEmptyList()
    {
        Assert.Empty(_sut.Tokenize(null));
        Assert.Empty(_sut.Tokenize(string.Empty));
        Assert.Empty(_sut.Tokenize("   "));
    }

    [Fact]
    public void Tokenize_SimpleText_ReturnsLowercaseTokens()
    {
        var tokens = _sut.Tokenize("User Login Feature");
        Assert.Contains("user", tokens);
        Assert.Contains("login", tokens);
        Assert.Contains("feature", tokens);
    }

    [Fact]
    public void Tokenize_AppliesCaseFolding()
    {
        var tokens = _sut.Tokenize("ShoppingCart AddItem");
        Assert.Contains("shoppingcart", tokens);
        Assert.Contains("additem", tokens);
    }

    [Fact]
    public void Tokenize_SplitsOnDelimiters()
    {
        var tokens = _sut.Tokenize("user-login_feature/path.test@tag#anchor");
        Assert.Contains("user", tokens);
        Assert.Contains("login", tokens);
        Assert.Contains("feature", tokens);
        Assert.Contains("path", tokens);
        Assert.Contains("test", tokens);
        Assert.Contains("tag", tokens);
        Assert.Contains("anchor", tokens);
    }

    [Fact]
    public void Tokenize_FiltersShortTokens()
    {
        // Single-char tokens should be excluded (min length = 2)
        var tokens = _sut.Tokenize("a b cd ef");
        Assert.DoesNotContain("a", tokens);
        Assert.DoesNotContain("b", tokens);
        Assert.Contains("cd", tokens);
        Assert.Contains("ef", tokens);
    }

    [Fact]
    public void Tokenize_DeduplicatesTokens()
    {
        var tokens = _sut.Tokenize("login Login LOGIN");
        Assert.Single(tokens);
        Assert.Equal("login", tokens[0]);
    }

    [Fact]
    public void Tokenize_ReturnsSortedTokens()
    {
        var tokens = _sut.Tokenize("zoo alpha middle");
        Assert.Equal(new List<string> { "alpha", "middle", "zoo" }, tokens);
    }

    [Fact]
    public void Tokenize_HandlesSpecialCharacters()
    {
        var tokens = _sut.Tokenize("Scenario: User (admin) [role]");
        Assert.Contains("scenario", tokens);
        Assert.Contains("user", tokens);
        Assert.Contains("admin", tokens);
        Assert.Contains("role", tokens);
    }

    [Fact]
    public void TokenizeMultiple_MergesTokensFromMultipleTexts()
    {
        var tokens = _sut.TokenizeMultiple(new[] { "User Login", "Cart Checkout" });
        Assert.Contains("user", tokens);
        Assert.Contains("login", tokens);
        Assert.Contains("cart", tokens);
        Assert.Contains("checkout", tokens);
    }

    [Fact]
    public void TokenizeMultiple_DeduplicatesAcrossTexts()
    {
        var tokens = _sut.TokenizeMultiple(new[] { "User Login", "Login Feature" });
        // "login" should appear only once
        Assert.Equal(1, tokens.FindAll(t => t == "login").Count);
    }

    [Fact]
    public void TokenizeMultiple_NullInput_ReturnsEmpty()
    {
        Assert.Empty(_sut.TokenizeMultiple(null));
    }

    [Fact]
    public void TokenizeMultiple_EmptyEnumerable_ReturnsEmpty()
    {
        Assert.Empty(_sut.TokenizeMultiple(new List<string>()));
    }

    [Fact]
    public void TokenizeMultiple_ReturnsSortedTokens()
    {
        var tokens = _sut.TokenizeMultiple(new[] { "Zoo Feature", "Alpha Scenario" });
        for (int i = 1; i < tokens.Count; i++)
        {
            Assert.True(string.Compare(tokens[i - 1], tokens[i], System.StringComparison.Ordinal) <= 0,
                $"Token '{tokens[i - 1]}' should come before '{tokens[i]}'");
        }
    }
}
