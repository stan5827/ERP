using System;

namespace ErpBackend.Validation
{
    /// <summary>
    /// Comparison validation rule for comparing two fields
    /// </summary>
    public class ComparisonRule : IValidationRule
    {
        public string ErrorMessage { get; }
        public string OtherFieldName { get; }
        private readonly Func<object?, object?, bool> _comparison;

        /// <summary>
        /// Initializes a new instance of the ComparisonRule class
        /// </summary>
        /// <param name="otherFieldName">The name of the field to compare against</param>
        /// <param name="comparison">The comparison function</param>
        /// <param name="errorMessage">The error message to display if validation fails</param>
        public ComparisonRule(string otherFieldName, Func<object?, object?, bool> comparison, string? errorMessage = null)
        {
            OtherFieldName = otherFieldName ?? throw new ArgumentNullException(nameof(otherFieldName));
            _comparison = comparison ?? throw new ArgumentNullException(nameof(comparison));
            ErrorMessage = errorMessage ?? $"值必須大於等於 {otherFieldName}";
        }

        /// <summary>
        /// Validates that the current field's value satisfies the comparison with the other field's value
        /// </summary>
        /// <param name="value">The value of the current field</param>
        /// <returns>True if validation passes, false otherwise</returns>
        public bool Validate(object? value)
        {
            // This method should not be called directly - use ValidateWithOtherValue instead
            throw new NotSupportedException("Use ValidateWithOtherValue method for comparison rules");
        }

        /// <summary>
        /// Validates that the current field's value satisfies the comparison with the other field's value
        /// </summary>
        /// <param name="value">The value of the current field</param>
        /// <param name="otherValue">The value of the other field</param>
        /// <returns>True if validation passes, false otherwise</returns>
        public bool ValidateWithOtherValue(object? value, object? otherValue)
        {
            // Handle null values - if current value is null, validation fails
            if (value == null) return false;
            
            // If other value is null, we can't compare - validation passes (other field validation will catch this)
            if (otherValue == null) return true;
            
            return _comparison(value, otherValue);
        }
    }
}