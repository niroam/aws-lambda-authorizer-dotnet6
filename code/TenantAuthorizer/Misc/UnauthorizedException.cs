namespace TenantAuthorizer;
public class UnauthorizedException : System.Exception
{
    public UnauthorizedException() : base("Unauthorized")
    {
    }
}

