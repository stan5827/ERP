using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ErpBackend.Validation
{
    /// <summary>
    /// Interface for validation rules
    /// </summary>
    public interface IValidationRule
    {
        bool Validate(object? value);
        string ErrorMessage { get; }
    }

    /// <summary>
    /// Required validation rule
    /// </summary>
    public class RequiredRule : IValidationRule
    {
        public string ErrorMessage { get; }

        public RequiredRule(string errorMessage = "此欄位為必填")
        {
            ErrorMessage = errorMessage;
        }

        public bool Validate(object? value)
        {
            if (value == null) return false;
            if (value is string str) return !string.IsNullOrWhiteSpace(str);
            if (value is ValueType) return true; // Value types (int, bool, etc.) are considered valid if not null
            return true; // For other types, if not null, consider valid
        }
    }

    /// <summary>
    /// String length validation rule
    /// </summary>
    public class StringLengthRule : IValidationRule
    {
        private readonly int _minLength;
        private readonly int? _maxLength;
        public string ErrorMessage { get; }

        public StringLengthRule(int minLength, int? maxLength = null, string? errorMessage = null)
        {
            _minLength = minLength;
            _maxLength = maxLength;
            if (_maxLength.HasValue)
                ErrorMessage = errorMessage ?? $"長度必須在 {_minLength} 到 {_maxLength} 個字元之間";
            else
                ErrorMessage = errorMessage ?? $"長度至少需要 {_minLength} 個字元";
        }

        public bool Validate(object? value)
        {
            if (value == null) return true;
            if (!(value is string str)) return true;
            
            if (str.Length < _minLength) return false;
            if (_maxLength.HasValue && str.Length > _maxLength.Value) return false;
            
            return true;
        }
    }

    /// <summary>
    /// Numeric range validation rule
    /// </>
    public class RangeRule : IValidationRule
    {
        private readonly decimal _min;
        private readonly decimal? _max;
        public string ErrorMessage { get; }

        public RangeRule(decimal min, decimal? max = null, string? errorMessage = null)
        {
            _min = min;
            _max = max;
            if (_max.HasValue)
                ErrorMessage = errorMessage ?? $"值必須在 {_min} 到 {_max} 之間";
            else
                ErrorMessage = errorMessage ?? $"值不能小於 {_min}";
        }

        public bool Validate(object? value)
        {
            if (value == null || value == DBNull.Value) return true;
            if (value is string s && string.IsNullOrEmpty(s)) return true;
            
            decimal numericValue;
            if (value is int i) numericValue = i;
            else if (value is long l) numericValue = l;
            else if (value is float f) numericValue = (decimal)f;
            else if (value is double d) numericValue = (decimal)d;
            else if (value is decimal dec) numericValue = dec;
            else return false; // Not a numeric type
            
            if (numericValue < _min) return false;
            if (_max.HasValue && numericValue > _max.Value) return false;
            
            return true;
        }
    }

    /// <summary>
    /// Email validation rule
    /// </summary>
    public class EmailRule : IValidationRule
    {
        public string ErrorMessage { get; }

        public EmailRule(string errorMessage = "請輸入有效的電子郵件地址")
        {
            ErrorMessage = errorMessage;
        }
        
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public bool Validate(object? value)
        {
            if (value == null) return false;
            if (!(value is string str)) return false;
            
            return EmailRegex.IsMatch(str);
        }
    }

    /// <summary>
    /// Regular expression validation rule
    /// </summary>
    public class RegexRule : IValidationRule
    {
        private readonly Regex _regex;
        public string ErrorMessage { get; }

        public RegexRule(string pattern, string errorMessage, RegexOptions options = RegexOptions.None)
        {
            _regex = new Regex(pattern, options);
            ErrorMessage = errorMessage;
        }

        public bool Validate(object? value)
        {
            if (value == null) return false;
            if (!(value is string str)) return false;
            
            return _regex.IsMatch(str);
        }
    }

    /// <summary>
    /// Validation result container
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public Dictionary<string, string> Errors { get; } = new Dictionary<string, string>();
        
        public void AddError(string fieldName, string errorMessage)
        {
            if (!Errors.ContainsKey(fieldName))
            {
                Errors[fieldName] = errorMessage;
            }
        }
        
        public string? GetError(string fieldName)
        {
            Errors.TryGetValue(fieldName, out var error);
            return error;
        }
    }

    /// <summary>
    /// Validator for validating objects against rules
    /// </summary>
    public static class Validator
    {
        /// <summary>
        /// Validates an object against a dictionary of field rules
        /// </summary>
        public static ValidationResult ValidateObject<T>(
            T obj, 
            Dictionary<string, List<IValidationRule>> fieldRules)
        {
            return ValidateObject(obj, typeof(T), fieldRules);
        }

        /// <summary>
        /// Validates an object against a dictionary of field rules with explicit type
        /// </summary>
        public static ValidationResult ValidateObject(
            object? obj,
            Type type,
            Dictionary<string, List<IValidationRule>> fieldRules)
        {
            var result = new ValidationResult();
            
            foreach (var kvp in fieldRules)
            {
                var fieldName = kvp.Key;
                var rules = kvp.Value;
                
                var propertyInfo = type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                {
                    continue;
                }
                
                var value = obj != null ? propertyInfo.GetValue(obj) : null;
                
                foreach (var rule in rules)
                {
                    if (rule is ComparisonRule comparisonRule)
                    {
                        var otherPropertyInfo = type.GetProperty(comparisonRule.OtherFieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                        if (otherPropertyInfo == null)
                        {
                            result.AddError(fieldName, $"找不到比較屬性: {comparisonRule.OtherFieldName}");
                            continue;
                        }
                        
                        var otherValue = obj != null ? otherPropertyInfo.GetValue(obj) : null;
                        
                        if (!comparisonRule.ValidateWithOtherValue(value, otherValue))
                        {
                            result.AddError(fieldName, rule.ErrorMessage);
                            break;
                        }
                    }
                    else
                    {
                        if (!rule.Validate(value))
                        {
                            result.AddError(fieldName, rule.ErrorMessage);
                            break;
                        }
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Validates that an object is valid, throwing an exception if not
        /// </summary>
        public static void ValidateAndThrow<T>(
            T obj, 
            Dictionary<string, List<IValidationRule>> fieldRules)
        {
            var result = ValidateObject(obj, fieldRules);
            if (!result.IsValid)
            {
                var errors = string.Join("; ", result.Errors.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                throw new ValidationException($"驗證失敗: {errors}");
            }
        }
    }

    /// <summary>
    /// Custom validation exception
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}