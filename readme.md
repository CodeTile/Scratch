Below is the regenerated Sprint 1 implementation, incorporating your actual architectural decisions.

I've consolidated the dependency injection into **one&#x20;**`IServiceCollection`**&#x20;extension**, retained `ApplicationUser : IdentityUser`, and removed all audit-viewing functionality.

I've also corrected some important details from the earlier version:

- The actor record and actor service have distinct names.
- The interceptor supports synchronous and asynchronous saves.
- Audit records are added to the same DbContext transaction.
- Password hashes, security stamps and concurrency stamps are excluded.
- Background operations must supply an explicit system or service actor rather than inherit an interactive Blazor user's identity.
- Audit values are redacted according to a central policy.
- The audit table has no foreign key to `AspNetUsers`.

# **1. Solution structure**

```
YourSolution
│
├── Domain
│   └── Identity
│       └── IdentityUserAudit.cs
│
├── Application
│   └── Identity
│       ├── IdentityAuditActor.cs
│       └── IIdentityAuditActor.cs
│
├── Infrastructure
│   ├── Data
│   │   └── ApplicationDbContext.cs
│   │
│   └── Identity
│       ├── IdentityUserAuditInterceptor.cs
│       ├── IdentityAuditPropertyPolicy.cs
│       └── HttpBlazorIdentityAuditActor.cs
│
├── Extensions
│   └── ServiceCollectionExtensions.cs
│
└── Tests
    └── Identity
        ├── IdentityUserAuditInterceptorTests.cs
        └── IdentityAuditPropertyPolicyTests.cs
```

The namespaces below are illustrative; keep your existing project namespace conventions.

# **2. SQL Server 2019 – database-first script**

File: `Database/Scripts/001_CreateIdentityUserAudit.sql`

```

IF OBJECT_ID(N'dbo.IdentityUserAudit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.IdentityUserAudit
    (
        AuditId BIGINT IDENTITY(1,1) NOT NULL,

        UserId NVARCHAR(450) NOT NULL,

        Action NVARCHAR(20) NOT NULL,

        PropertyName NVARCHAR(256) NOT NULL,

        OldValue NVARCHAR(MAX) NULL,

        NewValue NVARCHAR(MAX) NULL,

        ChangedBy NVARCHAR(450) NULL,

        ChangedByName NVARCHAR(256) NULL,

        ChangedAtUtc DATETIME2(7) NOT NULL,

        CorrelationId NVARCHAR(128) NULL,

        CONSTRAINT PK_IdentityUserAudit
            PRIMARY KEY CLUSTERED (AuditId),

        CONSTRAINT CK_IdentityUserAudit_Action
            CHECK (Action IN ('INSERT', 'UPDATE', 'DELETE'))
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_IdentityUserAudit_UserId'
      AND object_id = OBJECT_ID(N'dbo.IdentityUserAudit')
)
BEGIN
    CREATE INDEX IX_IdentityUserAudit_UserId
        ON dbo.IdentityUserAudit(UserId);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_IdentityUserAudit_ChangedAtUtc'
      AND object_id = OBJECT_ID(N'dbo.IdentityUserAudit')
)
BEGIN
    CREATE INDEX IX_IdentityUserAudit_ChangedAtUtc
        ON dbo.IdentityUserAudit(ChangedAtUtc);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_IdentityUserAudit_ChangedBy'
      AND object_id = OBJECT_ID(N'dbo.IdentityUserAudit')
)
BEGIN
    CREATE INDEX IX_IdentityUserAudit_ChangedBy
        ON dbo.IdentityUserAudit(ChangedBy);
END;
GO

```

No foreign key is deliberately created. Deleting an Identity account must not delete its audit history.

# **3. Domain – audit entity**

File: `Domain/Identity/IdentityUserAudit.cs`

```

namespace YourSolution.Domain.Identity;

/// <summary>
/// Represents an individual change made to an Identity user.
/// </summary>
public sealed class IdentityUserAudit
{
    /// <summary>
    /// Gets or sets the audit record identifier.
    /// </summary>
    public long AuditId { get; set; }

    /// <summary>
    /// Gets or sets the affected Identity user identifier.
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Gets or sets the type of operation performed.
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Gets or sets the name of the modified property.
    /// </summary>
    public required string PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the original property value.
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// Gets or sets the new property value.
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the person making the change.
    /// </summary>
    public string? ChangedBy { get; set; }

    /// <summary>
    /// Gets or sets the display name of the person making the change.
    /// </summary>
    public string? ChangedByName { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the change.
    /// </summary>
    public DateTime ChangedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the request correlation identifier.
    /// </summary>
    public string? CorrelationId { get; set; }
}

```

