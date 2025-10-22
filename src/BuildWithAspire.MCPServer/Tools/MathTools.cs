using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace BuildWithAspire.MCPServer.Tools;

/// <summary>
/// Mathematical calculation tools for MCP clients.
/// Provides various mathematical operations and utilities.
/// </summary>
[McpServerToolType]
public sealed class MathTools
{
    private readonly ILogger<MathTools> _logger;

    public MathTools(ILogger<MathTools> logger)
    {
        _logger = logger;
    }

    [McpServerTool(Name = "calculate")]
    [Description("Performs basic arithmetic operations (add, subtract, multiply, divide).")]
    public CalculationResult Calculate(
        [Description("First number")] double a,
        [Description("Second number")] double b,
        [Description("Operation: 'add', 'subtract', 'multiply', 'divide'")] string operation)
    {
        _logger.LogInformation("MCP Tool 'calculate' called with a={A}, b={B}, operation={Operation}", a, b, operation);
        try
        {
            var result = operation.ToLower() switch
            {
                "add" or "+" => a + b,
                "subtract" or "-" => a - b,
                "multiply" or "*" => a * b,
                "divide" or "/" when b != 0 => a / b,
                "divide" or "/" when b == 0 => throw new DivideByZeroException("Cannot divide by zero"),
                _ => throw new ArgumentException($"Unknown operation: {operation}")
            };

            return new CalculationResult(
                Result: result,
                Operation: $"{a} {operation} {b}",
                Success: true,
                Message: "Calculation completed successfully"
            );
        }
        catch (Exception ex)
        {
            return new CalculationResult(
                Result: 0,
                Operation: $"{a} {operation} {b}",
                Success: false,
                Message: ex.Message
            );
        }
    }

    [McpServerTool(Name = "squareRoot")]
    [Description("Calculates the square root of a number.")]
    public CalculationResult SquareRoot(
        [Description("Number to find square root of")] double number)
    {
        _logger.LogInformation("MCP Tool 'squareRoot' called with number={Number}", number);
        if (number < 0)
        {
            return new CalculationResult(
                Result: double.NaN,
                Operation: $"sqrt({number})",
                Success: false,
                Message: "Cannot calculate square root of negative number"
            );
        }

        var result = Math.Sqrt(number);
        return new CalculationResult(
            Result: result,
            Operation: $"sqrt({number})",
            Success: true,
            Message: "Square root calculated successfully"
        );
    }

    [McpServerTool(Name = "power")]
    [Description("Raises a number to a specified power.")]
    public CalculationResult Power(
        [Description("Base number")] double baseNumber,
        [Description("Exponent")] double exponent)
    {
        _logger.LogInformation("MCP Tool 'power' called with baseNumber={BaseNumber}, exponent={Exponent}", baseNumber, exponent);
        try
        {
            var result = Math.Pow(baseNumber, exponent);
            return new CalculationResult(
                Result: result,
                Operation: $"{baseNumber}^{exponent}",
                Success: true,
                Message: "Power calculation completed successfully"
            );
        }
        catch (Exception ex)
        {
            return new CalculationResult(
                Result: double.NaN,
                Operation: $"{baseNumber}^{exponent}",
                Success: false,
                Message: ex.Message
            );
        }
    }

    [McpServerTool(Name = "generateFibonacci")]
    [Description("Generates the Fibonacci sequence up to n terms.")]
    public FibonacciResult GenerateFibonacci(
        [Description("Number of terms to generate (1-50)")] int terms)
    {
        _logger.LogInformation("MCP Tool 'generateFibonacci' called with terms={Terms}", terms);
        if (terms < 1)
        {
            return new FibonacciResult(
                Sequence: Array.Empty<long>(),
                Terms: 0,
                Success: false,
                Message: "Number of terms must be at least 1"
            );
        }

        if (terms > 50)
        {
            return new FibonacciResult(
                Sequence: Array.Empty<long>(),
                Terms: 0,
                Success: false,
                Message: "Maximum 50 terms allowed to prevent overflow"
            );
        }

        var sequence = new long[terms];

        if (terms >= 1) sequence[0] = 0;
        if (terms >= 2) sequence[1] = 1;

        for (int i = 2; i < terms; i++)
        {
            sequence[i] = sequence[i - 1] + sequence[i - 2];
        }

        return new FibonacciResult(
            Sequence: sequence,
            Terms: terms,
            Success: true,
            Message: $"Generated Fibonacci sequence with {terms} terms"
        );
    }

    [McpServerTool(Name = "isPrime")]
    [Description("Checks if a number is prime.")]
    public PrimeCheckResult IsPrime(
        [Description("Number to check for primality")] long number)
    {
        _logger.LogInformation("MCP Tool 'isPrime' called with number={Number}", number);
        if (number < 2)
        {
            return new PrimeCheckResult(
                Number: number,
                IsPrime: false,
                Explanation: "Numbers less than 2 are not prime"
            );
        }

        if (number == 2)
        {
            return new PrimeCheckResult(
                Number: number,
                IsPrime: true,
                Explanation: "2 is the only even prime number"
            );
        }

        if (number % 2 == 0)
        {
            return new PrimeCheckResult(
                Number: number,
                IsPrime: false,
                Explanation: "Even numbers greater than 2 are not prime"
            );
        }

        // Check odd divisors up to sqrt(number)
        long sqrt = (long)Math.Sqrt(number);
        for (long i = 3; i <= sqrt; i += 2)
        {
            if (number % i == 0)
            {
                return new PrimeCheckResult(
                    Number: number,
                    IsPrime: false,
                    Explanation: $"Divisible by {i}"
                );
            }
        }

        return new PrimeCheckResult(
            Number: number,
            IsPrime: true,
            Explanation: "No divisors found, number is prime"
        );
    }
}

public record CalculationResult(
    double Result,
    string Operation,
    bool Success,
    string Message
);

public record FibonacciResult(
    long[] Sequence,
    int Terms,
    bool Success,
    string Message
);

public record PrimeCheckResult(
    long Number,
    bool IsPrime,
    string Explanation
);
