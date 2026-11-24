# **Account security implementation — Sprint 1**

We'll implement the three account-security requirements for your .NET 10 / C# 14 Blazor Interactive Server application.

The implementation will use:

- ASP.NET Core Identity and your existing `ApplicationUser`.
- SQL Server 2019, database-first.
- FluentUI Blazor for the user interface.
- `UserManager` and `SignInManager` for Identity operations.
- `TimeProvider` for all date/time calculations.
- Your existing central DI extension.
- xUnit.v3, Shouldly and Moq for unit tests.
- No SQLite, no integration tests and no scheduled worker in this sprint.

I'm going to separate the security rules from the UI and authentication transport. This is important because your application supports both username/password and Active Directory authentication.

## **1. Expected behaviour**

| Scenario                                        | Result                                           |
| ----------------------------------------------- | ------------------------------------------------ |
| Password is younger than configured expiry      | Normal login                                     |
| Password has expired                            | Restricted password-change session               |
| Administrator resets password                   | Restricted password-change session at next login |
| User changes own password                       | Expiry period restarts                           |
| User has not logged in within configured period | Account locked at login                          |
| Account is already locked                       | Login rejected                                   |
| AD user authenticates successfully              | Application inactivity policy still applies      |

For AD-managed passwords, the application should not attempt to change the AD password using `UserManager`. AD should remain responsible for the actual password lifecycle. The application can still enforce inactivity and any application-specific forced-change requirements.

## **2. Configuration**

Add the following to `appsettings.json`:

```

{
  "AccountSecurity": {
    "PasswordExpiryDays": 90,
    "InactiveAccountLockDays": 90,
    "LockoutExemptionsEnabled": true
  }
}

```

Create `AccountSecurityOptions.cs`:

```

namespace YourApplication.Configuration;

/// <summary>
/// Defines application account-security settings.
/// </summary>
public sealed class AccountSecurityOptions
{
    /// <summary>
    /// Gets or sets the number of days before a locally managed password expires.
    /// </summary>
    public int PasswordExpiryDays { get; set; } = 90;

    /// <summary>
    /// Gets or sets the number of days without a successful login
    /// before an account is locked.
    /// </summary>
    public int InactiveAccountLockDays { get; set; } = 90;

    /// <summary>
    /// Gets or sets whether explicitly exempted accounts are excluded
    /// from inactivity locking.
    /// </summary>
    public bool LockoutExemptionsEnabled { get; set; } = true;
}

```

## **3. Extend ApplicationUser**

Add the following properties to your existing `ApplicationUser`. Do not replace your existing Identity properties.

```

/// <summary>
/// Gets or sets the UTC date and time of the user's last successful application login.
/// </summary>
public DateTimeOffset? LastLoginAtUtc { get; set; }

/// <summary>
/// Gets or sets the UTC date and time the user's password was last changed.
/// </summary>
public DateTimeOffset? PasswordChangedAtUtc { get; set; }

/// <summary>
/// Gets or sets the UTC date and time the account was created.
/// </summary>
public DateTimeOffset CreatedAtUtc { get; set; }

/// <summary>
/// Gets or sets whether the user must change their password.
/// </summary>
public bool MustChangePassword { get; set; }

/// <summary>
/// Gets or sets whether the password is managed by an external directory.
/// </summary>
public bool PasswordManagedExternally { get; set; }

/// <summary>
/// Gets or sets whether the account is exempt from inactivity locking.
/// </summary>
public bool IsExemptFromInactivityLockout { get; set; }

```

`CreatedAtUtc` is important. Without it, we cannot correctly determine the inactivity period for users who have never logged in.

### **SQL Server script**

This is an idempotent database-first script:

