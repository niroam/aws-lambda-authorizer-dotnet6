using Microsoft.Extensions.Logging;

namespace TenantAuthorizer;
public class LoggerService : ILoggerService
{
    private ILoggerFactory _loggerFactory;

    public LoggerService()
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddLambdaLogger(CreateLambdaLoggerOptions());
        });
    }

    /// <summary>
    /// Will return a logger with the specified log level option
    /// </summary>
    public static LoggerService CreateWithMinimumLogLevel(string level)
    {
        if (String.IsNullOrEmpty(level))
        {
            Console.WriteLine("No level specified, falling back to default");
            return new LoggerService(LogLevel.Information);
        }

        return new LoggerService(MapToLogLevelEnum(level));
    }
                
    private LoggerService(LogLevel minimumLogLevel)
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(minimumLogLevel);
            builder.AddLambdaLogger(CreateLambdaLoggerOptions());
        });
    }
        
    private static LambdaLoggerOptions CreateLambdaLoggerOptions()
    {
        var loggerOptions = new LambdaLoggerOptions();
        loggerOptions.IncludeCategory = true;
        loggerOptions.IncludeLogLevel = true;
        loggerOptions.IncludeNewline = true;
        loggerOptions.IncludeException = true;
        loggerOptions.IncludeEventId = true;
        loggerOptions.IncludeScopes = true;

        // Configure Filter to only log some 
        loggerOptions.Filter = (category, logLevel) =>
        {
            // For some categories, only log events with minimum LogLevel
            if (string.Equals(category, "Default", StringComparison.Ordinal))
            {
                return (logLevel >= LogLevel.Debug);
            }

            if (string.Equals(category, "Microsoft", StringComparison.Ordinal))
            {
                return (logLevel >= LogLevel.Information);
            }

            // Log everything else
            return true;
        };
        return loggerOptions;
    }

    public static LogLevel MapToLogLevelEnum(string level)
    {
        switch (level.ToLower())
        {
            case "info":
            case "information":
                return LogLevel.Information;
                
            case "warn":
            case "warning":
                return LogLevel.Warning;
                
            case "debug": return LogLevel.Debug;
                
            case "trace": return LogLevel.Trace;
                
            case "error": return LogLevel.Error;
                
            case "critical": return LogLevel.Critical;
                
            default:
                throw new Exception($"Unsupported log level parameter {level}");
        }
    }

    public ILogger<T> CreateLogger<T>()
    {
        return _loggerFactory.CreateLogger<T>();
    }

    public ILoggerFactory GetLoggerFactory()
    {
        return _loggerFactory;
    }
}