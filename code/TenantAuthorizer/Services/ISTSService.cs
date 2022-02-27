using Amazon.Runtime;

namespace TenantAuthorizer;

public interface ISTSService
{
    SessionAWSCredentials GetTenantSessionCredentials(string iamPolicy, string roleArn);
}