```

IF COL_LENGTH('dbo.AspNetUsers', 'LastLoginAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.AspNetUsers
    ADD LastLoginAtUtc datetimeoffset(7) NULL;
END;

IF COL_LENGTH('dbo.AspNetUsers', 'PasswordChangedAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.AspNetUsers
    ADD PasswordChangedAtUtc datetimeoffset(7) NULL;
END;

IF COL_LENGTH('dbo.AspNetUsers', 'CreatedAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.AspNetUsers
    ADD CreatedAtUtc datetimeoffset(7) NOT NULL
        CONSTRAINT DF_AspNetUsers_CreatedAtUtc
        DEFAULT (SYSUTCDATETIME());
END;

IF COL_LENGTH('dbo.AspNetUsers', 'MustChangePassword') IS NULL
BEGIN
    ALTER TABLE dbo.AspNetUsers
    ADD MustChangePassword bit NOT NULL
        CONSTRAINT DF_AspNetUsers_MustChangePassword
        DEFAULT (0);
END;

IF COL_LENGTH('dbo.AspNetUsers', 'PasswordManagedExternally') IS NULL
BEGIN
    ALTER TABLE dbo.AspNetUsers
    ADD PasswordManagedExternally bit NOT NULL
        CONSTRAINT DF_AspNetUsers_PasswordManagedExternally
        DEFAULT (0);
END;

IF COL_LENGTH('dbo.AspNetUsers', 'IsExemptFromInactivityLockout') IS NULL
BEGIN
    ALTER TABLE dbo.AspNetUsers
    ADD IsExemptFromInactivityLockout bit NOT NULL
        CONSTRAINT DF_AspNetUsers_IsExemptFromInactivityLockout
        DEFAULT (0);
END;

```

Existing accounts will receive the deployment date as their initial `CreatedAtUtc`. You should initialise `PasswordChangedAtUtc` deliberately: setting it to the deployment date gives existing users a full new expiry period.

For new users, explicitly set `CreatedAtUtc` in your user-creation service. The SQL default is a safety net.

## **4. Account password policy**

This class contains the password-expiry calculation. It has no database dependency and is straightforward to unit test.

`AccountPasswordPolicy.cs`:

```

using Microsoft.Extensions.Options;
using YourApplication.Configuration;

namespace YourApplication.Security;

/// <summary>
/// Evaluates application password-change requirements.
/// </summary>
public sealed class AccountPasswordPolicy
{
    private readonly AccountSecurityOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initialises the password policy.
    /// </summary>
    public AccountPasswordPolicy(
        IOptions<AccountSecurityOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Determines whether a user must change their password.
    /// </summary>
    public bool MustChangePassword(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.PasswordManagedExternally)
        {
            return false;
        }

        if (user.MustChangePassword)
        {
            return true;
        }

        if (!user.PasswordChangedAtUtc.HasValue)
        {
            return true;
        }

        DateTimeOffset expiryDate =
            user.PasswordChangedAtUtc.Value.AddDays(
                _options.PasswordExpiryDays);

        return _timeProvider.GetUtcNow() >= expiryDate;
    }
}

```

The policy deliberately treats a locally managed account with no recorded password-change date as requiring a password change. This avoids silently allowing passwords whose age is unknown.

## **5. Account inactivity policy**

The inactivity calculation should also be independent of Identity and EF Core.

`AccountInactivityPolicy.cs`:

```

using Microsoft.Extensions.Options;
using YourApplication.Configuration;

namespace YourApplication.Security;

/// <summary>
/// Evaluates whether an account has exceeded its permitted inactivity period.
/// </summary>
public sealed class AccountInactivityPolicy
{
    private readonly AccountSecurityOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initialises the inactivity policy.
    /// </summary>
    public AccountInactivityPolicy(
        IOptions<AccountSecurityOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Determines whether an account has exceeded the inactivity period.
    /// </summary>
    public bool IsInactive(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (_options.LockoutExemptionsEnabled &&
            user.IsExemptFromInactivityLockout)
        {
            return false;
        }

        DateTimeOffset lastActivity =
            user.LastLoginAtUtc ?? user.CreatedAtUtc;

        DateTimeOffset inactivityThreshold =
            _timeProvider.GetUtcNow().AddDays(
                -_options.InactiveAccountLockDays);

        return lastActivity <= inactivityThreshold;
    }
}

```

Notice that this uses `LastLoginAtUtc` if available, otherwise `CreatedAtUtc`.

## **6. Restrict the authenticated session when a password change is required**

This is particularly important for Interactive Server.

We must not let a user with an expired password access ordinary application pages. A useful way to do this is to add a claim to the Identity principal when it is created.

Create `AccountSecurityClaimTypes.cs`:

```

namespace YourApplication.Security;

/// <summary>
/// Defines claim types used by account-security authorization.
/// </summary>
public static class AccountSecurityClaimTypes
{
    /// <summary>
    /// Identifies an account that must change its password.
    /// </summary>
    public const string PasswordChangeRequired =
        "account_security.password_change_required";
}

```

Create `AccountClaimsPrincipalFactory.cs`:

```

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace YourApplication.Security;

/// <summary>
/// Creates Identity principals containing application account-security claims.
/// </summary>
public sealed class AccountClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser>
{
    private readonly AccountPasswordPolicy _passwordPolicy;

    /// <summary>
    /// Initialises the account claims principal factory.
    /// </summary>
    public AccountClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> identityOptions,
        AccountPasswordPolicy passwordPolicy)
        : base(userManager, identityOptions)
    {
        ArgumentNullException.ThrowIfNull(passwordPolicy);

        _passwordPolicy = passwordPolicy;
    }

    /// <summary>
    /// Creates a claims principal for the specified user.
    /// </summary>
    public override async Task<ClaimsPrincipal> CreateAsync(
        ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        ClaimsPrincipal principal = await base.CreateAsync(user);

        ClaimsIdentity? identity = principal.Identity as ClaimsIdentity;

        if (identity is null)
        {
            throw new InvalidOperationException(
                "The Identity principal does not contain a ClaimsIdentity.");
        }

        if (_passwordPolicy.MustChangePassword(user))
        {
            identity.AddClaim(
                new Claim(
                    AccountSecurityClaimTypes.PasswordChangeRequired,
                    "true"));
        }

        return principal;
    }
}

```

This ensures that when `SignInManager` creates a cookie, the principal contains the appropriate restriction claim.

### **Authorization policies**

Register the following policies:

```

using Microsoft.AspNetCore.Authorization;
using YourApplication.Security;

namespace YourApplication.Configuration;

/// <summary>
/// Provides application authorization policy registration.
/// </summary>
public static class AccountAuthorizationPolicies
{
    /// <summary>
    /// Registers account-security authorization policies.
    /// </summary>
    public static void AddAccountAuthorizationPolicies(
        this AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        AuthorizationPolicy normalApplicationAccess =
            new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    !context.User.HasClaim(
                        AccountSecurityClaimTypes.PasswordChangeRequired,
                        "true"))
                .Build();

        options.DefaultPolicy = normalApplicationAccess;
        options.FallbackPolicy = normalApplicationAccess;

        options.AddPolicy(
            "PasswordChangeOnly",
            policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(
                    AccountSecurityClaimTypes.PasswordChangeRequired,
                    "true");
            });
    }
}

```

Register the normal policy as the default and fallback policy. Your login endpoints must explicitly allow anonymous access, and your password-change endpoint must use the `PasswordChangeOnly` policy.

For Blazor pages, use `[Authorize]` on application pages and `[Authorize(Policy = "PasswordChangeOnly")]` on the password-change page. The `AuthorizeRouteView` in your router must be configured to use authorization.

**Important:** do not rely on hiding navigation items or buttons. Authorization must protect the actual pages and server-side operations.

## **7. Login-time inactivity enforcement**

The scheduled job is deferred. This service enforces inactivity whenever a user successfully authenticates.

I recommend separating credential verification from the decision to issue an application session. This also allows your AD authentication flow to use the same inactivity and password policies.

Create `AccountLoginResult.cs`:

```

namespace YourApplication.Security;

/// <summary>
/// Defines the possible outcomes of application login processing.
/// </summary>
public enum AccountLoginResult
{
    /// <summary>
    /// The application session was created successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// The account has been locked due to inactivity.
    /// </summary>
    LockedOut,

    /// <summary>
    /// The account is already locked.
    /// </summary>
    AlreadyLocked,

    /// <summary>
    /// The user must change their password.
    /// </summary>
    PasswordChangeRequired,

    /// <summary>
    /// The account could not be updated.
    /// </summary>
    AccountUpdateFailed
}

```

Create `AccountLoginService.cs`:

