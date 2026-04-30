using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace LogSystem.Core.Services.Database;

public class ExtractionStyleRegex : ExtractionStyle
{
    private readonly ConcurrentDictionary<string, Regex?> _regexCache = new();
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);
    private static readonly RegexOptions RegexCompilationOptions = RegexOptions.Compiled | RegexOptions.Singleline;

    public ExtractionStyleRegex() : base("regex")
    {
    }

    /// <summary>
    /// Extracts a value from text content using a regular expression.
    /// Returns the entire match of the first occurrence.
    /// Supports look-behind and look-ahead assertions.
    /// </summary>
    /// <param name="content">Text content to search</param>
    /// <param name="expression">Regular expression pattern</param>
    /// <returns>Extracted match as string or null if no match found or regex invalid</returns>
    public object? Extract(string content, string expression)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(expression))
            return null;

        // Get or compile the regex from cache
        var regex = GetOrCompileRegex(expression);

        if (regex == null)
            return null;

        try
        {
            var match = regex.Match(content);

            if (!match.Success)
                return null;

            return match.Value;
        }
        catch (RegexMatchTimeoutException)
        {
            // Timeout occurred - regex took too long to execute
            // This prevents ReDoS (Regular Expression Denial of Service) attacks
            return null;
        }
        catch (Exception)
        {
            // Unexpected error during regex extraction
            return null;
        }
    }

    /// <summary>
    /// Gets a compiled regex from cache or compiles and caches a new one.
    /// Returns null if the regex pattern is invalid.
    /// </summary>
    private Regex? GetOrCompileRegex(string expression)
    {
        return _regexCache.GetOrAdd(expression, pattern =>
        {
            try
            {
                return new Regex(pattern, RegexCompilationOptions, RegexTimeout);
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern - will be cached as null
                return null;
            }
        });
    }

    /// <summary>
    /// Validates if a regex expression is valid by attempting to compile it.
    /// Used by endpoints to validate user input before persisting.
    /// </summary>
    /// <param name="expression">Regular expression pattern to validate</param>
    /// <returns>True if the regex is valid, false otherwise</returns>
    public bool IsValidExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        try
        {
            _ = new Regex(expression, RegexCompilationOptions, RegexTimeout);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
