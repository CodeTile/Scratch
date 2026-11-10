# Complete authentication integration – FluentUI.Blazor v5 + ASP.NET Core Identity + Active Directory

Your starting point is now clear:

- Portal: FluentUI.Blazor v5 Identity template.
- Framework: .NET 10.
- Language: C# 14.
- Authentication: ASP.NET Core Identity.
- AD authentication: Windows Integrated Authentication using Negotiate.
- Business logic: Separate ASP.NET Core Web API using Controllers.
- Database: Shared application identity and business data, accessed through the appropriate server-side services.
- Authorisation: Multiple roles, permissions and service-account mappings.

I would preserve the Identity template rather than replace its authentication system.

The key design decision is to make ASP.NET Core Identity the central application identity system, with Negotiate providing an additional way to establish a user's domain identity.

This gives us one consistent application principal, one set of roles and permissions, and one portal authentication cookie regardless of how the user authenticated.

There is one important security distinction: a Windows-authenticated user who fails the approved AD group check must not be considered an AD-authenticated application user. If they subsequently use local credentials, they receive a local authentication identity.

## 1. How Identity and Negotiate will work together

There are three distinct authentication mechanisms in the portal:

| Scheme                 | Responsibility                                 |
| ---------------------- | ---------------------------------------------- |
| `Identity.Application` | Main portal authentication cookie              |
| `Identity.External`    | Reserved for external authentication workflows |
| `Windows`              | Temporary Negotiate authentication challenge   |

The Windows scheme will not become the default application authentication scheme.

This is important because making Negotiate the default would interfere with the normal Identity login experience.

### Authentication sequence

1. User opens portal

   ASP.NET Core Identity checks for an existing valid application cookie.
2. No existing session

   Display the login page with a Windows sign-in option and local username/password login.
3. Windows sign-in

   The browser is challenged using Negotiate. Windows validates the domain identity.
4. Validate AD group membership

   Check the authenticated user's approved global AD group SIDs.
5. Create or link application identity

   Resolve the AD user to an ASP.NET Core Identity user, preferably using the immutable AD object SID.
6. Issue Identity cookie

   Create the normal Identity application cookie with additional authentication claims.
7. Fallback

   If Windows authentication is unavailable or group membership is insufficient, leave the user on the local login page.

A local user authenticates using the existing Identity `SignInManager`.

An AD user authenticates through Negotiate and is then signed into the same Identity application cookie.

This means the rest of the portal does not need to know which authentication method was used unless it needs to enforce an AD-only policy.

### Important implementation detail

I would not automatically try Negotiate on every anonymous page request.

Instead, the login page should offer Windows authentication explicitly. This avoids unwanted browser authentication challenges and allows local login to remain accessible when AD is unavailable.

Negotiate is designed for Windows-domain intranet scenarios. Its deployment also needs consideration of Kerberos, NTLM, IIS/Kestrel configuration and reverse proxies.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://learn.microsoft.com\&sz=32)

Microsoft Learn+1



## 2. Identity user model

Keep the template's `ApplicationUser`, extending it with application-specific fields.

```
namespace MyProject.Portal.Data;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public string? ActiveDirectorySid { get; set; }

    public bool IsActiveDirectoryUser { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTimeOffset? LastSuccessfulLoginUtc { get; set; }

    public DateTimeOffset? LastPasswordChangeUtc { get; set; }

    public DateTimeOffset? AccountLockedUtc { get; set; }

    public string? AccountLockReason { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }
}
```

For AD users:

- `ActiveDirectorySid` stores the domain user's SID.
- `IsActiveDirectoryUser` is true.
- The application does not store an AD password.
- The application uses the SID to link the domain identity to the application account.

For local users:

- `ActiveDirectorySid` is null.
- `IsActiveDirectoryUser` is false.
- ASP.NET Core Identity manages the password hash.

I would also use Identity's `AspNetUserLogins` table to record the external Windows identity link, using a stable SID as the provider key. This avoids creating duplicate application users when usernames or display names change.

Do not use the username as the permanent AD identifier.