```

using Microsoft.AspNetCore.Identity;
using YourApplication.Security;

namespace YourApplication.Services;

/// <summary>
/// Applies application account-security policies before creating a login session.
/// </summary>
public sealed class AccountLoginService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AccountInactivityPolicy _inactivityPolicy;
    private readonly AccountPasswordPolicy _passwordPolicy;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initialises the account login service.
    /// </summary>
    public AccountLoginService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AccountInactivityPolicy inactivityPolicy,
        AccountPasswordPolicy passwordPolicy,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(inactivityPolicy);
        ArgumentNullException.ThrowIfNull(passwordPolicy);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _userManager = userManager;
        _signInManager = signInManager;
        _inactivityPolicy = inactivityPolicy;
        _passwordPolicy = passwordPolicy;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Applies account-security policies and creates an application session
    /// after the user's credentials have been successfully verified.
    /// </summary>
    public async Task<AccountLoginResult> CompleteVerifiedLoginAsync(
        ApplicationUser user,
        bool isPersistent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        cancellationToken.ThrowIfCancellationRequested();

        if (await _userManager.IsLockedOutAsync(user))
        {
            return AccountLoginResult.AlreadyLocked;
        }

        if (_inactivityPolicy.IsInactive(user))
        {
            IdentityResult enableLockoutResult =
                await _userManager.SetLockoutEnabledAsync(user, true);

            if (!enableLockoutResult.Succeeded)
            {
                return AccountLoginResult.AccountUpdateFailed;
            }

            IdentityResult lockoutResult =
                await _userManager.SetLockoutEndDateAsync(
                    user,
                    DateTimeOffset.MaxValue);

            if (!lockoutResult.Succeeded)
            {
                return AccountLoginResult.AccountUpdateFailed;
            }

            return AccountLoginResult.LockedOut;
        }

        bool passwordChangeRequired =
            _passwordPolicy.MustChangePassword(user);

        if (!passwordChangeRequired)
        {
            user.LastLoginAtUtc = _timeProvider.GetUtcNow();

            IdentityResult updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                return AccountLoginResult.AccountUpdateFailed;
            }
        }

        await _signInManager.SignInAsync(
            user,
            isPersistent);

        return passwordChangeRequired
            ? AccountLoginResult.PasswordChangeRequired
            : AccountLoginResult.Succeeded;
    }
}

```

There is an important contract here: `CompleteVerifiedLoginAsync` must only be called after credentials have been successfully verified.

For local username/password authentication, the caller should use `CheckPasswordSignInAsync` to verify the credentials without issuing the normal authentication cookie, then call this service on success.

For AD authentication, call this service after the directory credentials have been verified and the corresponding `ApplicationUser` has been resolved.

If you use Identity two-factor authentication, its completion flow must also pass through this gate before it issues the final application cookie. Do not let a separate two-factor callback bypass inactivity enforcement.

## **8. Password change service**

Both administrator resets and user-initiated password changes should go through one service.

The administrator must set `MustChangePassword` to `true`. The user must clear it after successfully changing their password.

`AccountPasswordService.cs`:

```

using Microsoft.AspNetCore.Identity;

namespace YourApplication.Services;

/// <summary>
/// Provides password-change operations for application accounts.
/// </summary>
public sealed class AccountPasswordService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initialises the account password service.
    /// </summary>
    public AccountPasswordService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _userManager = userManager;
        _signInManager = signInManager;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Resets a user's password administratively and requires the user
    /// to change it at their next login.
    /// </summary>
    public async Task<IdentityResult> ResetPasswordByAdministratorAsync(
        ApplicationUser user,
        string resetToken,
        string newPassword)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(resetToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        if (user.PasswordManagedExternally)
        {
            return IdentityResult.Failed(
                new IdentityError
                {
                    Code = "ExternalPasswordManagement",
                    Description =
                        "This user's password is managed by an external directory."
                });
        }

        DateTimeOffset? originalPasswordChangedAt =
            user.PasswordChangedAtUtc;

        bool originalMustChangePassword =
            user.MustChangePassword;

        user.PasswordChangedAtUtc = _timeProvider.GetUtcNow();
        user.MustChangePassword = true;

        IdentityResult result =
            await _userManager.ResetPasswordAsync(
                user,
                resetToken,
                newPassword);

        if (!result.Succeeded)
        {
            user.PasswordChangedAtUtc = originalPasswordChangedAt;
            user.MustChangePassword = originalMustChangePassword;
        }

        return result;
    }

    /// <summary>
    /// Changes the current user's password, resets its expiry period,
    /// clears the forced-change flag and refreshes the authentication session.
    /// </summary>
    public async Task<IdentityResult> ChangeOwnPasswordAsync(
        ApplicationUser user,
        string currentPassword,
        string newPassword)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        if (user.PasswordManagedExternally)
        {
            return IdentityResult.Failed(
                new IdentityError
                {
                    Code = "ExternalPasswordManagement",
                    Description =
                        "This user's password is managed by an external directory."
                });
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();

        DateTimeOffset? originalPasswordChangedAt =
            user.PasswordChangedAtUtc;

        DateTimeOffset? originalLastLoginAt =
            user.LastLoginAtUtc;

        bool originalMustChangePassword =
            user.MustChangePassword;

        user.PasswordChangedAtUtc = now;
        user.LastLoginAtUtc = now;
        user.MustChangePassword = false;

        IdentityResult result =
            await _userManager.ChangePasswordAsync(
                user,
                currentPassword,
                newPassword);

        if (!result.Succeeded)
        {
            user.PasswordChangedAtUtc = originalPasswordChangedAt;
            user.LastLoginAtUtc = originalLastLoginAt;
            user.MustChangePassword = originalMustChangePassword;

            return result;
        }

        await _signInManager.RefreshSignInAsync(user);

        return IdentityResult.Success;
    }
}

```

