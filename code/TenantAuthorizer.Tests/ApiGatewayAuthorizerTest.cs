using Xunit;
using Amazon.Lambda.TestUtilities;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using FluentAssertions;

namespace TenantAuthorizer.Tests;

public class ApiGatewayAuthorizerTest
{
    private static readonly TestLambdaContext Context = new TestLambdaContext();
    private const string validToken = "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6InRlc3QtcnNhIn0.eyJvcmlnaW5fanRpIjoiYjA2NjFkZjItMjZmMS00NzFkLTkwODAtODQxMDc0M2M5MGRhIiwiY3VzdG9tOnRlbmFudElkIjoiMTIzNDU2N3h5eiIsInN1YiI6ImQxZmRmMDA2LTNlOTktNDE1ZS05ODRlLWI2NDliZWIyMjEyZiIsImF1ZCI6IjI4aXFyZ2lybW5oM3ZjMmRwbGRnNGgxOW4iLCJldmVudF9pZCI6ImYwYmJkZGZkLTU2NGYtNDI2OC05NGI3LTJiMGU2NGY1N2Q1MSIsInRva2VuX3VzZSI6ImlkIiwiYXV0aF90aW1lIjoxNjQ0ODIzODk0LCJpc3MiOiJodHRwczovL2RvdG5ldDYuYmx1ZXByaW50LmF1dGguY29tIiwiY29nbml0bzp1c2VybmFtZSI6Im5pcm8uYW0iLCJleHAiOjE2NDk5NjgyMzUsImlhdCI6MTY0NTU2NDYzNSwianRpIjoiNjNiY2U2NmItMTUwYi00ZmFjLWJhNjctZmZiZmM1N2Q4MzUwIn0.cXc886rf8nblQCz-yz72fyuZqGn_pCrXXIPMtizIh7eq4pOAjtAc6N4FbcKVZf9Xu_5KdZop_BGPpRSUye0ZPiIq-UggA08vxqpqiF_VHpsLi3zxZZvAKRyTn6hZL6dwxM2E3uzTmdwI2ExgiRnDHwSVNzbumHNGYfYirdzgJKBRayx6ZtpFX26Mbx9ipxi35nHsxthMyxxXhCQ8CSruIbWmyc6vevqs7Qv5zH5dXrKJW2n_wSRRu9nGKCiiwmUPdhFAwiWm0wpE-DOkm91OoVQoxG1MkpN9PIK3BmcfFT-B_CAUMgj2-dPGngBaCzD7bysRIE5bk9B5OKcTdyG9bA";
    private static readonly List<String> validIssuerList = new List<String>() { "https://dotnet6.blueprint.auth.com" };
    private static readonly JsonWebKeySet validJwkCache = new JsonWebKeySet(@"{
  'keys': [
    {
      'kty': 'RSA',
      'n': '6S7asUuzq5Q_3U9rbs-PkDVIdjgmtgWreG5qWPsC9xXZKiMV1AiV9LXyqQsAYpCqEDM3XbfmZqGb48yLhb_XqZaKgSYaC_h2DjM7lgrIQAp9902Rr8fUmLN2ivr5tnLxUUOnMOc2SQtr9dgzTONYW5Zu3PwyvAWk5D6ueIUhLtYzpcB-etoNdL3Ir2746KIy_VUsDwAM7dhrqSK8U2xFCGlau4ikOTtvzDownAMHMrfE7q1B6WZQDAQlBmxRQsyKln5DIsKv6xauNsHRgBAKctUxZG8M4QJIx3S6Aughd3RZC4Ca5Ae9fd8L8mlNYBCrQhOZ7dS0f4at4arlLcajtw',
      'e': 'AQAB',
      'kid': 'test-rsa',
      'alg':'RS256'
    },
    {
      'kty': 'EC',
      'crv': 'P-521',
      'x': 'AYeAr-K3BMaSlnrjmszuJdOYBstGJf0itM2TTGwsaO0-cGcXor8f0LPXbB9B_gLK7m0th3okXzypIrq-qgTMsMig',
      'y': 'AGLdv92aARm6efe_sEJyRJ-n4IBxhMRTm6wIe8AZhlkdLWxzEyfusiXLZHon1Ngt_Q8d_PYWYrbJVWS7VrnK05bJ',
      'kid': 'test-ec',
      'alg':'RS256'
    }
  ]
}");

    [Theory]
    [InlineData(validToken)]
    public void TestValidTokenFrom(string validJwt)
    {
        // Create a fake logger that accepts the AWS request ID
        var mockLogger = new Mock<ILogger<ApiGatewayAuthorizer>>();

        var mockSessionCredentials =
                new Amazon.Runtime.SessionAWSCredentials("AccessKeyId","SecretAccessKey", "SessionToken");

        // Create fake STS Service
        var mockStsService = new Mock<ISTSService>();
        mockStsService.Setup(x => x.GetTenantSessionCredentials(It.IsAny<string>(), It.IsAny<string>())).Returns(mockSessionCredentials);

        var apigAuthRequest = new APIGatewayCustomAuthorizerRequestV2();
        apigAuthRequest.AuthorizationToken = validJwt;
        apigAuthRequest.MethodArn = "arn:aws:execute-api:eu-west-2:xxxxxx:yyyyyyyyy/dummy-stage/GET/";
        // Invoke the function
        var testFunction = new ApiGatewayAuthorizer(mockLogger.Object, "test", false, validIssuerList, validJwkCache, mockStsService.Object);

        var response = testFunction.FunctionHandler(apigAuthRequest, Context);

        response.PolicyDocument.Should().NotBeNull();
        response.Context.Should().NotBeEmpty();

    }
}