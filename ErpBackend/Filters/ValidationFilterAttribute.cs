using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ErpBackend.Validation;
using ErpBackend.DTOs;
using ErpBackend.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ErpBackend.Filters
{
    /// <summary>
    /// 自訂驗證過濾器，自動驗證帶有資料註解標記的請求模型
    /// </summary>
    public class ValidationFilterAttribute : IActionFilter
    {
        private readonly Dictionary<Type, Dictionary<string, List<IValidationRule>>> _validationRulesCache = 
            new Dictionary<Type, Dictionary<string, List<IValidationRule>>>();
        private readonly object _cacheLock = new object();

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // 檢查模型狀態
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors != null && x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.FirstOrDefault()?.ErrorMessage ?? "驗證錯誤"
                    );

                 var errorMessage = string.Join("; ", errors.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                 context.Result = new BadRequestObjectResult(
                     ApiResponse.Fail($"驗證失敗: {errorMessage}")
                 );
                return;
            }

            // 檢查每個參數是否需要自定義驗證
            foreach (var parameter in context.ActionDescriptor.Parameters)
            {
                // 只檢查從請求體綁定的複雜類型參數
                if (parameter.BindingInfo?.BindingSource == Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Body &&
                    parameter.ParameterType.IsClass && 
                    parameter.ParameterType != typeof(string))
                {
                    if (context.ActionArguments.TryGetValue(parameter.Name, out var value) && value != null)
                    {
                        ValidateCustomObject(parameter.ParameterType, value, context);
                    }
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // 不需要在執行後做任何事情
        }

        private void ValidateCustomObject(Type type, object value, ActionExecutingContext context)
        {
            // 檢查緩存
            if (!_validationRulesCache.TryGetValue(type, out var fieldRules))
            {
                lock (_cacheLock)
                {
                    if (!_validationRulesCache.TryGetValue(type, out fieldRules))
                    {
                        fieldRules = BuildValidationRules(type);
                        _validationRulesCache[type] = fieldRules;
                    }
                }
            }

              var validationResult = Validator.ValidateObject(value, type, fieldRules);
              if (!validationResult.IsValid)
              {
                  var firstError = validationResult.Errors.FirstOrDefault();
                  context.Result = new BadRequestObjectResult(
                      ApiResponse.Fail(firstError.Value ?? "驗證失敗")
                  );
              }
        }

private Dictionary<string, List<IValidationRule>> BuildValidationRules(Type type)
{
    var rules = new Dictionary<string, List<IValidationRule>>();
    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

    foreach (var property in properties)
    {
        var propertyRules = new List<IValidationRule>();
        
        // 檢查 Required 屬性
        var requiredAttribute = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>();
        if (requiredAttribute != null)
        {
            propertyRules.Add(new RequiredRule(
                string.IsNullOrEmpty(requiredAttribute.ErrorMessage) 
                    ? $"此欄位為必填" 
                    : requiredAttribute.ErrorMessage));
        }

        // 檢查 StringLength 屬性
        var stringLengthAttribute = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.StringLengthAttribute>();
        if (stringLengthAttribute != null)
        {
            propertyRules.Add(new StringLengthRule(
                stringLengthAttribute.MinimumLength,
                stringLengthAttribute.MaximumLength,
                stringLengthAttribute.ErrorMessage ?? string.Empty));
        }

        // 檢查 Range 屬性
        var rangeAttribute = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.RangeAttribute>();
        if (rangeAttribute != null)
        {
            propertyRules.Add(new RangeRule(
                Convert.ToDecimal(rangeAttribute.Minimum),
                Convert.ToDecimal(rangeAttribute.Maximum),
                rangeAttribute.ErrorMessage ?? string.Empty));
        }

        // 檢查 EmailAddress 屬性
        var emailAttribute = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.EmailAddressAttribute>();
        if (emailAttribute != null)
        {
            propertyRules.Add(new EmailRule());
        }

        // 檢查 RegularExpression 屬性
         var regexAttribute = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.RegularExpressionAttribute>();
         if (regexAttribute != null)
         {
             propertyRules.Add(new RegexRule(
                 regexAttribute.Pattern,
                 string.IsNullOrEmpty(regexAttribute.ErrorMessage) 
                     ? $"輸入格式不正確" 
                     : regexAttribute.ErrorMessage));
         }

        // 只添加有規則的屬性
        if (propertyRules.Count > 0)
        {
            // 使用不區分大小寫的鍵，或者同時添加PascalCase和camelCase版本
            rules[property.Name] = propertyRules;
            // 也添加camelCase版本以匹配前端發送的屬性名
            var camelCaseName = char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
            if (!rules.ContainsKey(camelCaseName))
            {
                rules[camelCaseName] = propertyRules;
            }
        }
    }

    return rules;
}
    }
}