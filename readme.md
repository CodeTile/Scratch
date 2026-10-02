I can format it as a plain `.txt` file, but Canvas/file creation isn't available in this session.

 Below is the **plain-text file content**. You can copy everything inside the block into:

```
MyAdApp-Setup.txt
```

```
============================================================
.NET 10 BLAZOR + FLUENT UI V5
WINDOWS ACTIVE DIRECTORY AUTHENTICATION
IIS PRODUCTION SETUP
============================================================

PURPOSE
=======

Build a .NET 10 Blazor Web App using:

    .NET 10
    Blazor Web App
    Interactive Server
    Fluent UI Blazor v5
    Windows Authentication
    Active Directory
    IIS for production

The application will:

    1. Automatically authenticate users using Windows Authentication.
    2. Display the logged-in user's Windows identity.
    3. Display Active Directory information.
    4. Display AD groups.
    5. Display claims.
    6. Allow pages to be protected using AD group membership.
    7. Allow Fluent UI controls to be shown based on authorization.
    8. Keep actual authorization enforced on the server.
    9. Be suitable for deployment to IIS.

============================================================
1. CREATE THE PROJECT
============================================================

Run:

    dotnet new blazor -n MyAdApp

    cd MyAdApp

Install Fluent UI:

    dotnet add package Microsoft.FluentUI.AspNetCore.Components

Restore:

    dotnet restore

============================================================
2. PROJECT STRUCTURE
============================================================

The project should eventually contain:

    MyAdApp/
    |
    +-- Components/
    |   |
    |   +-- Layout/
    |   |
    |   +-- Pages/
    |       |
    |       +-- Home.razor
    |       +-- Identity.razor
    |       +-- Claims.razor
    |       +-- Admin.razor
    |
    +-- Models/
    |   |
    |   +-- UserContext.cs
    |   +-- UserClaim.cs
    |   +-- ActiveDirectoryUser.cs
    |
    +-- Services/
    |   |
    |   +-- IUserContextService.cs
    |   +-- UserContextService.cs
    |   +-- IActiveDirectoryService.cs
    |   +-- ActiveDirectoryService.cs
    |
    +-- Program.cs
    +-- appsettings.json
    +-- appsettings.Production.json
    +-- MyAdApp.csproj

============================================================
3. PROGRAM.CS
============================================================

Replace Program.cs with:

using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.FluentUI.AspNetCore.Components;
using MyAdApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------
// Razor Components / Blazor
// ----------------------------------------------------------

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// ----------------------------------------------------------
// Fluent UI
// ----------------------------------------------------------

builder.Services.AddFluentUIComponents();

// ----------------------------------------------------------
// Windows Authentication
// ----------------------------------------------------------

builder.Services.AddAuthentication(
    IISDefaults.AuthenticationScheme);

// ----------------------------------------------------------
// Authorization
// ----------------------------------------------------------

builder.Services.AddAuthorization(options =>
{
    // Replace this example SID with the actual SID
    // of your AD application administrators group.

    const string applicationAdminSid =
        "S-1-5-21-0000000000-0000000000-0000000000-0000";

    options.AddPolicy(
        "ApplicationAdmin",
        policy =>
        {
            policy.RequireClaim(
                System.Security.Claims.ClaimTypes.GroupSid,
                applicationAdminSid);
        });
});

// ----------------------------------------------------------
// HTTP context
// ----------------------------------------------------------

builder.Services.AddHttpContextAccessor();

// ----------------------------------------------------------
// Application services
// ----------------------------------------------------------

builder.Services.AddScoped<IUserContextService,
    UserContextService>();

builder.Services.AddScoped<IActiveDirectoryService,
    ActiveDirectoryService>();

var app = builder.Build();

// ----------------------------------------------------------
// HTTP pipeline
// ----------------------------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.UseAuthentication();

app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

IMPORTANT:

If the generated .NET project already contains
authentication configuration, do not register the same
authentication scheme twice.

Keep one authoritative authentication configuration.

============================================================
4. MODELS/USERCLAIM.CS
============================================================

Create:

    Models/UserClaim.cs

Content:

using System;

namespace MyAdApp.Models;

public sealed class UserClaim
{
    public string Type { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}

============================================================
5. MODELS/USERCONTEXT.CS
============================================================

Create:

    Models/UserContext.cs

Content:

namespace MyAdApp.Models;

public sealed class UserContext
{
    public bool IsAuthenticated { get; init; }

    /// <summary>
    /// Windows logon name.
    /// Example: MYDOMAIN\jsmith
    /// </summary>
    public string? LogonName { get; init; }

    /// <summary>
    /// Active Directory displayName.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Active Directory mail attribute.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Active Directory userPrincipalName.
    /// </summary>
    public string? UserPrincipalName { get; init; }

    /// <summary>
    /// Active Directory sAMAccountName.
    /// </summary>
    public string? SamAccountName { get; init; }

    /// <summary>
    /// Active Directory department.
    /// </summary>
    public string? Department { get; init; }

    /// <summary>
    /// Active Directory jobTitle.
    /// </summary>
    public string? JobTitle { get; init; }

    /// <summary>
    /// Active Directory / Windows group names.
    /// </summary>
    public IReadOnlyList<string> Groups { get; init; } = [];

    /// <summary>
    /// Claims supplied by ASP.NET Core authentication.
    /// </summary>
    public IReadOnlyList<UserClaim> Claims { get; init; } = [];
}

============================================================
6. MODELS/ACTIVEDIRECTORYUSER.CS
============================================================

Create:

    Models/ActiveDirectoryUser.cs

Content:

namespace MyAdApp.Models;

public sealed class ActiveDirectoryUser
{
    public string? DisplayName { get; init; }

    public string? Email { get; init; }

    public string? UserPrincipalName { get; init; }

    public string? SamAccountName { get; init; }

    public string? Department { get; init; }

    public string? JobTitle { get; init; }

    public IReadOnlyList<string> Groups { get; init; } = [];
}

============================================================
7. SERVICES/IUSERCONTEXTSERVICE.CS
============================================================

Create:

    Services/IUserContextService.cs

Content:

using MyAdApp.Models;

namespace MyAdApp.Services;

public interface IUserContextService
{
    Task<UserContext> GetCurrentUserAsync(
        CancellationToken cancellationToken = default);
}

============================================================
8. SERVICES/IACTIVEDIRECTORYSERVICE.CS
============================================================

Create:

    Services/IActiveDirectoryService.cs

Content:

using MyAdApp.Models;

namespace MyAdApp.Services;

public interface IActiveDirectoryService
{
    Task<ActiveDirectoryUser?> FindUserAsync(
        string logonName,
        CancellationToken cancellationToken = default);
}

============================================================
9. SERVICES/USERCONTEXTSERVICE.CS
============================================================

Create:

    Services/UserContextService.cs

Content:

using System.Security.Claims;
using MyAdApp.Models;

namespace MyAdApp.Services;

public sealed class UserContextService
    : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly IActiveDirectoryService
        _activeDirectoryService;

    public UserContextService(
        IHttpContextAccessor httpContextAccessor,
        IActiveDirectoryService activeDirectoryService)
    {
        _httpContextAccessor = httpContextAccessor;
        _activeDirectoryService = activeDirectoryService;
    }

    public async Task<UserContext> GetCurrentUserAsync(
        CancellationToken cancellationToken = default)
    {
        var httpContext =
            _httpContextAccessor.HttpContext;

        var user = httpContext?.User;

        if (user is null ||
            user.Identity?.IsAuthenticated != true)
        {
            return new UserContext
            {
                IsAuthenticated = false
            };
        }

        var logonName =
            user.Identity?.Name;

        var claims =
            user.Claims
                .Select(claim => new UserClaim
                {
                    Type = claim.Type,
                    Value = claim.Value
                })
                .ToList();

        ActiveDirectoryUser? adUser = null;

        if (!string.IsNullOrWhiteSpace(logonName))
        {
            adUser =
                await _activeDirectoryService
                    .FindUserAsync(
                        logonName,
                        cancellationToken);
        }

        return new UserContext
        {
            IsAuthenticated = true,

            LogonName = logonName,

            DisplayName =
                adUser?.DisplayName
                ?? user.FindFirst(ClaimTypes.Name)?.Value,

            Email =
                adUser?.Email
                ?? user.FindFirst(ClaimTypes.Email)?.Value,

            UserPrincipalName =
                adUser?.UserPrincipalName,

            SamAccountName =
                adUser?.SamAccountName,

            Department =
                adUser?.Department,

            JobTitle =
                adUser?.JobTitle,

            Groups =
                adUser?.Groups
                ?? [],

            Claims = claims
        };
    }
}

============================================================
10. SERVICES/ACTIVEDIRECTORYSERVICE.CS
============================================================

Create:

    Services/ActiveDirectoryService.cs

For the initial implementation, use:

using MyAdApp.Models;

namespace MyAdApp.Services;

public sealed class ActiveDirectoryService
    : IActiveDirectoryService
{
    private readonly ILogger<ActiveDirectoryService>
        _logger;

    public ActiveDirectoryService(
        ILogger<ActiveDirectoryService> logger)
    {
        _logger = logger;
    }

    public Task<ActiveDirectoryUser?> FindUserAsync(
        string logonName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Active Directory lookup requested for {LogonName}",
            logonName);

        // Active Directory / LDAP implementation will be
        // added after Windows Authentication has been verified.

        return Task.FromResult<ActiveDirectoryUser?>(null);
    }
}

IMPORTANT:

Do not guess the LDAP implementation yet.

First prove that Windows Authentication works.

The actual LDAP implementation depends on the AD environment.

============================================================
11. COMPONENTS/PAGES/IDENTITY.RAZOR
============================================================

Create:

    Components/Pages/Identity.razor

Content:

@page "/identity"

@using System.Security.Claims

@inject AuthenticationStateProvider
    AuthenticationStateProvider

<PageTitle>Windows Identity</PageTitle>

<h1>Windows Identity</h1>

@if (_user is null)
{
    <p>Loading...</p>
}
else
{
    <FluentCard Style="padding:20px">

        <p>
            <strong>Authenticated:</strong>
            @_user.Identity?.IsAuthenticated
        </p>

        <p>
            <strong>Name:</strong>
            @_user.Identity?.Name
        </p>

        <p>
            <strong>Authentication type:</strong>
            @_user.Identity?.AuthenticationType
        </p>

    </FluentCard>
}

@code
{
    private ClaimsPrincipal? _user;

    protected override async Task OnInitializedAsync()
    {
        var state =
            await AuthenticationStateProvider
                .GetAuthenticationStateAsync();

        _user = state.User;
    }
}

============================================================
12. COMPONENTS/PAGES/CLAIMS.RAZOR
============================================================

Create:

    Components/Pages/Claims.razor

Content:

@page "/claims"

@using System.Security.Claims
@using Microsoft.AspNetCore.Authorization

@attribute [Authorize]

@inject AuthenticationStateProvider
    AuthenticationStateProvider

<PageTitle>Claims</PageTitle>

<h1>Claims</h1>

@if (_user is null)
{
    <p>Loading...</p>
}
else
{
    <FluentDataGrid
        Items="_claims.AsQueryable()">

        <PropertyColumn
            Property="@(x => x.Type)"
            Title="Type" />

        <PropertyColumn
            Property="@(x => x.Value)"
            Title="Value" />

    </FluentDataGrid>
}

@code
{
    private ClaimsPrincipal? _user;

    private List<Claim> _claims = [];

    protected override async Task OnInitializedAsync()
    {
        var state =
            await AuthenticationStateProvider
                .GetAuthenticationStateAsync();

        _user = state.User;

        _claims = _user.Claims.ToList();
    }
}

============================================================
13. COMPONENTS/PAGES/HOME.RAZOR
============================================================

Replace the generated home page with:

@page "/"

@using MyAdApp.Models
@using MyAdApp.Services

@inject IUserContextService UserContextService

<PageTitle>Home</PageTitle>

<h1>Welcome</h1>

@if (_user is null)
{
    <p>Loading...</p>
}
else if (!_user.IsAuthenticated)
{
    <FluentMessageBar Intent="MessageIntent.Error">
        You are not authenticated.
    </FluentMessageBar>
}
else
{
    <FluentCard Style="padding:20px">

        <h2>User</h2>

        <p>
            <strong>Display name:</strong>
            @_user.DisplayName
        </p>

        <p>
            <strong>Logon name:</strong>
            @_user.LogonName
        </p>

        <p>
            <strong>Email:</strong>
            @_user.Email
        </p>

        <p>
            <strong>User principal name:</strong>
            @_user.UserPrincipalName
        </p>

        <p>
            <strong>SAM account:</strong>
            @_user.SamAccountName
        </p>

        <p>
            <strong>Department:</strong>
            @_user.Department
        </p>

        <p>
            <strong>Job title:</strong>
            @_user.JobTitle
        </p>

    </FluentCard>

    <br />

    <FluentCard Style="padding:20px">

        <h2>Active Directory Groups</h2>

        @if (_user.Groups.Count == 0)
        {
            <p>No AD groups returned.</p>
        }
        else
        {
            @foreach (var group in _user.Groups)
            {
                <FluentBadge>
                    @group
                </FluentBadge>
            }
        }

    </FluentCard>

    <br />

    <FluentCard Style="padding:20px">

        <h2>Claims</h2>

        @if (_user.Claims.Count == 0)
        {
            <p>No claims returned.</p>
        }
        else
        {
            <FluentDataGrid
                Items="@_user.Claims.AsQueryable()">

                <PropertyColumn
                    Property="@(x => x.Type)"
                    Title="Type" />

                <PropertyColumn
                    Property="@(x => x.Value)"
                    Title="Value" />

            </FluentDataGrid>
        }

    </FluentCard>
}

@code
{
    private UserContext? _user;

    protected override async Task OnInitializedAsync()
    {
        _user =
            await UserContextService
                .GetCurrentUserAsync();
    }
}

============================================================
14. COMPONENTS/PAGES/ADMIN.RAZOR
============================================================

Create:

    Components/Pages/Admin.razor

Content:

@page "/admin"

@using Microsoft.AspNetCore.Authorization

@attribute [Authorize(
    Policy = "ApplicationAdmin")]

<PageTitle>Administration</PageTitle>

<h1>Administration</h1>

<FluentMessageBar Intent="MessageIntent.Success">
    You are authorized to access this page.
</FluentMessageBar>

============================================================
15. AUTHORIZEVIEW
============================================================

Navigation or UI controls can use:

<AuthorizeView Policy="ApplicationAdmin">

    <Authorized>

        <FluentNavLink Href="/admin">
            Administration
        </FluentNavLink>

    </Authorized>

</AuthorizeView>

IMPORTANT:

AuthorizeView only controls the UI.

It is NOT the security boundary.

The actual page must still use:

@attribute [Authorize(
    Policy = "ApplicationAdmin")]

============================================================
16. IIS CONFIGURATION
============================================================

In IIS Manager:

    Sites
        -> MyAdApp
            -> Authentication

Set:

    Windows Authentication       ENABLED
    Anonymous Authentication     DISABLED

Do not create a username/password login page.

The Windows account should be supplied by IIS.

============================================================
17. IIS APPLICATION POOL
============================================================

Create a dedicated application pool:

    MyAdAppPool

Set:

    .NET CLR Version
        No Managed Code

The application is ASP.NET Core.

It is not an old ASP.NET Framework application.

For the initial setup:

    Identity
        ApplicationPoolIdentity

If the application later needs a domain service account
for AD access, configure that deliberately.

Do not automatically give the application a highly
privileged domain account.

============================================================
18. HTTPS
============================================================

Configure an HTTPS binding.

Example:

    https://myapp.mycompany.local

Use a valid TLS certificate.

Production should use HTTPS.

============================================================
19. FIRST TEST
============================================================

Do NOT implement LDAP first.

First test:

    IIS
      |
      v
    Windows Authentication
      |
      v
    ClaimsPrincipal
      |
      v
    Blazor

Browse to:

    /identity

Expected result:

    Authenticated: True

    Name:
        MYDOMAIN\jsmith

    Authentication type:
        Negotiate

The exact authentication type can vary.

============================================================
20. SECOND TEST
============================================================

Browse to:

    /claims

Inspect the actual claims.

Possible claims include:

    Name
    GroupSid
    Other Windows claims

For example:

    http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name

        MYDOMAIN\jsmith

and:

    http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid

        S-1-5-21-...

Do NOT assume that your environment will expose exactly
these claims.

Inspect the real claims first.

============================================================
21. THIRD TEST
============================================================

Browse to:

    /

Initially ActiveDirectoryService returns null.

Therefore you may see:

    Display name:
        MYDOMAIN\jsmith

    Logon name:
        MYDOMAIN\jsmith

    Email:
        blank

    User principal name:
        blank

    SAM account:
        blank

    Department:
        blank

    Job title:
        blank

This is expected.

The purpose of this stage is to prove:

    IIS
      |
      v
    Windows Authentication
      |
      v
    ClaimsPrincipal
      |
      v
    UserContextService
      |
      v
    Blazor

============================================================
22. ACTIVE DIRECTORY LOOKUP
============================================================

Only after Windows Authentication works should
ActiveDirectoryService be implemented.

The final architecture becomes:

                    IIS
                     |
                     | Windows Authentication
                     v
              ClaimsPrincipal
                     |
                     v
             UserContextService
                     |
             +-------+-------+
             |               |
             v               v
          Claims       ActiveDirectoryService
                              |
                              v
                       Active Directory
                              |
                              v
                    ActiveDirectoryUser
                              |
                              v
                         UserContext
                              |
                              v
                         Blazor UI

The ActiveDirectoryService should retrieve fields such as:

    displayName
    mail
    userPrincipalName
    sAMAccountName
    department
    title
    group membership

============================================================
23. ACTIVE DIRECTORY GROUPS
============================================================

There are several possible sources of group membership.

Possible source:

    WindowsIdentity.Groups

Possible source:

    GroupSid claims

Possible source:

    Active Directory LDAP

Do not assume that all environments expose groups identically.

Inspect the claims and Windows identity first.

============================================================
24. GROUP SID AUTHORIZATION
============================================================

Once the actual AD group SID has been identified:

    const string applicationAdminSid =
        "S-1-5-21-...";

Configure:

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(
            "ApplicationAdmin",
            policy =>
            {
                policy.RequireClaim(
                    ClaimTypes.GroupSid,
                    applicationAdminSid);
            });
    });

Use the actual SID.

Do not copy the example SID into production.

============================================================
25. PROTECT A PAGE
============================================================

Example:

    @page "/admin"

    @attribute [Authorize(
        Policy = "ApplicationAdmin")]

Users who do not satisfy the policy must not be allowed
to access the protected page.

============================================================
26. PROTECT SERVER OPERATIONS
============================================================

Never rely on:

    Hidden buttons
    Disabled buttons
    JavaScript
    Local storage
    Query strings
    Client-side variables

for security.

Every sensitive server operation must enforce authorization.

The model is:

    User clicks operation
            |
            v
    Server operation
            |
            v
    Authorization check
            |
       +----+----+
       |         |
       v         v
    Denied    Authorized
       |         |
       v         v
     Reject    Perform

============================================================
27. USER CONTEXT RESPONSIBILITIES
============================================================

UserContext represents application-level user information.

It contains:

    IsAuthenticated
    LogonName
    DisplayName
    Email
    UserPrincipalName
    SamAccountName
    Department
    JobTitle
    Groups
    Claims

It is NOT the authentication mechanism.

The authenticated identity remains:

    ClaimsPrincipal

============================================================
28. AUTHENTICATION VS AUTHORIZATION
============================================================

Authentication asks:

    Who is this user?

Authorization asks:

    Is this user allowed to perform this operation?

UI asks:

    What should the user see?

Keep these responsibilities separate.

============================================================
29. CONFIGURATION
============================================================

appsettings.json:

{
  "ActiveDirectory": {
    "Domain": "mycompany.local",
    "LdapPath": "LDAP://DC=mycompany,DC=local"
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}

Production-specific configuration can be placed in:

    appsettings.Production.json

Do not store passwords or other secrets in source control.

============================================================
30. ACTIVE DIRECTORY SERVICE ACCOUNT
============================================================

If LDAP queries are required, determine whether the
application should use:

    ApplicationPoolIdentity

or:

    Dedicated domain service account

Do not automatically use:

    Domain Administrator

or another highly privileged account.

Use the minimum permissions required for the lookup.

============================================================
31. KERBEROS
============================================================

Windows Authentication can use:

    Kerberos
    NTLM

Kerberos should normally be preferred when the IIS/AD
environment is configured appropriately.

If Kerberos is required, verify:

    DNS
    IIS hostname
    Service account
    SPN
    Active Directory configuration

Example SPN:

    HTTP/myapp.mycompany.local

SPN configuration should be performed by the appropriate
AD/IIS administrator.

============================================================
32. IIS SERVER
============================================================

Verify:

    IIS server is appropriately joined to the domain.

    DNS is correct.

    Domain controllers are reachable.

    Domain trust is functioning.

    Windows Authentication is enabled.

    Anonymous Authentication is disabled.

============================================================
33. CACHING
============================================================

AD information can potentially be cached.

Example:

    User
      |
      v
    UserContextService
      |
      v
    Active Directory
      |
      v
    Cached UserContext

However, authorization-related caching requires care.

If removing a user from an AD group must immediately revoke
access, do not rely on a long-lived authorization cache.

============================================================
34. DIAGNOSTIC PAGES
============================================================

During development:

    /identity
    /claims

are useful.

Do not expose detailed claims or sensitive AD information
to unauthenticated users in production.

Protect diagnostic pages with authorization.

For example:

    @attribute [Authorize]

or preferably an administrator policy.

============================================================
35. SECURITY MODEL
============================================================

The final architecture should be:

                    Browser
                       |
                       |
                 Windows Auth
                       |
                       v
                      IIS
                       |
                       v
                ASP.NET Core
                       |
                       v
                ClaimsPrincipal
                       |
             +---------+---------+
             |                   |
             v                   v
        Authorization       UserContextService
          Policies                  |
                                    v
                         ActiveDirectoryService
                                    |
                                    v
                           Active Directory
                                    |
                                    v
                              UserContext
                                    |
                                    v
                             Blazor / Fluent UI

Responsibilities:

    IIS
        Windows authentication

    ASP.NET Core
        Authorization

    ActiveDirectoryService
        AD information lookup

    UserContextService
        Application user context

    Blazor
        Presentation

    Fluent UI
        Presentation only

============================================================
36. DEVELOPMENT CHECKLIST
============================================================

Before implementing LDAP:

    [ ] .NET 10 project created
    [ ] Fluent UI installed
    [ ] Interactive Server configured
    [ ] Windows Authentication enabled
    [ ] Anonymous Authentication disabled
    [ ] IIS/IIS Express tested
    [ ] /identity works
    [ ] User.Identity.Name confirmed
    [ ] IsAuthenticated confirmed
    [ ] /claims works
    [ ] Actual claims inspected
    [ ] GroupSid claims identified
    [ ] UserContext implemented
    [ ] UserContextService implemented

============================================================
37. ACTIVE DIRECTORY CHECKLIST
============================================================

Before writing the final LDAP implementation determine:

    [ ] AD domain name
    [ ] LDAP path
    [ ] IIS server domain membership
    [ ] Application Pool identity
    [ ] Whether a service account is required
    [ ] Required AD attributes
    [ ] Direct groups vs nested groups
    [ ] Whether multiple domains are involved
    [ ] Required application groups

Do not guess these values.

============================================================
38. AUTHORIZATION CHECKLIST
============================================================

Before production:

    [ ] Actual AD group SIDs identified
    [ ] Policies defined
    [ ] Protected pages use [Authorize]
    [ ] Protected server operations enforce authorization
    [ ] AuthorizeView used only for UI
    [ ] No client-side security decisions
    [ ] Diagnostic pages restricted
    [ ] Unauthorized users tested
    [ ] Authorized users tested
    [ ] Removed group membership tested
    [ ] Nested group behaviour tested if required

============================================================
39. IIS PRODUCTION CHECKLIST
============================================================

    [ ] Windows Authentication enabled
    [ ] Anonymous Authentication disabled
    [ ] HTTPS configured
    [ ] Valid TLS certificate
    [ ] Dedicated Application Pool
    [ ] No Managed Code
    [ ] Domain connectivity verified
    [ ] DNS verified
    [ ] Kerberos verified if required
    [ ] SPN verified if required
    [ ] AD lookup tested
    [ ] Logging configured

============================================================
40. APPLICATION PRODUCTION CHECKLIST
============================================================

    [ ] Authentication configured
    [ ] Authorization configured
    [ ] UserContext implemented
    [ ] UserContextService implemented
    [ ] ActiveDirectoryService implemented
    [ ] AD permissions minimized
    [ ] Sensitive pages protected
    [ ] Sensitive operations protected
    [ ] Claims diagnostic page restricted
    [ ] Sensitive logging disabled
    [ ] Multiple domain users tested

============================================================
41. IMPORTANT IMPLEMENTATION ORDER
============================================================

DO NOT start with LDAP.

Use this order:

    STEP 1
        Create the .NET 10 Blazor application.

    STEP 2
        Install Fluent UI.

    STEP 3
        Configure Interactive Server.

    STEP 4
        Configure Windows Authentication.

    STEP 5
        Configure IIS.

    STEP 6
        Test /identity.

    STEP 7
        Confirm:

            User.Identity.Name

    STEP 8
        Test /claims.

    STEP 9
        Inspect actual claims.

    STEP 10
        Identify actual GroupSid values.

    STEP 11
        Implement UserContext.

    STEP 12
        Implement UserContextService.

    STEP 13
        Display UserContext.

    STEP 14
        Implement ActiveDirectoryService.

    STEP 15
        Retrieve AD attributes.

    STEP 16
        Determine group authorization strategy.

    STEP 17
        Create authorization policies.

    STEP 18
        Protect pages.

    STEP 19
        Protect server operations.

    STEP 20
        Deploy to IIS.

    STEP 21
        Test using multiple AD users.

============================================================
42. FINAL NOTES
============================================================

The previously missing classes are now included:

    Models/UserContext.cs

    Models/UserClaim.cs

    Models/ActiveDirectoryUser.cs

    Services/IUserContextService.cs

    Services/UserContextService.cs

    Services/IActiveDirectoryService.cs

    Services/ActiveDirectoryService.cs

The initial ActiveDirectoryService intentionally does not
perform LDAP queries.

This allows the application to first prove that:

    IIS
        |
        v
    Windows Authentication
        |
        v
    ClaimsPrincipal
        |
        v
    Blazor
        |
        v
    UserContext

works correctly.

Once that is confirmed, implement the LDAP/Active Directory
lookup against the actual AD environment.

Do not guess:

    Domain
    LDAP path
    Service account
    Group SIDs
    Nested group requirements
    Multiple-domain configuration

============================================================
END OF SETUP
============================================================
```