using System.Security.Claims;
using System.Security.Cryptography;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Text.Json.Serialization;

[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<TenantAuthorizer.HttpApiJsonSerializerContext>))]

namespace TenantAuthorizer;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(APIGatewayCustomAuthorizerRequestV2))]
[JsonSerializable(typeof(APIGatewayCustomAuthorizerResponseV2))]
[JsonSerializable(typeof(APIGatewayCustomAuthorizerPolicyV2))]
[JsonSerializable(typeof(APIGatewayCustomAuthorizerContextOutputV2))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(UnauthorizedException))]
public partial class HttpApiJsonSerializerContext : JsonSerializerContext
{
}
/// <summary>
/// ApiGatewayAuthorizer validates JWT bearer tokens
/// </summary>

public class ApiGatewayAuthorizer
{
    JsonWebKeySet _jwkCache;
    private ILogger _logger;
    private string _envName;
    private bool _validateLifetime;
    private bool _validateAudience = false;
    private List<string> _validIssuerList;
    private JwtSecurityTokenHandler _handler;
    private ISTSService _stsService;
    private string _awsRegion;

    // Main constructor, setup envrionment specific constants
    public ApiGatewayAuthorizer()
    {
        _envName = Environment.GetEnvironmentVariable("ENV_NAME");

        var jwkString = Environment.GetEnvironmentVariable("JWKS_STRING");

        var validIssuerString = Environment.GetEnvironmentVariable("VALID_ISSUERS");

        _awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");

        if (string.IsNullOrEmpty(_envName))
            throw new Exception("ENV_NAME must be set");

        if (string.IsNullOrEmpty(jwkString))
            throw new Exception("JWKS_STRING must be set");

        if (string.IsNullOrEmpty(validIssuerString))
            throw new Exception("VALID_ISSUERS must be set");

        var _loggerService = new LoggerService();
        _logger = _loggerService.CreateLogger<ApiGatewayAuthorizer>();

        _validateLifetime = true;

        _validIssuerList = JsonSerializer.Deserialize<List<string>>(validIssuerString);

        _jwkCache = new JsonWebKeySet(jwkString);

        _handler = new JwtSecurityTokenHandler();

        _stsService = new STSService(_loggerService.CreateLogger<STSService>(), _awsRegion);
    }

    // Additional constructor for unit testing
    public ApiGatewayAuthorizer(ILogger logger, string envName, bool validateLifetime, List<string> validIssuerList, JsonWebKeySet jwkCache, ISTSService stsService)
    {
        _envName = envName;

        _validIssuerList = validIssuerList;

        if (string.IsNullOrEmpty(_envName))
            throw new Exception("ENV_NAME must be set");

        _logger = logger;

        _validateLifetime = validateLifetime;

        _jwkCache = jwkCache;

        _handler = new JwtSecurityTokenHandler();

        _stsService = stsService;

    }

    public APIGatewayCustomAuthorizerResponseV2 FunctionHandler(APIGatewayCustomAuthorizerRequestV2 request, ILambdaContext context)
    {
        using (_logger.BeginScope(context.AwsRequestId))
        {
            _logger.LogDebug($"{nameof(request.AuthorizationToken)}: {request.AuthorizationToken}");
            _logger.LogDebug($"{nameof(request.MethodArn)}: {request.MethodArn}");

            return DoAuthentication(request);
        }
    }