# **4. Application – actor abstraction**

File: `Application/Identity/IdentityAuditActor.cs`

```

namespace YourSolution.Application.Identity;

/// <summary>
/// Describes the identity responsible for an audited operation.
/// </summary>
public sealed record IdentityAuditActor(
    string? UserId,
    string? DisplayName,
    string? CorrelationId);

```

File: `Application/Identity/IIdentityAuditActor.cs`

```

namespace YourSolution.Application.Identity;

/// <summary>
/// Resolves the identity responsible for an audited operation.
/// </summary>
public interface IIdentityAuditActor
{
    /// <summary>
    /// Gets the current audit actor synchronously.
    /// </summary>
    IdentityAuditActor GetCurrentActor();

    /// <summary>
    /// Gets the current audit actor asynchronously.
    /// </summary>
    Task<IdentityAuditActor> GetCurrentActorAsync(
        CancellationToken cancellationToken = default);
}

```

# **5. Infrastructure – property auditing policy**

I recommend keeping sensitive-property handling separate from the interceptor.

This makes it easier to extend `ApplicationUser` later without embedding security decisions throughout the persistence logic.

File: `Infrastructure/Identity/IdentityAuditPropertyPolicy.cs`

```

namespace YourSolution.Infrastructure.Identity;

/// <summary>
/// Determines which ApplicationUser properties may be audited.
/// </summary>
public sealed class IdentityAuditPropertyPolicy
{
    private static readonly HashSet<string> ExcludedProperties =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Id",
            "PasswordHash",
            "SecurityStamp",
            "ConcurrencyStamp",
            "AuthenticatorKey",
            "RecoveryCodes",
            "TwoFactorRecoveryCodes",
            "AccessToken",
            "RefreshToken",
            "ClientSecret"
        };

    /// <summary>
    /// Determines whether a property is eligible for auditing.
    /// </summary>
    public bool ShouldAudit(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        return !ExcludedProperties.Contains(propertyName);
    }
}

```

This is an explicit deny-list for sensitive values. When you add additional confidential properties to `ApplicationUser`, add them here.

# **6. Infrastructure – audit actor implementation**

File: `Infrastructure/Identity/HttpBlazorIdentityAuditActor.cs`

This supports the two execution environments:

- Standard ASP.NET Core HTTP requests.
- Interactive Blazor authentication through `AuthenticationStateProvider`.

For synchronous saves, it uses the HTTP principal only. If a Blazor circuit has no HTTP context, the actor is recorded as an unattributed system operation rather than incorrectly attributing it to somebody else.

For asynchronous saves, it can resolve the Blazor circuit principal.

```

using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using YourSolution.Application.Identity;

namespace YourSolution.Infrastructure.Identity;

/// <summary>
/// Resolves audit actors from ASP.NET Core and Blazor authentication.
/// </summary>
public sealed class HttpBlazorIdentityAuditActor(
    IHttpContextAccessor httpContextAccessor,
    AuthenticationStateProvider authenticationStateProvider)
    : IIdentityAuditActor
{
    /// <inheritdoc />
    public IdentityAuditActor GetCurrentActor()
    {
        var principal = httpContextAccessor.HttpContext?.User;

        return CreateActor(principal);
    }

    /// <inheritdoc />
    public async Task<IdentityAuditActor> GetCurrentActorAsync(
        CancellationToken cancellationToken = default)
    {
        var principal = httpContextAccessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            var authenticationState =
                await authenticationStateProvider
                    .GetAuthenticationStateAsync();

            cancellationToken.ThrowIfCancellationRequested();

            principal = authenticationState.User;
        }

        return CreateActor(principal);
    }

    private IdentityAuditActor CreateActor(
        ClaimsPrincipal? principal)
    {
        var httpContext = httpContextAccessor.HttpContext;

        var correlationId =
            httpContext?.TraceIdentifier
            ?? Activity.Current?.Id;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            return new IdentityAuditActor(
                null,
                "Unattributed System Operation",
                correlationId);
        }

        var userId = principal.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var displayName =
            principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.Identity?.Name
            ?? userId;

        return new IdentityAuditActor(
            userId,
            displayName,
            correlationId);
    }
}

```