## 3. Claims model

Create a consistent claim model regardless of authentication method.

```
namespace MyProject.Contracts.Security;

public static class ApplicationClaimTypes
{
    public const string AuthenticationMethod =
        "authentication_method";

    public const string IsActiveDirectoryUser =
        "is_active_directory_user";

    public const string IsGlobalGroupMember =
        "is_global_group_member";

    public const string ApplicationUserId =
        "application_user_id";

    public const string SessionId =
        "application_session_id";
}
```

Example AD claims:

```
authentication_method = ActiveDirectory
is_active_directory_user = true
is_global_group_member = true
application_user_id = 12345
```

Example local claims:

```
authentication_method = Local
is_active_directory_user = false
is_global_group_member = false
application_user_id = 12345
```

I recommend that the application also maintain an immutable authentication-method record in the database. Claims should be treated as a snapshot of the trusted login decision, not as a substitute for current account status.

## 4. Portal authentication configuration

The following is the proposed configuration for the portal's `Program.cs`.

It extends Identity rather than replacing it.

```

using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyProject.Portal.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection is not configured.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services
    .AddAuthentication()
    .AddNegotiate("Windows");

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "__Host-MyProject.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Path = "/";

    options.ExpireTimeSpan = TimeSpan.FromMinutes(
        builder.Configuration.GetValue<int>(
            "Authentication:Session:IdleTimeoutMinutes"));

    options.SlidingExpiration = true;

    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActiveDirectoryOnly", policy =>
    {
        policy.RequireAuthenticatedUser();

        policy.RequireClaim(
            ApplicationClaimTypes.IsActiveDirectoryUser,
            "true");

        policy.RequireClaim(
            ApplicationClaimTypes.IsGlobalGroupMember,
            "true");
    });

    options.AddPolicy("Administrator", policy =>
    {
        policy.RequireAuthenticatedUser();

        policy.RequireClaim(
            ApplicationClaimTypes.IsActiveDirectoryUser,
            "true");

        policy.RequireRole("Administrator");
    });
});

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

```

This is the authentication foundation, not the complete application startup file. The template's existing registrations for Identity endpoints, FluentUI services, database initialisation and other middleware must be retained. The configured timeout must also be startup-validated rather than silently defaulting to zero.

For the seven-minute inactivity requirement, we will additionally implement session activity tracking. Cookie sliding expiration alone does not accurately measure user activity inside an existing SignalR connection.

## 5. Windows authentication integration service

Keep the Windows authentication logic out of Razor components.

Create:

```
Authentication/
    IWindowsAuthenticationService.cs
    WindowsAuthenticationService.cs
    IActiveDirectoryGroupService.cs
    ActiveDirectoryGroupService.cs
    IApplicationUserProvisioningService.cs
    ApplicationUserProvisioningService.cs
```

The responsibilities are:

| Service                            | Responsibility                        |
| ---------------------------------- | ------------------------------------- |
| WindowsAuthenticationService       | Validate Windows principal            |
| ActiveDirectoryGroupService        | Check approved AD groups              |
| ApplicationUserProvisioningService | Find or link Identity user            |
| AuthenticationAuditService         | Record login result                   |
| IdentitySessionService             | Issue and revoke application sessions |

The Windows authentication endpoint should:

1. Call `AuthenticateAsync("Windows")`.
2. Challenge the Windows scheme if no authenticated principal exists.
3. Obtain the user's SID from the validated Windows principal.
4. Check the user's approved global groups.
5. Resolve the corresponding Identity user.
6. Create the application cookie using `SignInManager.SignInWithClaimsAsync`.
7. Redirect to the requested local return URL.

Conceptually:

```
await signInManager.SignInWithClaimsAsync(
    applicationUser,
    isPersistent: false,
    additionalClaims:
    [
        new Claim(
            ApplicationClaimTypes.AuthenticationMethod,
            "ActiveDirectory"),

        new Claim(
            ApplicationClaimTypes.IsActiveDirectoryUser,
            "true"),

        new Claim(
            ApplicationClaimTypes.IsGlobalGroupMember,
            "true")
    ]);
```

