using System.Globalization;
using System.Text.Json;
using AgenticPlatform.Core.Constants;
using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Interfaces;
using AgenticPlatform.Core.Models.Tools;

namespace AgenticPlatform.Infrastructure.Services.Tools;

public sealed class CalculatorToolExecutor : ToolExecutorBase, IToolExecutor
{
    public string Name => BuiltInToolCategories.Calculator;

    public bool CanExecute(Tool tool)
    {
        return tool.Category.Equals(BuiltInToolCategories.Calculator, StringComparison.OrdinalIgnoreCase);
    }

    public Task<ToolExecutionResult> ExecuteAsync(ToolExecutionRequest request, CancellationToken cancellationToken = default)
    {
        return ExecuteCoreAsync(request.Tool, () =>
        {
            using var document = JsonDocument.Parse(request.InputJson);
            var expression = document.RootElement.TryGetProperty("expression", out var expressionElement)
                ? expressionElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new InvalidOperationException("Calculator input requires an 'expression' value.");
            }

            var result = new ExpressionParser(expression).Parse();
            var json = JsonSerializer.Serialize(new
            {
                expression,
                result
            });

            return Task.FromResult(json);
        });
    }

    private sealed class ExpressionParser
    {
        private readonly string _expression;
        private int _position;

        public ExpressionParser(string expression)
        {
            _expression = expression;
        }

        public double Parse()
        {
            var value = ParseExpression();
            SkipWhiteSpace();

            if (_position != _expression.Length)
            {
                throw new InvalidOperationException("Expression contains unexpected characters.");
            }

            return value;
        }

        private double ParseExpression()
        {
            var value = ParseTerm();

            while (true)
            {
                SkipWhiteSpace();

                if (Match('+'))
                {
                    value += ParseTerm();
                }
                else if (Match('-'))
                {
                    value -= ParseTerm();
                }
                else
                {
                    return value;
                }
            }
        }

        private double ParseTerm()
        {
            var value = ParseFactor();

            while (true)
            {
                SkipWhiteSpace();

                if (Match('*'))
                {
                    value *= ParseFactor();
                }
                else if (Match('/'))
                {
                    var divisor = ParseFactor();
                    if (divisor == 0)
                    {
                        throw new DivideByZeroException("Expression attempted to divide by zero.");
                    }

                    value /= divisor;
                }
                else
                {
                    return value;
                }
            }
        }

        private double ParseFactor()
        {
            SkipWhiteSpace();

            if (Match('+'))
            {
                return ParseFactor();
            }

            if (Match('-'))
            {
                return -ParseFactor();
            }

            if (Match('('))
            {
                var value = ParseExpression();
                if (!Match(')'))
                {
                    throw new InvalidOperationException("Expression contains an unclosed parenthesis.");
                }

                return value;
            }

            return ParseNumber();
        }

        private double ParseNumber()
        {
            SkipWhiteSpace();
            var start = _position;

            while (_position < _expression.Length
                && (char.IsDigit(_expression[_position]) || _expression[_position] == '.'))
            {
                _position++;
            }

            if (start == _position)
            {
                throw new InvalidOperationException("Expression expected a number.");
            }

            var token = _expression[start.._position];
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                throw new InvalidOperationException($"Expression contains an invalid number '{token}'.");
            }

            return value;
        }

        private bool Match(char value)
        {
            SkipWhiteSpace();

            if (_position >= _expression.Length || _expression[_position] != value)
            {
                return false;
            }

            _position++;
            return true;
        }

        private void SkipWhiteSpace()
        {
            while (_position < _expression.Length && char.IsWhiteSpace(_expression[_position]))
            {
                _position++;
            }
        }
    }
}