**Important:** Background services should use a dedicated system actor identity, not an interactive Blazor `AuthenticationStateProvider`. The unattributed fallback above is intentional. If you need to identify individual background jobs, introduce an explicit job actor rather than relying on ambient authentication.

# **7. Infrastructure – ApplicationDbContext**

File: `Infrastructure/Data/ApplicationDbContext.cs`

This is the relevant addition to your existing context.

```

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YourSolution.Domain.Identity;
using YourSolution.Domain.Users;

namespace YourSolution.Infrastructure.Data;

/// <summary>
/// Provides database access for application and Identity data.
/// </summary>
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    /// <summary>
    /// Gets the Identity user audit records.
    /// </summary>
    public DbSet<IdentityUserAudit> IdentityUserAudits =>
        Set<IdentityUserAudit>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<IdentityUserAudit>(entity =>
        {
            entity.ToTable("IdentityUserAudit", "dbo");

            entity.HasKey(audit => audit.AuditId);

            entity.Property(audit => audit.AuditId)
                .ValueGeneratedOnAdd();

            entity.Property(audit => audit.UserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(audit => audit.Action)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(audit => audit.PropertyName)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(audit => audit.OldValue)
                .HasColumnType("nvarchar(max)");

            entity.Property(audit => audit.NewValue)
                .HasColumnType("nvarchar(max)");

            entity.Property(audit => audit.ChangedBy)
                .HasMaxLength(450);

            entity.Property(audit => audit.ChangedByName)
                .HasMaxLength(256);

            entity.Property(audit => audit.ChangedAtUtc)
                .HasColumnType("datetime2(7)")
                .IsRequired();

            entity.Property(audit => audit.CorrelationId)
                .HasMaxLength(128);
        });
    }
}

```

If your existing context already has substantial `OnModelCreating` configuration, retain it and add only the `IdentityUserAudit` entity configuration.

No migrations are required.

# **8. Infrastructure – EF Core SaveChangesInterceptor**

File: `Infrastructure/Identity/IdentityUserAuditInterceptor.cs`

This is the main auditing component.