This is the crucial integration point: Negotiate authenticates the Windows identity; Identity creates the portal session.

If the user is not in an approved group, do not call `SignInWithClaimsAsync`. Audit the result and redirect to the local login page.

If the user is not already registered, I recommend controlled just-in-time provisioning for approved AD users. Do not automatically provision every domain user.

## 6. Local Identity login

Keep the existing template login page and its Identity functionality.

The login page should provide:

# MyProject

Sign in to your account

Windows domain account

Sign in using your organisation's Windows account.

Sign in with Windows

Username

Enter username

Password

Enter password

Forgot your password?

Sign in

The diagram is illustrative of the intended layout.

The local login continues to use:

```
var result = await signInManager.PasswordSignInAsync(
    username,
    password,
    isPersistent: false,
    lockoutOnFailure: true);
```

After successful authentication, additional validation is required:

- Account enabled.
- Account not locked.
- Account not expired due to inactivity.
- Account authentication method permits local authentication.
- Application roles and permissions loaded.

The login audit should record both successful and unsuccessful attempts.

I would also disable public registration unless you specifically require it. Local accounts should be provisioned by an administrator.

## 7. API authentication – separate security boundary

The portal and API will have separate authentication responsibilities.

Portal

Identity authenticated

Browser authentication cookie.

Portal token service

Validates the current Identity session and issues a short-lived JWT signed with a private key.

API

Two credentials required

- API key
- JWT bearer token

API authorisation

Resolves the user's current permissions and service-account entitlement before executing business logic.

The API should not accept the portal's Identity cookie. It should trust the JWT issuer and validate the token's signature, issuer, audience and expiration.

I recommend:

| Setting           | Value                        |
| ----------------- | ---------------------------- |
| JWT issuer        | MyProject.Portal             |
| JWT audience      | MyProject.Api                |
| JWT lifetime      | 5 minutes                    |
| Signing algorithm | RSA SHA-256                  |
| API key           | Separate high-entropy secret |
| API key location  | `X-API-Key`                  |
| JWT location      | `Authorization: Bearer`      |

The portal holds the signing private key; the API holds only the corresponding public key.

The API key identifies the calling application. The JWT identifies the user.

Do not issue an API token based solely on a username or user ID supplied by the browser. The token service must derive the identity from the authenticated server-side Identity principal and validate the session.

The API should also independently enforce AD-only administration policies. It should not blindly trust a role claim to grant administrator access.

## 8. API controller structure

```
MyProject.Api/
│
├── Controllers/
│   ├── AuthenticationController.cs
│   ├── ProfileController.cs
│   ├── UsersController.cs
│   ├── RolesController.cs
│   ├── PermissionsController.cs
│   ├── ReportsController.cs
│   ├── FinanceController.cs
│   └── AdministrationController.cs
│
├── Authentication/
│   ├── ApiKeyAuthenticationHandler.cs
│   ├── JwtConfiguration.cs
│   └── JwtValidationService.cs
│
├── Authorization/
│   ├── PermissionRequirement.cs
│   ├── PermissionAuthorizationHandler.cs
│   └── ApplicationPolicies.cs
│
└── Program.cs
```

Example controller:

```
namespace MyProject.Api.Controllers;

[ApiController]
[Route("api/finance")]
[Authorize(Policy = "Finance")]
public sealed class FinanceController(
    IFinanceService financeService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var result = await financeService.GetFinanceAsync(
            User,
            cancellationToken);

        return Ok(result);
    }
}
```

The `Finance` policy should be evaluated against current application permissions, not just a UI visibility flag.

## 9. FluentUI Razor authorisation

The template's existing `AuthorizeRouteView`, authentication state and Identity components should be retained.

For example, an AD-only administration page:

```
@page "/administration"
@attribute [Authorize(Policy = "Administrator")]

<PageTitle>Administration</PageTitle>

<FluentStack Orientation="Orientation.Vertical">
    <FluentLabel Typo="Typography.PaneHeader">
        Administration
    </FluentLabel>

    <FluentTextField Label="User name" />

    <FluentButton Appearance="Appearance.Accent">
        Search users
    </FluentButton>
</FluentStack>
```

