using Amazon.Auth.AccessControlPolicy;

namespace TenantAuthorizer;
public static class AuthManager
{
    public static string GenerateTenantPolicyDocument(string accountId, string region, string tenantId)
    {
        // using Amazon.Auth.AccessControlPolicy;

        // Create a policy that looks like this:
        /*
    {	
    "Version": "2012-10-17",
        "Statement": [
            {
                "Effect": "Allow",
                "Action": [
                    "dynamodb:UpdateItem",
                    "dynamodb:GetItem",
                    "dynamodb:PutItem",
                    "dynamodb:DeleteItem",
                    "dynamodb:Query"      
                ],
                "Resource": [
                    "arn:aws:dynamodb:{0}:{1}:table/Product-*".format(region, aws_account_id),                      
                ],
                "Condition": {
                    "ForAllValues:StringLike": {
                        "dynamodb:LeadingKeys": [
                            "{0}-*".format(tenant_id)
                        ]
                    }
                }
            },
            {
                "Effect": "Allow",
                "Action": [
                    "dynamodb:UpdateItem",
                    "dynamodb:GetItem",
                    "dynamodb:PutItem",
                    "dynamodb:DeleteItem",
                    "dynamodb:Query"               
                ],
                "Resource": [
                    "arn:aws:dynamodb:{0}:{1}:table/Order-*".format(region, aws_account_id),                      
                ],
                "Condition": {
                    "ForAllValues:StringLike": {
                        "dynamodb:LeadingKeys": [
                            "{0}-*".format(tenant_id)
                        ]
                    }
                }
            }
        ]
    }
        */

        var actions = new List<ActionIdentifier>
        {
            new ActionIdentifier("dynamodb:UpdateItem"),
            new ActionIdentifier("dynamodb:GetItem"),
            new ActionIdentifier("dynamodb:PutItem"),
            new ActionIdentifier("dynamodb:DeleteItem"),
            new ActionIdentifier("dynamodb:Query")
        };

        var resources = new List<Resource>();
        resources.Add(new Resource($"arn:aws:dynamodb:{region}:{accountId}:table/Product-*"));

        var condition = new List<Condition>();

        condition.Add(ConditionFactory.NewCondition(ConditionFactory.StringComparisonType.StringLike, "dynamodb:LeadingKeys", $"{tenantId}-*"));

        // Statement for the Product table
        var statement =
            new Amazon.Auth.AccessControlPolicy.Statement(Amazon.Auth.AccessControlPolicy.Statement.StatementEffect
                .Allow)
            {
                Actions = actions,
                Id = "",
                Resources = resources,
                Conditions = condition
            };


        // Statement for the Order table
        resources.Clear();
        resources.Add(new Resource($"arn:aws:dynamodb:{region}:{accountId}:table/Order-*"));

        var statement2 =
            new Amazon.Auth.AccessControlPolicy.Statement(Amazon.Auth.AccessControlPolicy.Statement.StatementEffect
                .Allow)
            {
                Actions = actions,
                Id = "",
                Resources = resources,
                Conditions = condition
            };

        var statements = new List<Amazon.Auth.AccessControlPolicy.Statement>();

        statements.Add(statement);
        statements.Add(statement2);

        var policy = new Policy
        {
            Version = "2012-10-17",
            Statements = statements
        };

        return policy.ToJson();
    }
}