```

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using YourSolution.Application.Identity;
using YourSolution.Domain.Identity;
using YourSolution.Domain.Users;

namespace YourSolution.Infrastructure.Identity;

/// <summary>
/// Intercepts EF Core persistence operations and audits ApplicationUser changes.
/// </summary>
public sealed class IdentityUserAuditInterceptor(
    IIdentityAuditActor auditActor,
    IdentityAuditPropertyPolicy propertyPolicy,
    TimeProvider timeProvider)
    : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is DbContext context)
        {
            AddAuditEntries(
                context,
                auditActor.GetCurrentActor());
        }

        return result;
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>>
        SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
    {
        if (eventData.Context is DbContext context)
        {
            var actor = await auditActor.GetCurrentActorAsync(
                cancellationToken);

            AddAuditEntries(context, actor);
        }

        return result;
    }

    private void AddAuditEntries(
        DbContext context,
        IdentityAuditActor actor)
    {
        context.ChangeTracker.DetectChanges();

        var userEntries = context.ChangeTracker
            .Entries<ApplicationUser>()
            .Where(entry =>
                entry.State is EntityState.Added
                    or EntityState.Modified
                    or EntityState.Deleted)
            .ToList();

        if (userEntries.Count == 0)
        {
            return;
        }

        var timestamp = timeProvider.GetUtcNow().UtcDateTime;

        var auditEntries = new List<IdentityUserAudit>();

        foreach (var entry in userEntries)
        {
            auditEntries.AddRange(
                CreateAuditEntries(
                    entry,
                    actor,
                    timestamp));
        }

        if (auditEntries.Count > 0)
        {
            context.Set<IdentityUserAudit>()
                .AddRange(auditEntries);
        }
    }

    private IEnumerable<IdentityUserAudit> CreateAuditEntries(
        EntityEntry<ApplicationUser> entry,
        IdentityAuditActor actor,
        DateTime timestamp)
    {
        var action = GetAction(entry.State);

        foreach (var property in entry.Properties)
        {
            var propertyName = property.Metadata.Name;

            if (!propertyPolicy.ShouldAudit(propertyName))
            {
                continue;
            }

            if (!ShouldAuditProperty(entry, property))
            {
                continue;
            }

            yield return new IdentityUserAudit
            {
                UserId = entry.Entity.Id,
                Action = action,
                PropertyName = propertyName,
                OldValue = GetOldValue(entry, property),
                NewValue = GetNewValue(entry, property),
                ChangedBy = actor.UserId,
                ChangedByName = actor.DisplayName,
                ChangedAtUtc = timestamp,
                CorrelationId = actor.CorrelationId
            };
        }
    }

    private static string GetAction(EntityState state)
    {
        return state switch
        {
            EntityState.Added => "INSERT",
            EntityState.Modified => "UPDATE",
            EntityState.Deleted => "DELETE",

            _ => throw new InvalidOperationException(
                $"Unsupported Identity user state: {state}")
        };
    }

    private static bool ShouldAuditProperty(
        EntityEntry<ApplicationUser> entry,
        PropertyEntry property)
    {
        if (entry.State == EntityState.Added)
        {
            return property.CurrentValue is not null;
        }

        if (entry.State == EntityState.Deleted)
        {
            return property.OriginalValue is not null;
        }

        if (entry.State == EntityState.Modified)
        {
            return property.IsModified
                && !Equals(
                    property.OriginalValue,
                    property.CurrentValue);
        }

        return false;
    }

    private static string? GetOldValue(
        EntityEntry<ApplicationUser> entry,
        PropertyEntry property)
    {
        if (entry.State == EntityState.Added)
        {
            return null;
        }

        return FormatValue(property.OriginalValue);
    }

    private static string? GetNewValue(
        EntityEntry<ApplicationUser> entry,
        PropertyEntry property)
    {
        if (entry.State == EntityState.Deleted)
        {
            return null;
        }

        return FormatValue(property.CurrentValue);
    }

    private static string? FormatValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is byte[])
        {
            return "[BINARY VALUE OMITTED]";
        }

        if (value is DateTime dateTime)
        {
            return dateTime.ToString(
                "O",
                CultureInfo.InvariantCulture);
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.ToString(
                "O",
                CultureInfo.InvariantCulture);
        }

        if (value is bool boolean)
        {
            return boolean.ToString();
        }

        if (value is IFormattable formattable)
        {
            return formattable.ToString(
                null,
                CultureInfo.InvariantCulture);
        }

        return value.ToString();
    }
}

```

### **How this works**

For example, if your application executes:

```
var user = await userManager.FindByIdAsync(userId);

user.Email = "new@company.com";
user.PhoneNumber = "0123456789";

await userManager.UpdateAsync(user);
```

The Identity EF store updates `ApplicationUser` through `ApplicationDbContext`.

The interceptor sees the two modified properties and adds two audit records before EF Core executes the save.

The resulting records look like this:

| Property    | Old value        | New value        |
| ----------- | ---------------- | ---------------- |
| Email       | old\@company.com | new\@company.com |
| PhoneNumber | NULL             | 0123456789       |

The audit records and user changes are persisted together.

**Important:** `UserManager.UpdateAsync` can also update the concurrency stamp and other Identity properties. Those are deliberately excluded.

Also, this interceptor only captures changes made through the tracked EF Core entity pipeline. Bulk updates and direct SQL need separate auditing.

# **9. Single IServiceCollection extension**

This is where all the registrations belong in your project.

File: `Extensions/ServiceCollectionExtensions.cs`

The following example assumes your existing `ApplicationDbContext` is registered here too.