For conditional UI:

```
<AuthorizeView Policy="Finance">
    <Authorized>
        <FluentButton Appearance="Appearance.Accent"
                      OnClick="OpenFinance">
            Finance
        </FluentButton>
    </Authorized>
</AuthorizeView>
```

I recommend an `IUserAccessService` that provides a common interface for the entire portal:

```
namespace MyProject.Portal.Authorization;

public interface IUserAccessService
{
    Task<bool> HasPermissionAsync(
        string permission,
        CancellationToken cancellationToken = default);

    Task<bool> HasRoleAsync(
        string role,
        CancellationToken cancellationToken = default);

    Task<bool> IsActiveDirectoryUserAsync(
        CancellationToken cancellationToken = default);

    Task<bool> IsAdministratorAsync(
        CancellationToken cancellationToken = default);

    Task<string?> GetServiceAccountReferenceAsync(
        string page,
        CancellationToken cancellationToken = default);
}
```

This service can be used in Razor components, navigation menus and page-level logic.

The API must independently enforce the same permissions. Hiding a menu item is never security enforcement.

## 10. Session management

I would implement a dedicated server-side session record.

```
UserSessions
    SessionId
    UserId
    AuthenticationMethod
    CreatedUtc
    LastActivityUtc
    ExpiresUtc
    RevokedUtc
    RevocationReason
    IpAddress
```

The configuration:

```
{
  "Authentication": {
    "Session": {
      "IdleTimeoutMinutes": 7,
      "SlidingExpiration": true,
      "ActivityHeartbeatMinutes": 2
    },
    "LocalAccounts": {
      "InactivityLockMonths": 3
    }
  }
}
```

The two-minute heartbeat is a maximum interval, not an unconditional background activity signal. It should only be sent while the user is actually active.

The session service should:

1. Check the session on authenticated requests.
2. Update `LastActivityUtc` on genuine user activity.
3. Calculate expiry using `TimeProvider`.
4. Revoke the session on logout.
5. Reject API token issuance for expired or revoked sessions.
6. Revalidate session state when the user reconnects to Blazor Server.

This is especially important with Interactive Server because a long-lived SignalR connection does not automatically mean the authentication cookie is refreshed.

## 11. Revised Mermaid security diagram

This is the authentication design I would now implement.

flowchart TD
    A[User opens FluentUI Portal] --> B{Valid Identity cookie?}

    B -->|Yes| C[Validate application session]
    B -->|No| D[Identity Login Page]

    D --> E{Windows sign-in selected?}

    E -->|Yes| F[Negotiate Authentication]
    E -->|No| L[Identity Username Password]

    F --> G{Windows identity validated?}

    G -->|No| L
    G -->|Yes| H[Resolve AD SID]

    H --> I{Approved global AD group?}

    I -->|No| L
    I -->|Yes| J[Find or provision Identity user]

    J --> K[Issue Identity application cookie]

    L --> M[Identity Password Validation]

    M --> N{Account valid and active?}

    N -->|No| O[Audit failed login]
    O --> D

    N -->|Yes| P[Issue Identity application cookie]

    K --> C
    P --> C

    C --> Q[Load roles and permissions]
    Q --> R{AD-only operation?}

    R -->|Yes| S{AD authentication claim?}
    S -->|No| T[Access Denied]
    S -->|Yes| U[Authorise permission]

    R -->|No| U

    U --> V{Permission granted?}
    V -->|No| T
    V -->|Yes| W[Portal operation]

    W --> X[Request API JWT]
    X --> Y[API key validation]
    Y --> Z[JWT validation]
    Z --> AA[API permission validation]
    AA --> AB[Business Logic]
	
## 12. Important deployment considerations

There are a few things I would build into the architecture from day one.

Active Directory availability

