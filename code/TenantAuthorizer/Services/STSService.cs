using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Microsoft.Extensions.Logging;

namespace TenantAuthorizer;
public class STSService : ISTSService
{
    private ILogger<STSService> _logger;
    private AmazonSecurityTokenServiceClient _stsClient;
    public STSService(ILogger<STSService> logger, string awsRegion)
    {
        _logger = logger;

        var config = new AmazonSecurityTokenServiceConfig { ServiceURL = $"https://sts.{awsRegion}.amazonaws.com" };
        _stsClient = new AmazonSecurityTokenServiceClient(config);
    }

    /// <summary>
    /// Assumes the tenant specific role/policy and returns the credentials for it
    /// </summary>
    public SessionAWSCredentials GetTenantSessionCredentials(string iamPolicy, string roleArn)
    {
        _logger.LogDebug($"Trying to assume role with ARN : {roleArn}");

        var assumeRoleRequest = new AssumeRoleRequest
        {
            DurationSeconds = 900,
            RoleArn = roleArn,
            RoleSessionName = "tenant-aware-session",
            Policy = iamPolicy
        };

        var assumeRoleResponse = _stsClient.AssumeRoleAsync(assumeRoleRequest).Result;

        _logger.LogDebug($"Creating session crendetions : {assumeRoleResponse}");

        var sessionCredentials =
            new SessionAWSCredentials(assumeRoleResponse.Credentials.AccessKeyId,
                assumeRoleResponse.Credentials.SecretAccessKey,
                assumeRoleResponse.Credentials.SessionToken);
        return sessionCredentials;
    }
}