```

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YourSolution.Application.Identity;
using YourSolution.Infrastructure.Data;
using YourSolution.Infrastructure.Identity;

namespace YourSolution.Extensions;

/// <summary>
/// Provides central application dependency injection registrations.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers application services and infrastructure dependencies.
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        RegisterIdentityAuditing(services);

        RegisterDatabase(services, configuration);

        // Retain all your existing application registrations here.

        return services;
    }

    private static void RegisterIdentityAuditing(
        IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.TryAddSingleton<TimeProvider>(
            TimeProvider.System);

        services.AddScoped<
            IIdentityAuditActor,
            HttpBlazorIdentityAuditActor>();

        services.AddScoped<IdentityAuditPropertyPolicy>();

        services.AddScoped<IdentityUserAuditInterceptor>();
    }

    private static void RegisterDatabase(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionString);

        services.AddDbContext<ApplicationDbContext>(
            (serviceProvider, options) =>
            {
                options.UseSqlServer(connectionString);

                options.AddInterceptors(
                    serviceProvider.GetRequiredService<
                        IdentityUserAuditInterceptor>());
            });
    }
}

```

**Registration warning:** If your existing extension already registers `ApplicationDbContext`, keep that registration and merge the `AddInterceptors` call into it. Do not register the same context twice.

Your `Program.cs` remains:

```
builder.Services.AddApplicationServices(
    builder.Configuration);
```

Keep your existing Identity and hybrid authentication registrations. The auditing registration does not replace them.

# **10. Automated tests**

I would keep the tests against a real EF Core relational provider rather than mocking `DbContext` or the change tracker.

For the fast test suite, SQLite in-memory is suitable. SQL Server integration tests should additionally verify SQL Server-specific transaction and schema behaviour.

### **Test project packages**

```
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk"
                    Version="18.0.1" />

  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite"
                    Version="10.0.0" />

  <PackageReference Include="xunit.v3"
                    Version="3.2.2" />

  <PackageReference Include="Shouldly"
                    Version="4.3.0" />
</ItemGroup>
```

Use versions aligned with the actual .NET 10 servicing version and your repository's package-management policy.

### **IdentityUserAuditInterceptorTests.cs**

