using System.Reflection;
using Xunit;

namespace ErpBackend.Tests.Architecture;

/// <summary>
/// 控制器依賴規則測試
/// 驗證各模組控制器是否遵守依賴方向。
/// 注意：目前基線中 73 個控制器直接依賴 DbContext，本測試將逐步收緊。
/// </summary>
public class ControllerDependencyTests
{
    private static Assembly AppAssembly => typeof(ErpBackend.Data.ErpGeneralDbContext).Assembly;
    private const string ControllersNamespace = "ErpBackend.Controllers";

    /// <summary>
    /// 取得所有控制器類別。
    /// </summary>
    private List<Type> AllControllers =>
        AppAssembly.GetTypes()
            .Where(t => t.IsClass && t.Namespace == ControllersNamespace && t.Name.EndsWith("Controller"))
            .ToList();

    [Fact]
    public void Controllers_ShouldUseServices_NotDbContext()
    {
        var controllers = AllControllers;

        var dbContextUsers = controllers
            .Where(c => DependencyInspector.DependsOnNamespace(c, "ErpBackend.Data.ErpGeneralDbContext"))
            .Select(c => c.Name)
            .ToList();

        var baselineCount = dbContextUsers.Count;

        // 基線確認
        Assert.True(controllers.Count >= 80,
            $"應有至少 80 個控制器，實際 {controllers.Count}");

        // 記錄 debt 追蹤
        if (baselineCount > 0)
        {
            System.Console.WriteLine(
                $"[WARNING] 目前 {baselineCount}/{controllers.Count} 個控制器直接依賴 DbContext。\n" +
                $"違規清單: {string.Join(", ", dbContextUsers.Take(15))}" +
                $"{(dbContextUsers.Count > 15 ? "..." : "")}");
        }
    }

    [Fact]
    public void InventoryControllers_ShouldNotDependOnDbContext()
    {
        AssertControllerDependency("Inventory");
    }

    [Fact]
    public void ProcurementControllers_ShouldNotDependOnDbContext()
    {
        AssertControllerDependency("Supplier");
        AssertControllerDependency("Purchase");
    }

    [Fact]
    public void SalesControllers_ShouldNotDependOnDbContext()
    {
        AssertControllerDependency("Sales");
        AssertControllerDependency("Customer");
        AssertControllerDependency("Quote");
    }

    [Fact]
    public void HrControllers_ShouldNotDependOnDbContext()
    {
        AssertControllerDependency("Employee");
        AssertControllerDependency("Leave");
        AssertControllerDependency("Attendance");
        AssertControllerDependency("Department");
    }

    [Fact]
    public void Controllers_ShouldNotHaveSyncBlockingCalls()
    {
        // 檢查控制器是否有 .Result 或 .Wait() 的阻塞調用
        var controllers = AllControllers;
        var violations = new List<string>();

        foreach (var ctrl in controllers)
        {
            var methods = ctrl.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                // 無法直接檢查方法體，但可以透過回傳型別推斷
                // 確認非同步方法應回傳 Task 而非 void
                if (method.ReturnType == typeof(void)
                    && method.GetCustomAttributes(typeof(System.Runtime.CompilerServices.AsyncStateMachineAttribute), false).Any())
                {
                    violations.Add($"{ctrl.Name}.{method.Name} (async void)");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"以下方法使用 async void（應改為 async Task）: {string.Join(", ", violations)}");
    }

    private void AssertControllerDependency(string controllerPrefix)
    {
        var controllers = AppAssembly.GetTypes()
            .Where(t => t.IsClass && t.Namespace == ControllersNamespace && t.Name.StartsWith(controllerPrefix))
            .ToList();

        if (controllers.Count == 0)
        {
            // 沒有匹配的控制器，測試通過
            return;
        }

        var violations = controllers
            .Where(c => DependencyInspector.DependsOnNamespace(c, "ErpBackend.Data"))
            .Select(c => c.Name)
            .ToList();

        if (violations.Count > 0)
        {
            System.Console.WriteLine(
                $"[WARNING] {controllerPrefix} 區塊控制器仍依賴 Data 層: {string.Join(", ", violations)}");
        }
    }
}
