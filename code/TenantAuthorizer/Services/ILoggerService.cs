using Microsoft.Extensions.Logging;

namespace TenantAuthorizer;
public interface ILoggerService
{
    ILogger<T> CreateLogger<T>();
    ILoggerFactory GetLoggerFactory();
}