Negotiate must be hosted in an environment where Windows authentication is configured correctly. With IIS, enable Windows Authentication and retain anonymous access where necessary for the local login route. With Kestrel, configure the Negotiate handler and domain environment appropriately.

Kerberos

Use Kerberos where possible. Configure the appropriate HTTP SPN for the service identity. Avoid relying on NTLM as the preferred production authentication mechanism.

Reverse proxies

Negotiate has connection-affinity requirements with proxies. Do not put it behind a load balancer or reverse proxy without confirming the documented connection handling requirements.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://learn.microsoft.com\&sz=32)

Microsoft Learn+1



Blazor authentication

Keep Identity authentication endpoints using static SSR, as intended by the template. The template already provides the relevant Identity components and authentication infrastructure.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://learn.microsoft.com\&sz=32)

Microsoft Learn+1



Multiple portal instances

If the portal runs on multiple servers, use shared Data Protection keys and a shared session store. Otherwise, cookies and session validation can behave inconsistently between instances.

## 13. Final project configuration decisions

I would lock in these architectural rules before writing the implementation.

| Area                   | Decision                              |
| ---------------------- | ------------------------------------- |
| Portal authentication  | Existing ASP.NET Core Identity        |
| Windows authentication | Additional named Negotiate scheme     |
| Local authentication   | Existing Identity `SignInManager`     |
| AD user mapping        | Immutable AD SID                      |
| Portal session         | Identity application cookie           |
| Session expiry         | Seven-minute inactivity               |
| API authentication     | API key AND JWT                       |
| JWT signing            | RSA private/public key pair           |
| JWT lifetime           | Five minutes                          |
| User permissions       | Database-driven                       |
| Admin access           | AD authenticated + Administrator role |
| Business logic         | Separate API                          |
| Service accounts       | Resolved inside API                   |
| Password reset         | Identity token-based reset            |
| Login audit            | Successful and unsuccessful           |
| Account inactivity     | Three months, configurable            |
| UI                     | FluentUI.Blazor v5                    |
| Testing                | xUnit + integration tests             |

### One correction to the earlier startup example

Because you are starting from the Identity template, do not replace its `AddIdentityCore` / `AddIdentityCookies` registrations with a new `AddIdentity` registration.

Keep the generated template configuration and extend its authentication builder with the named Negotiate scheme. This preserves the template's cookie configuration, Identity endpoints, sign-in manager and authentication state implementation.

Also, the API should enforce API-key validation as an independent mandatory check before allowing JWT-authorised requests. Do not configure the two authentication schemes as an either/or authorisation policy.

### Reference documentation

These are the Microsoft references I would use when implementing the actual code:

1. [ASP.NET Core Identity – .NET 10](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?tabs=visual-studio\&view=aspnetcore-10.0\).
2. [Windows Authentication and Negotiate – .NET 10](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/windowsauth?view=aspnetcore-10.0).
3. [Blazor authentication and authorisation – .NET 10](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0).

## 14. Recommended implementation sequence

I would now build the solution in the following order, preserving your actual FluentUI Identity template.

## Implementation roadmap

1. Foundation

   Create Domain, Contracts, Application and Infrastructure projects. Extend the existing Identity user and database context.
2. Hybrid authentication

   Implement the Windows Negotiate endpoint, AD SID mapping, group validation and Identity cookie sign-in.
3. Local authentication

   Retain Identity login, password change, password reset, lockout and profile functionality.
4. Session management

   Implement seven-minute inactivity, activity tracking, session revocation and logout.
5. API security

   Build API key validation, JWT issuance, JWT validation and mandatory API authorisation.
6. Roles and permissions

   Implement multiple roles, individual permissions, page access and service account mappings.
7. FluentUI pages

   Implement login, dashboard, user profile, administration, role management and audit pages.
8. Testing

   Add unit tests, authentication integration tests, API security tests and session expiry tests.

My recommendation: Keep the FluentUI Identity template as the foundation of the portal, rather than trying to turn the API into an Identity server. The portal owns the interactive authentication experience; the API owns business security and data access. This gives us a clean separation without fighting the framework's existing authentication architecture.