```

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;
using YourSolution.Application.Identity;
using YourSolution.Domain.Users;
using YourSolution.Infrastructure.Data;
using YourSolution.Infrastructure.Identity;

namespace YourSolution.Tests.Identity;

/// <summary>
/// Tests Identity user auditing through EF Core.
/// </summary>
public sealed class IdentityUserAuditInterceptorTests
{
    [Fact]
    public async Task SaveChangesAsync_WhenEmailChanges_ShouldCreateAuditRecord()
    {
        // Arrange
        var timestamp = new DateTimeOffset(
            2026, 11, 23, 13, 0, 0, TimeSpan.Zero);

        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var interceptor = CreateInterceptor(timestamp);

        var options = CreateOptions(
            connection,
            interceptor);

        await using var context =
            new ApplicationDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var user = new ApplicationUser
        {
            UserName = "john.smith",
            Email = "old@company.com",
            NormalizedEmail = "OLD@COMPANY.COM"
        };

        // Act: insert user
        context.Users.Add(user);

        await context.SaveChangesAsync();

        // Act: update user
        user.Email = "new@company.com";
        user.NormalizedEmail = "NEW@COMPANY.COM";

        await context.SaveChangesAsync();

        // Assert
        var audit = await context.IdentityUserAudits
            .SingleAsync(record =>
                record.UserId == user.Id &&
                record.PropertyName == "Email");

        audit.Action.ShouldBe("UPDATE");
        audit.OldValue.ShouldBe("old@company.com");
        audit.NewValue.ShouldBe("new@company.com");
        audit.ChangedBy.ShouldBe("admin-123");
        audit.ChangedByName.ShouldBe("Administrator");
        audit.CorrelationId.ShouldBe("correlation-123");
        audit.ChangedAtUtc.ShouldBe(timestamp.UtcDateTime);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenUserIsCreated_ShouldCreateInsertAudits()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(
            connection,
            CreateInterceptor(TimeProvider.System.GetUtcNow()));

        await using var context =
            new ApplicationDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var user = new ApplicationUser
        {
            UserName = "john.smith",
            Email = "john@company.com"
        };

        // Act
        context.Users.Add(user);

        await context.SaveChangesAsync();

        // Assert
        var auditRecords = await context.IdentityUserAudits
            .Where(record => record.UserId == user.Id)
            .ToListAsync();

        auditRecords.ShouldNotBeEmpty();

        auditRecords.ShouldAllBe(record =>
            record.Action == "INSERT");

        auditRecords.ShouldContain(record =>
            record.PropertyName == "Email" &&
            record.NewValue == "john@company.com");
    }

    [Fact]
    public async Task SaveChangesAsync_WhenUserIsDeleted_ShouldCreateDeleteAudits()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(
            connection,
            CreateInterceptor(TimeProvider.System.GetUtcNow()));

        await using var context =
            new ApplicationDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var user = new ApplicationUser
        {
            UserName = "john.smith",
            Email = "john@company.com"
        };

        context.Users.Add(user);

        await context.SaveChangesAsync();

        var userId = user.Id;

        // Act
        context.Users.Remove(user);

        await context.SaveChangesAsync();

        // Assert
        var audit = await context.IdentityUserAudits
            .SingleAsync(record =>
                record.UserId == userId &&
                record.PropertyName == "Email" &&
                record.Action == "DELETE");

        audit.OldValue.ShouldBe("john@company.com");
        audit.NewValue.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenPasswordHashChanges_ShouldNotAuditPasswordHash()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(
            connection,
            CreateInterceptor(TimeProvider.System.GetUtcNow()));

        await using var context =
            new ApplicationDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var user = new ApplicationUser
        {
            UserName = "john.smith",
            PasswordHash = "OLD_HASH"
        };

        context.Users.Add(user);

        await context.SaveChangesAsync();

        // Act
        user.PasswordHash = "NEW_HASH";

        await context.SaveChangesAsync();

        // Assert
        var passwordAudit = await context.IdentityUserAudits
            .AnyAsync(record =>
                record.PropertyName == "PasswordHash");

        passwordAudit.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenPropertyIsUnchanged_ShouldNotCreateAuditRecord()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(
            connection,
            CreateInterceptor(TimeProvider.System.GetUtcNow()));

        await using var context =
            new ApplicationDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var user = new ApplicationUser
        {
            UserName = "john.smith",
            Email = "john@company.com"
        };

        context.Users.Add(user);

        await context.SaveChangesAsync();

        // Act
        user.Email = "john@company.com";

        await context.SaveChangesAsync();

        // Assert
        var emailUpdate = await context.IdentityUserAudits
            .AnyAsync(record =>
                record.UserId == user.Id &&
                record.PropertyName == "Email" &&
                record.Action == "UPDATE");

        emailUpdate.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenTransactionRollsBack_ShouldRollbackAuditAndUserChanges()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(
            connection,
            CreateInterceptor(TimeProvider.System.GetUtcNow()));

        await using var context =
            new ApplicationDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var user = new ApplicationUser
        {
            UserName = "john.smith",
            Email = "old@company.com"
        };

        context.Users.Add(user);

        await context.SaveChangesAsync();

        var userId = user.Id;

        // Act
        await using (var transaction =
            await context.Database.BeginTransactionAsync())
        {
            user.Email = "new@company.com";

            await context.SaveChangesAsync();

            await transaction.RollbackAsync();
        }

        // Assert
        await using var verificationContext =
            new ApplicationDbContext(options);

        var persistedUser = await verificationContext.Users
            .SingleAsync(record => record.Id == userId);

        persistedUser.Email.ShouldBe("old@company.com");

        var updateAudit = await verificationContext.IdentityUserAudits
            .AnyAsync(record =>
                record.UserId == userId &&
                record.PropertyName == "Email" &&
                record.Action == "UPDATE");

        updateAudit.ShouldBeFalse();
    }

    private static IdentityUserAuditInterceptor CreateInterceptor(
        DateTimeOffset timestamp)
    {
        return new IdentityUserAuditInterceptor(
            new TestAuditActor(
                new IdentityAuditActor(
                    "admin-123",
                    "Administrator",
                    "correlation-123")),
            new IdentityAuditPropertyPolicy(),
            new FixedTimeProvider(timestamp));
    }

    private static DbContextOptions<ApplicationDbContext> CreateOptions(
        SqliteConnection connection,
        IdentityUserAuditInterceptor interceptor)
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;
    }

    private sealed class TestAuditActor(
        IdentityAuditActor actor) : IIdentityAuditActor
    {
        public IdentityAuditActor GetCurrentActor()
        {
            return actor;
        }

        public Task<IdentityAuditActor> GetCurrentActorAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(actor);
        }
    }

    private sealed class FixedTimeProvider(
        DateTimeOffset timestamp) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return timestamp;
        }
    }
}

```

