using System.Reflection;

namespace ErpBackend.Tests.Architecture;

/// <summary>
/// 依賴檢查輔助類別
/// 透過反射直接檢查類型的命名空間依賴，避免對 NetArchTest API 的過度依賴。
/// </summary>
public static class DependencyInspector
{
    /// <summary>
    /// 取得指定組件中位於特定命名空間下的所有具體類型（類別）。
    /// </summary>
    public static List<Type> GetConcreteClasses(Assembly assembly, string namespacePrefix)
    {
        return assembly.GetTypes()
            .Where(t => t.IsClass
                        && !t.IsAbstract
                        && !t.IsGenericType
                        && t.Namespace != null
                        && t.Namespace.StartsWith(namespacePrefix, StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>
    /// 檢查類型是否直接依賴某個命名空間。
    /// </summary>
    public static bool DependsOnNamespace(Type type, string namespacePrefix)
    {
        // 檢查欄位
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
                                 | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var fields = type.GetFields(flags);
        foreach (var field in fields)
        {
            if (NamespacesMatch(field.FieldType, namespacePrefix))
                return true;
        }

        // 檢查屬性
        var props = type.GetProperties(flags);
        foreach (var prop in props)
        {
            if (NamespacesMatch(prop.PropertyType, namespacePrefix))
                return true;
        }

        // 檢查方法參數與回傳
        var methods = type.GetMethods(flags);
        foreach (var method in methods)
        {
            if (NamespacesMatch(method.ReturnType, namespacePrefix))
                return true;
            foreach (var param in method.GetParameters())
            {
                if (NamespacesMatch(param.ParameterType, namespacePrefix))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 找出指定命名空間下，依賴另一個命名空間的所有類型名稱。
    /// </summary>
    public static List<string> FindDependencies(Assembly assembly, string fromNamespacePrefix, string toNamespacePrefix)
    {
        var result = new List<string>();
        var candidates = GetConcreteClasses(assembly, fromNamespacePrefix);
        foreach (var type in candidates)
        {
            if (DependsOnNamespace(type, toNamespacePrefix))
            {
                result.Add(type.FullName ?? type.Name);
            }
        }
        return result;
    }

    private static bool NamespacesMatch(Type type, string namespacePrefix)
    {
        // 處理泛型參數
        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            foreach (var arg in genericArgs)
            {
                if (NamespacesMatch(arg, namespacePrefix))
                    return true;
            }
        }

        var ns = type.Namespace ?? string.Empty;
        return ns.StartsWith(namespacePrefix, StringComparison.Ordinal);
    }
}