    public APIGatewayCustomAuthorizerResponseV2 DoAuthentication(APIGatewayCustomAuthorizerRequestV2 input)
    {
        try
        {
            // Basic Token validation - Bearer tokens only
            var tokenType = input.AuthorizationToken.Split(' ')[0];
            var inputToken = input.AuthorizationToken.Split(' ')[1];

            if (!tokenType.Equals("Bearer"))
            {
                _logger.LogDebug("Bad Token Type");
                throw new UnauthorizedException();
            }

            var validatedPrincipalId = "";
            var validatedUserId = "";
            var validatedTenantId = "";

            // Read the raw token header to get kid
            var rawToken = _handler.ReadJwtToken(inputToken);
            var unvalidatedKid = rawToken.Header.Kid;
                
            // Get the matching key from the JWK list
            var signingKey = GetKey(unvalidatedKid);

            // Validate the token and get claims
            var validatedClaims = DecodeAndValidateToken(inputToken, signingKey);

            validatedTenantId = validatedClaims.Where(c => c.Type == "custom:tenantId")
                .DefaultIfEmpty(new Claim("", ""))
                .First().Value;
            validatedPrincipalId = validatedClaims.Where(c => c.Type == ClaimTypes.NameIdentifier)
                .DefaultIfEmpty(new Claim("", ""))
                .First().Value;

            if (validatedPrincipalId == "" || validatedTenantId == "")
                throw new UnauthorizedException();

            validatedUserId = "User|" + validatedPrincipalId;

            var tmp = input.MethodArn.Split(":");
            var api_gateway_arn_tmp = tmp[5].Split("/");
            var aws_account_id = tmp[4];

            var tenantIamPolicy = AuthManager.GenerateTenantPolicyDocument(aws_account_id, _awsRegion, validatedTenantId);


            var tenantRoleArn = $"arn:aws:iam::{aws_account_id}:role/blueprint-dotnet6-api-authorizer-access";

            var tenantSessionCredentials = _stsService.GetTenantSessionCredentials(tenantIamPolicy, tenantRoleArn).GetCredentials();

            // Setup the outputs we want to process inside our application
            var contextOutput = new APIGatewayCustomAuthorizerContextOutputV2();
            contextOutput["userId"] = validatedUserId;
            contextOutput["tenantId"] = validatedTenantId;
            contextOutput["accesskey"] = tenantSessionCredentials.AccessKey;
            contextOutput["secretkey"] = tenantSessionCredentials.SecretKey;
            contextOutput["sessiontoken"] = tenantSessionCredentials.Token;

            APIGatewayCustomAuthorizerPolicyV2 policy = new APIGatewayCustomAuthorizerPolicyV2
            {
                Version = "2012-10-17",
                Statement = new List<APIGatewayCustomAuthorizerPolicyV2.IAMPolicyStatement>()
            };

            policy.Statement.Add(new APIGatewayCustomAuthorizerPolicyV2.IAMPolicyStatement
            {
                Action = new HashSet<string>(new string[] { "execute-api:Invoke" }),
                Effect = "Allow",
                Resource = new HashSet<string>(new string[] { input.MethodArn })

            });

            _logger.LogDebug("Auth accepted");

            return new APIGatewayCustomAuthorizerResponseV2
            {
                PrincipalID = validatedPrincipalId,
                Context = contextOutput,
                PolicyDocument = policy
            };

        }
        catch (Exception ex)
        {
            if (ex is UnauthorizedException)
                throw;

            // log the exception and return a 401
            _logger.LogError($"Error occurred validating token: {ex.ToString()}");
            throw new UnauthorizedException();
        }
    }

    public IEnumerable<Claim> DecodeAndValidateToken(string token, RsaSecurityKey signingKey)
    {
        SecurityToken validatedToken;

        // Setup validation params
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = _validIssuerList,
            ValidateAudience = _validateAudience,
            ClockSkew = TimeSpan.FromMinutes(5),
            IssuerSigningKey = signingKey,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = _validateLifetime
        };

        // Validate the token and get claims
        var validatedClaims = _handler.ValidateToken(token, tokenValidationParameters,
            out validatedToken).Claims;

        return validatedClaims;
    }
    public virtual RsaSecurityKey GetKey(string unvalidatedKid)
    {
        // first check the jwkCache to see if the kid exists
        var theJwk = _jwkCache.Keys.FirstOrDefault(x => x.Kid.Equals(unvalidatedKid));
            
        // if it doesnt exist try refreshing the cache
        if (theJwk == null)
        {
            throw new UnauthorizedException();
        }

        return JwkToPem(theJwk);
    }

    public virtual RsaSecurityKey JwkToPem(JsonWebKey theJwk)
    {
        /* Create RSA from Elements in JWK */
        RSAParameters rsap = new RSAParameters
        {
            Modulus = Base64UrlTextEncoder.Decode(theJwk.N),
            Exponent = Base64UrlTextEncoder.Decode(theJwk.E),
        };
        RSA rsa = RSA.Create();
        rsa.ImportParameters(rsap);
        RsaSecurityKey rsakey = new RsaSecurityKey(rsa);

        return rsakey;
    }
}

