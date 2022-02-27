namespace TenantAuthorizer;

using Amazon.Lambda.APIGatewayEvents;
using System;
using System.Collections.Generic;

/// <summary>
/// An object representing an IAM policy.
/// </summary>
public class APIGatewayCustomAuthorizerPolicyV2
{
    /// <summary>
    /// Gets or sets the IAM API version.
    /// </summary>
    public string Version { get; set; } = "2012-10-17";

    /// <summary>
    /// Gets or sets a list of IAM policy statements to apply.
    /// </summary>
    public List<IAMPolicyStatement> Statement { get; set; } = new List<IAMPolicyStatement>();

    public static implicit operator APIGatewayCustomAuthorizerPolicyV2(APIGatewayCustomAuthorizerContextOutput v)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// A class representing an IAM Policy Statement.
    /// </summary>
    public class IAMPolicyStatement
    {
        /// <summary>
        /// Gets or sets the effect the statement has.
        /// </summary>
        public string Effect { get; set; } = "Allow";

        /// <summary>
        /// Gets or sets the action/s the statement has.
        /// </summary>
        public HashSet<string> Action { get; set; }

        /// <summary>
        /// Gets or sets the resources the statement applies to.
        /// </summary>
        public HashSet<string> Resource { get; set; }
    }
}