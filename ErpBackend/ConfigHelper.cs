public static class ConfigHelper
{
    public static string ResolvePlaceholder(string? configVal, string? envVarName, string fallback)
    {
        if (!string.IsNullOrEmpty(configVal) && configVal.StartsWith("${") && configVal.EndsWith("}"))
        {
            var envName = configVal.Substring(2, configVal.Length - 3);
            configVal = Environment.GetEnvironmentVariable(envName);
        }
        return configVal ?? Environment.GetEnvironmentVariable(envVarName ?? "") ?? fallback;
    }
}