The user password change operation deliberately refreshes the authentication session. That rebuilds the claims principal, removing the password-change restriction claim.

The administrator reset uses `ResetPasswordAsync`, not direct modification of `PasswordHash`.

**Important Interactive Server detail:** call `ChangeOwnPasswordAsync` from an HTTP request that can issue the updated authentication cookie, such as a controller POST endpoint. Do not rely on changing cookies from an already-established SignalR circuit event. The same applies to initial sign-in: the login form should submit to an HTTP endpoint that can issue the cookie.

## **9. Central DI registration**

Add the following to your existing central service-registration extension. Do not create a second `ApplicationDbContext` registration.

```

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YourApplication.Configuration;
using YourApplication.Security;
using YourApplication.Services;

namespace YourApplication.Extensions;

/// <summary>
/// Provides central application service registration.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers application account-security services.
    /// </summary>
    public static IServiceCollection AddAccountSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<AccountSecurityOptions>()
            .Bind(configuration.GetSection("AccountSecurity"))
            .Validate(
                options => options.PasswordExpiryDays > 0,
                "PasswordExpiryDays must be greater than zero.")
            .Validate(
                options => options.InactiveAccountLockDays > 0,
                "InactiveAccountLockDays must be greater than zero.")
            .ValidateOnStart();

        services.TryAddSingleton<TimeProvider>(TimeProvider.System);

        services.AddScoped<AccountPasswordPolicy>();
        services.AddScoped<AccountInactivityPolicy>();
        services.AddScoped<AccountLoginService>();
        services.AddScoped<AccountPasswordService>();

        services.Replace(
            ServiceDescriptor.Scoped<
                IUserClaimsPrincipalFactory<ApplicationUser>,
                AccountClaimsPrincipalFactory>());

        services.AddAuthorization(options =>
        {
            options.AddAccountAuthorizationPolicies();
        });

        return services;
    }
}

```

Call it from your existing registration:

```
builder.Services.AddAccountSecurity(builder.Configuration);
```

Call it after your existing Identity registration so the claims-principal factory replacement takes effect.

If you already register `TimeProvider`, keep your existing registration rather than adding another one.

## **10. EF Core mapping**

Add the following to the existing `ApplicationDbContext.OnModelCreating` mapping for `ApplicationUser`:

```

builder.Entity<ApplicationUser>(entity =>
{
    entity.Property(user => user.LastLoginAtUtc)
        .HasColumnType("datetimeoffset(7)");

    entity.Property(user => user.PasswordChangedAtUtc)
        .HasColumnType("datetimeoffset(7)");

    entity.Property(user => user.CreatedAtUtc)
        .HasColumnType("datetimeoffset(7)")
        .IsRequired();

    entity.Property(user => user.MustChangePassword)
        .HasDefaultValue(false)
        .IsRequired();

    entity.Property(user => user.PasswordManagedExternally)
        .HasDefaultValue(false)
        .IsRequired();

    entity.Property(user => user.IsExemptFromInactivityLockout)
        .HasDefaultValue(false)
        .IsRequired();
});

```

Keep your existing Identity mappings. This is an addition to them, not a replacement.

Also check your audit property policy. These fields should be audited:

- `LastLoginAtUtc`
- `PasswordChangedAtUtc`
- `MustChangePassword`
- `LockoutEnabled`
- `LockoutEnd`

They are useful for understanding why an account was locked or why a password change was required. Password hashes and security tokens must remain excluded.

## **11. Unit tests**

No database is required for these tests.

For the test project, use:

```
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" Version="10.0.0" />
  <PackageReference Include="Shouldly" Version="4.3.0" />
  <PackageReference Include="xunit.v3" Version="3.2.2" />
</ItemGroup>
```

