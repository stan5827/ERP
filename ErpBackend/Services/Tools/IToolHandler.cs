using System.Collections.Generic;
using System.Threading.Tasks;

namespace ErpBackend.Services;

public interface IToolHandler
{
    Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters);
}