### **IdentityAuditPropertyPolicyTests.cs**

```

using Shouldly;
using Xunit;
using YourSolution.Infrastructure.Identity;

namespace YourSolution.Tests.Identity;

/// <summary>
/// Tests the Identity audit property policy.
/// </summary>
public sealed class IdentityAuditPropertyPolicyTests
{
    private readonly IdentityAuditPropertyPolicy _sut = new();

    [Theory]
    [InlineData("PasswordHash")]
    [InlineData("SecurityStamp")]
    [InlineData("ConcurrencyStamp")]
    [InlineData("AuthenticatorKey")]
    [InlineData("RecoveryCodes")]
    [InlineData("Id")]
    public void ShouldAudit_WhenSensitivePropertyIsSupplied_ShouldReturnFalse(
        string propertyName)
    {
        // Act
        var result = _sut.ShouldAudit(propertyName);

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData("Email")]
    [InlineData("UserName")]
    [InlineData("PhoneNumber")]
    [InlineData("EmailConfirmed")]
    [InlineData("LockoutEnabled")]
    [InlineData("FirstName")]
    [InlineData("LastName")]
    public void ShouldAudit_WhenNormalPropertyIsSupplied_ShouldReturnTrue(
        string propertyName)
    {
        // Act
        var result = _sut.ShouldAudit(propertyName);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void ShouldAudit_WhenPropertyNameIsEmpty_ShouldThrowException()
    {
        // Act
        var action = () => _sut.ShouldAudit(string.Empty);

        // Assert
        Should.Throw<ArgumentException>(action);
    }
}

```

Run the test suite using:

```
dotnet test \
    --framework net10.0 \
    -- \
    --report-trx
```

The exact reporting options depend on the MTP runner configuration you install.

# **11. Important implementation checks**

Before considering Sprint 1 complete, there are a few integration details worth making explicit.

| Area                            | Expected behaviour                 |
| ------------------------------- | ---------------------------------- |
| UserManager.CreateAsync         | Audited                            |
| UserManager.UpdateAsync         | Audited                            |
| UserManager.DeleteAsync         | Audited                            |
| UserManager.AddPasswordAsync    | PasswordHash excluded              |
| UserManager.ChangePasswordAsync | PasswordHash excluded              |
| UserManager.ResetPasswordAsync  | PasswordHash excluded              |
| AD attribute synchronisation    | Audited when saved through EF Core |
| Direct SQL changes              | Not captured                       |
| EF Core ExecuteUpdateAsync      | Not captured                       |

One additional consideration: the property policy currently excludes known sensitive properties by name. Any new confidential properties added to `ApplicationUser` must be reviewed and excluded explicitly.

## **12. Two implementation details I would tighten before production**

**A. UserManager integration testing**

The tests above exercise the actual EF Core interception pipeline. I would add a further integration test using a real `UserManager<ApplicationUser>` and `UserStore<ApplicationUser>` against the test database. That proves the Identity store registration actually uses the intercepted DbContext.

**B. Interactive Blazor actor resolution**

Use asynchronous Identity operations from interactive Blazor components and services. Synchronous saves only resolve the actor through `IHttpContextAccessor`; an interactive circuit without an HTTP context will be recorded as an unattributed system operation. This is preferable to incorrectly attributing a background operation to an administrator.

## **Final Sprint 1 acceptance criteria**

Scope agreed

The deliverable is now limited to:

1. SQL Server audit table and indexes.
2. `ApplicationUser` change interception.
3. Before-and-after property values.
4. Administrator identification.
5. UTC timestamps.
6. Sensitive property exclusion.
7. Atomic persistence with Identity changes.
8. Centralised DI registration.
9. Automated tests.

No FluentUI pages, controllers, audit browsing, reporting or export functionality.

**One final architectural recommendation:** keep the audit table append-only at the database permission level. That gives you a much stronger audit trail than relying on application code alone.

This is the foundation I'd use for Sprint 1, with the integration tests and SQL Server verification as release gates.