Use package versions aligned with the rest of your solution if you already have a centrally managed package version file.

### **Password policy tests**

`AccountPasswordPolicyTests.cs`:

```

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using Xunit;
using YourApplication.Configuration;
using YourApplication.Security;

namespace YourApplication.Tests.Security;

/// <summary>
/// Tests password-expiry policy calculations.
/// </summary>
public sealed class AccountPasswordPolicyTests
{
    [Fact]
    public void MustChangePassword_WhenPasswordIsWithinExpiryPeriod_ReturnsFalse()
    {
        FakeTimeProvider timeProvider = new(
            new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));

        ApplicationUser user = new()
        {
            PasswordChangedAtUtc =
                new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero)
        };

        AccountPasswordPolicy policy = new(
            Options.Create(new AccountSecurityOptions
            {
                PasswordExpiryDays = 90
            }),
            timeProvider);

        policy.MustChangePassword(user).ShouldBeFalse();
    }

    [Fact]
    public void MustChangePassword_WhenPasswordHasExpired_ReturnsTrue()
    {
        FakeTimeProvider timeProvider = new(
            new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));

        ApplicationUser user = new()
        {
            PasswordChangedAtUtc =
                new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero)
        };

        AccountPasswordPolicy policy = new(
            Options.Create(new AccountSecurityOptions
            {
                PasswordExpiryDays = 90
            }),
            timeProvider);

        policy.MustChangePassword(user).ShouldBeTrue();
    }

    [Fact]
    public void MustChangePassword_WhenAdministratorFlagIsSet_ReturnsTrue()
    {
        FakeTimeProvider timeProvider = new(
            new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));

        ApplicationUser user = new()
        {
            MustChangePassword = true,
            PasswordChangedAtUtc =
                new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero)
        };

        AccountPasswordPolicy policy = new(
            Options.Create(new AccountSecurityOptions
            {
                PasswordExpiryDays = 90
            }),
            timeProvider);

        policy.MustChangePassword(user).ShouldBeTrue();
    }

    [Fact]
    public void MustChangePassword_WhenPasswordDateIsMissing_ReturnsTrue()
    {
        FakeTimeProvider timeProvider = new(
            new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));

        ApplicationUser user = new();

        AccountPasswordPolicy policy = new(
            Options.Create(new AccountSecurityOptions
            {
                PasswordExpiryDays = 90
            }),
            timeProvider);

        policy.MustChangePassword(user).ShouldBeTrue();
    }

    [Fact]
    public void MustChangePassword_WhenPasswordIsManagedExternally_ReturnsFalse()
    {
        FakeTimeProvider timeProvider = new(
            new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));

        ApplicationUser user = new()
        {
            PasswordManagedExternally = true,
            MustChangePassword = true
        };

        AccountPasswordPolicy policy = new(
            Options.Create(new AccountSecurityOptions
            {
                PasswordExpiryDays = 90
            }),
            timeProvider);

        policy.MustChangePassword(user).ShouldBeFalse();
    }
}

```

### **Inactivity policy tests**

`AccountInactivityPolicyTests.cs`:

```

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using Xunit;
using YourApplication.Configuration;
using YourApplication.Security;

namespace YourApplication.Tests.Security;

/// <summary>
/// Tests account inactivity calculations.
/// </summary>
public sealed class AccountInactivityPolicyTests
{
    [Fact]
    public void IsInactive_WhenLastLoginIsWithinThreshold_ReturnsFalse()
    {
        FakeTimeProvider timeProvider = new(
            new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));

        ApplicationUser user = new()
        {
            CreatedAtUtc =
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            LastLoginAtUtc =
                new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero)
        };

        AccountInactivityPolicy policy = new(
            Options.Create(new AccountSecurityOptions
            {
                InactiveAccountLockDays = 90
            }),
            timeProvider);

        policy.IsInactive(user).ShouldBeFalse();
    }

    [Fact]
    public void IsInactive_WhenLastLoginExceedsThreshold_ReturnsTrue()
    {
        FakeTimeProvider timeProvider = new(
            new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));

        ApplicationUser user = new()
        {
            CreatedAtUtc =
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            LastLoginAtUtc =
                new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero)
        };

        AccountInactivityPolicy policy = new(
            Options.Create(new AccountSecurityOptions
            {
                InactiveAccountLockDays = 90
            }),
            timeProvider);

        policy.IsInactive(user).ShouldBeTrue();
    }

    [Fact]
    public void IsInactive_WhenUserHasNeverLoggedIn_UsesCreationDate()
    {
        FakeTimeProvider timeProvider = new(
            new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));

        ApplicationUser user = new()
        {
            CreatedAtUtc =
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        AccountInactivityPolicy policy = new(
            Options.Create(new AccountSecurityOptions
            {
                InactiveAccountLockDays = 90
            }),
            timeProvider);

        policy.IsInactive(user).ShouldBeTrue();
    }

    [Fact]
    public void IsInactive_WhenUserIsExemptAndExemptionsEnabled_ReturnsFalse()
    {
        FakeTimeProvider timeProvider = new(
            new DateTimeOffset(2026, 11, 23, 12, 0, 0, TimeSpan.Zero));

        ApplicationUser user = new()
        {
            CreatedAtUtc =
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            IsExemptFromInactivityLockout = true
        };

        AccountInactivityPolicy policy = new(
            Options.Create(new AccountSecurityOptions
            {
                InactiveAccountLockDays = 90,
                LockoutExemptionsEnabled = true
            }),
            timeProvider);

        policy.IsInactive(user).ShouldBeFalse();
    }
}

```

