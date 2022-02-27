namespace TenantAuthorizer;

using System.Runtime.Serialization;

/// <summary>
/// An object representing the expected format of an API Gateway authorization response.
/// </summary>
[DataContract]
public class APIGatewayCustomAuthorizerResponseV2
{
    /// <summary>
    /// Gets or sets the ID of the principal.
    /// </summary>
    [DataMember(Name = "principalId")]
    public string PrincipalID { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="APIGatewayCustomAuthorizerPolicy"/> policy document.
    /// </summary>
    [DataMember(Name = "policyDocument")]
    public APIGatewayCustomAuthorizerPolicyV2 PolicyDocument { get; set; } = new APIGatewayCustomAuthorizerPolicyV2();

    /// <summary>
    /// Gets or sets the <see cref="APIGatewayCustomAuthorizerContext"/> property.
    /// </summary>
    [DataMember(Name = "context")]
    public APIGatewayCustomAuthorizerContextOutputV2 Context { get; set; }

    /// <summary>
    /// Gets or sets the usageIdentifierKey.
    /// </summary>
    [DataMember(Name = "usageIdentifierKey")]
    public string UsageIdentifierKey { get; set; }
}