These are pure unit tests. The login and password services should also have tests using mocked Identity managers to verify that they call the expected Identity operations and return the correct outcomes. Those tests should cover:

- Inactive user: lockout enabled and lockout end set.
- Already locked user: no new sign-in.
- Expired password: restricted session created.
- Administrator reset: forced-change flag persisted.
- Successful own-password change: expiry date reset and sign-in refreshed.
- Failed password change: password-policy fields not changed.
- AD account: local password operations rejected.

## **12. Connecting the login component**

The local username/password login flow should follow this sequence:

```
ApplicationUser? user =
    await userManager.FindByNameAsync(model.UserName);

if (user is null)
{
    // Return a generic invalid-credentials message.
    return;
}

SignInResult verification =
    await signInManager.CheckPasswordSignInAsync(
        user,
        model.Password,
        lockoutOnFailure: true);

if (verification.Succeeded)
{
    AccountLoginResult result =
        await accountLoginService.CompleteVerifiedLoginAsync(
            user,
            model.RememberMe);

    // Redirect according to result.
}
else if (verification.IsLockedOut)
{
    // Display the account-locked message.
}
else if (verification.RequiresTwoFactor)
{
    // Continue through the two-factor flow.
}
else
{
    // Display a generic invalid-credentials message.
}
```

This is the service-level flow, not code to execute directly from a Blazor circuit event. The actual login submission should go through an HTTP endpoint so Identity can issue the authentication cookie.

For your pages, the intended authorization is:

```
@* Ordinary application pages *@
@attribute [Authorize]
```

```
@* Password-change page *@
@attribute [Authorize(Policy = "PasswordChangeOnly")]
```

The login page should be anonymous:

```
@attribute [AllowAnonymous]
```

The password-change page should submit its change through an HTTP POST endpoint. After the password change succeeds, `RefreshSignInAsync` refreshes the cookie and the user can enter the normal application.

### **Two things to be aware of**

1. The sample login flow deliberately separates credential verification from session creation. Your two-factor flow must do the same; don't use a two-factor completion method that issues a normal cookie before the account-security checks.
2. Your AD authentication flow must also call `CompleteVerifiedLoginAsync` after successful directory authentication. Otherwise, AD users could bypass the application inactivity policy.

## **13. Final implementation checklist**

**Sprint 1 — account security**

Included in design

- Configuration-driven password expiry
- Administrator-forced password change
- User password-change handling
- Login-time inactivity enforcement
- Identity account lockout
- Restricted password-change authentication
- AD-managed password distinction
- TimeProvider-based date calculations
- Database-first SQL changes
- Central DI registration
- Unit-test approach without SQLite

**Deferred**

- Scheduled inactivity-locking job
- Account-security administration UI
- Account-security reporting
- Integration tests

**One qualification about the code above:** it provides the security-policy and service foundation, but it is not yet a drop-in replacement for your login component. The HTTP login/change-password endpoints and your existing two-factor/AD flows must be wired into it. I don't want to invent those parts and accidentally give you an authentication bypass.

If you provide your current FluentUI login component and the relevant Identity registration from `Program.cs`, I can adapt the endpoint and component code to your actual application rather than make assumptions about its existing authentication flow.