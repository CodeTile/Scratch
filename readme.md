# **Complete implementation — .NET 10 / C# 14**

We'll build this in dependency order, so each section can be implemented and tested before moving to the next. This is **Part 1: SQL Server and database-first preparation**. It establishes the database contract for the rest of the code.

The final decisions are:

- Local accounts authenticate with application passwords and access business functionality.
- AD accounts authenticate through Active Directory and are strictly administration-only.
- AD users are provisioned into `AspNetUsers` on first eligible login.
- Any one of several configured AD groups grants administrator eligibility.
- AD group names are configured by display name in `appsettings.json`.
- Local users can be assigned service accounts.
- Self-service password changes reject the current and previous 19 passwords.
- All relevant changes are audited.
- Existing generated entities and the database context are retained.

# **Part 1 — SQL Server**

## **1. Database assumptions and safeguards**

We will retain your existing structure:

```
MyApplication.Data/
├── Contexts/
│   └── ExistingDatabaseContext.cs
├── Entities/
│   ├── AspNetUser.cs
│   ├── ServiceAccount.cs
│   └── ...
└── Infrastructure/
    └── IocStartup.cs
```

The scripts below assume:

- The existing user table is `dbo.AspNetUsers`.
- Its primary key is `Id`.
- Its key type is `NVARCHAR(450)`.
- Your existing `AspNetUsers` table is not dropped or recreated.
- `ServiceAccount` is an existing business table whose key definition must be checked before we add a foreign key.

**Important:** The scripts deliberately do not create a foreign key to `ServiceAccount`, because its actual key type and column are not known. We will establish that relationship after checking the existing schema.

## **2. Preflight script**

Run this before the migration scripts.

```

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
    THROW 50001, 'dbo.AspNetUsers does not exist.', 1;

IF COL_LENGTH(N'dbo.AspNetUsers', N'Id') IS NULL
    THROW 50002, 'dbo.AspNetUsers.Id does not exist.', 1;

SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length AS MaxLengthInBytes,
    c.is_nullable AS IsNullable
FROM sys.columns AS c
INNER JOIN sys.tables AS t
    ON t.object_id = c.object_id
INNER JOIN sys.schemas AS s
    ON s.schema_id = t.schema_id
INNER JOIN sys.types AS ty
    ON ty.user_type_id = c.user_type_id
WHERE s.name = N'dbo'
  AND t.name IN (N'AspNetUsers', N'ServiceAccount')
ORDER BY
    t.name,
    c.column_id;

SELECT
    i.name AS IndexName,
    i.is_primary_key AS IsPrimaryKey,
    i.is_unique AS IsUnique,
    i.filter_definition AS FilterDefinition
FROM sys.indexes AS i
WHERE i.object_id = OBJECT_ID(N'dbo.AspNetUsers')
ORDER BY i.name;

SELECT
    NormalizedUserName,
    COUNT(*) AS DuplicateCount
FROM dbo.AspNetUsers
WHERE NormalizedUserName IS NOT NULL
GROUP BY NormalizedUserName
HAVING COUNT(*) > 1;

```

Confirm that `AspNetUsers.Id` is `NVARCHAR(450)` (900 bytes). If not, the new `UserId` columns and foreign keys must be adjusted to match its actual type and collation.

## **3. Script 001 — Extend AspNetUsers**

This adds application-specific properties without changing the existing Identity columns.

```

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
        THROW 50001, 'dbo.AspNetUsers does not exist.', 1;

    IF COL_LENGTH(N'dbo.AspNetUsers', N'AuthenticationProvider') IS NULL
    BEGIN
        ALTER TABLE dbo.AspNetUsers
        ADD AuthenticationProvider TINYINT NOT NULL
            CONSTRAINT DF_AspNetUsers_AuthenticationProvider
            DEFAULT (0);
    END;

    IF COL_LENGTH(N'dbo.AspNetUsers', N'ActiveDirectoryObjectId') IS NULL
    BEGIN
        ALTER TABLE dbo.AspNetUsers
        ADD ActiveDirectoryObjectId NVARCHAR(256) NULL;
    END;

    IF COL_LENGTH(N'dbo.AspNetUsers', N'DisplayName') IS NULL
    BEGIN
        ALTER TABLE dbo.AspNetUsers
        ADD DisplayName NVARCHAR(256) NULL;
    END;

    IF COL_LENGTH(N'dbo.AspNetUsers', N'Department') IS NULL
    BEGIN
        ALTER TABLE dbo.AspNetUsers
        ADD Department NVARCHAR(128) NULL;
    END;

    IF COL_LENGTH(N'dbo.AspNetUsers', N'IsActive') IS NULL
    BEGIN
        ALTER TABLE dbo.AspNetUsers
        ADD IsActive BIT NOT NULL
            CONSTRAINT DF_AspNetUsers_IsActive
            DEFAULT (1);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AspNetUsers')
          AND name = N'UX_AspNetUsers_ActiveDirectoryObjectId'
    )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX
            UX_AspNetUsers_ActiveDirectoryObjectId
        ON dbo.AspNetUsers (ActiveDirectoryObjectId)
        WHERE ActiveDirectoryObjectId IS NOT NULL;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

```

Authentication provider values:

| Value | Meaning          |
| ----- | ---------------- |
| 0     | Local            |
| 1     | Active Directory |

Existing users receive provider value `0`. If AD users already exist, migrate them explicitly using a reliable AD identifier. Do not infer their provider from email addresses or usernames.

`ActiveDirectoryObjectId` stores the AD user's immutable object GUID in canonical string form. The unique filtered index prevents the same AD identity being provisioned twice.

## **4. Script 002 — UserPasswordHistory**

Stores previous local password hashes.

```

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
        THROW 50001, 'dbo.AspNetUsers does not exist.', 1;

    IF OBJECT_ID(N'dbo.UserPasswordHistory', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.UserPasswordHistory
        (
            Id BIGINT IDENTITY(1,1) NOT NULL,

            UserId NVARCHAR(450) NOT NULL,

            PasswordHash NVARCHAR(MAX) NOT NULL,

            ChangedUtc DATETIME2(7) NOT NULL
                CONSTRAINT DF_UserPasswordHistory_ChangedUtc
                DEFAULT (SYSUTCDATETIME()),

            CONSTRAINT PK_UserPasswordHistory
                PRIMARY KEY CLUSTERED (Id),

            CONSTRAINT FK_UserPasswordHistory_AspNetUsers
                FOREIGN KEY (UserId)
                REFERENCES dbo.AspNetUsers (Id)
                ON DELETE CASCADE
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.UserPasswordHistory')
          AND name = N'IX_UserPasswordHistory_UserId_ChangedUtc'
    )
    BEGIN
        CREATE NONCLUSTERED INDEX
            IX_UserPasswordHistory_UserId_ChangedUtc
        ON dbo.UserPasswordHistory
        (
            UserId,
            ChangedUtc DESC,
            Id DESC
        );
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

```

Password policy:

- Compare the proposed password against the current hash and the 19 most recent historical hashes.
- On a successful self-service change, save the old hash before updating the current hash.
- Retain only 19 historical hashes after a successful change.
- Do not apply this rule to administrator-initiated resets.
- Do not apply local password history to AD accounts.
- Never store plain-text passwords.
- Password history cannot recover passwords changed before this feature was introduced.

## **5. Script 003 — UserAuditLogs**

Stores user, authentication, provisioning, password and service-account audit events.

```

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.UserAuditLogs', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.UserAuditLogs
        (
            Id BIGINT IDENTITY(1,1) NOT NULL,

            UserId NVARCHAR(450) NULL,
            ChangedByUserId NVARCHAR(450) NULL,
            ServiceAccountId NVARCHAR(450) NULL,

            AuthenticationProvider TINYINT NULL,

            Action NVARCHAR(100) NOT NULL,
            PropertyName NVARCHAR(128) NULL,

            OldValue NVARCHAR(MAX) NULL,
            NewValue NVARCHAR(MAX) NULL,

            ChangedUtc DATETIME2(7) NOT NULL
                CONSTRAINT DF_UserAuditLogs_ChangedUtc
                DEFAULT (SYSUTCDATETIME()),

            CorrelationId NVARCHAR(100) NULL,

            CONSTRAINT PK_UserAuditLogs
                PRIMARY KEY CLUSTERED (Id)
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.UserAuditLogs')
          AND name = N'IX_UserAuditLogs_UserId_ChangedUtc'
    )
    BEGIN
        CREATE NONCLUSTERED INDEX
            IX_UserAuditLogs_UserId_ChangedUtc
        ON dbo.UserAuditLogs
        (
            UserId,
            ChangedUtc DESC
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.UserAuditLogs')
          AND name = N'IX_UserAuditLogs_ChangedByUserId_ChangedUtc'
    )
    BEGIN
        CREATE NONCLUSTERED INDEX
            IX_UserAuditLogs_ChangedByUserId_ChangedUtc
        ON dbo.UserAuditLogs
        (
            ChangedByUserId,
            ChangedUtc DESC
        );
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

```

There are intentionally no foreign keys from audit records to users or service accounts. This preserves audit history if an account is subsequently deleted.

Never write passwords, password hashes, authentication tokens or other secrets into audit records.

## **6. Script 004 — UserServiceAccounts**

Maps local application users to existing service accounts.

Supports:

- Global assignments.
- Page-specific assignments.
- Default service accounts.
- Revocation without deleting assignment history.
- Multiple assignments in the future.

```

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
        THROW 50001, 'dbo.AspNetUsers does not exist.', 1;

    IF OBJECT_ID(N'dbo.UserServiceAccounts', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.UserServiceAccounts
        (
            Id BIGINT IDENTITY(1,1) NOT NULL,

            UserId NVARCHAR(450) NOT NULL,

            -- Align this with the actual ServiceAccount primary key
            -- before adding a foreign key to that existing table.
            ServiceAccountId NVARCHAR(450) NOT NULL,

            PageKey NVARCHAR(128) NULL,

            IsDefault BIT NOT NULL
                CONSTRAINT DF_UserServiceAccounts_IsDefault
                DEFAULT (0),

            IsActive BIT NOT NULL
                CONSTRAINT DF_UserServiceAccounts_IsActive
                DEFAULT (1),

            CreatedUtc DATETIME2(7) NOT NULL
                CONSTRAINT DF_UserServiceAccounts_CreatedUtc
                DEFAULT (SYSUTCDATETIME()),

            RevokedUtc DATETIME2(7) NULL,

            CONSTRAINT PK_UserServiceAccounts
                PRIMARY KEY CLUSTERED (Id),

            CONSTRAINT FK_UserServiceAccounts_AspNetUsers
                FOREIGN KEY (UserId)
                REFERENCES dbo.AspNetUsers (Id)
                ON DELETE CASCADE
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.UserServiceAccounts')
          AND name = N'IX_UserServiceAccounts_UserId_IsActive'
    )
    BEGIN
        CREATE NONCLUSTERED INDEX
            IX_UserServiceAccounts_UserId_IsActive
        ON dbo.UserServiceAccounts (UserId, IsActive);
    END;

    -- Prevent duplicate active global assignments.
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.UserServiceAccounts')
          AND name = N'UX_UserServiceAccounts_Global'
    )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX
            UX_UserServiceAccounts_Global
        ON dbo.UserServiceAccounts (UserId, ServiceAccountId)
        WHERE PageKey IS NULL
          AND IsActive = 1;
    END;

    -- Prevent duplicate active page-specific assignments.
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.UserServiceAccounts')
          AND name = N'UX_UserServiceAccounts_Page'
    )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX
            UX_UserServiceAccounts_Page
        ON dbo.UserServiceAccounts
        (
            UserId,
            ServiceAccountId,
            PageKey
        )
        WHERE PageKey IS NOT NULL
          AND IsActive = 1;
    END;

    -- Only one active global default per user.
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.UserServiceAccounts')
          AND name = N'UX_UserServiceAccounts_GlobalDefault'
    )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX
            UX_UserServiceAccounts_GlobalDefault
        ON dbo.UserServiceAccounts (UserId)
        WHERE PageKey IS NULL
          AND IsDefault = 1
          AND IsActive = 1;
    END;

    -- Only one active default per user and page.
    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.UserServiceAccounts')
          AND name = N'UX_UserServiceAccounts_PageDefault'
    )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX
            UX_UserServiceAccounts_PageDefault
        ON dbo.UserServiceAccounts (UserId, PageKey)
        WHERE PageKey IS NOT NULL
          AND IsDefault = 1
          AND IsActive = 1;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;

```

**Important deployment checks:**

1. `UserId` must match the existing `AspNetUsers.Id` type and collation.
2. `ServiceAccountId` must be changed to match the actual existing `ServiceAccount` primary key before deployment.
3. We will add the service-account foreign key only after confirming the existing schema.
4. The database permits future multiple assignments. The initial one-active-assignment limit will be enforced in the API using a transaction and concurrency protection.
5. The API must reject all service-account assignment attempts for AD users.

## **7. Database-first generation**

After running the SQL scripts:

1. Open EF Core Power Tools in Visual Studio.
2. Select Reverse Engineer.
3. Select your existing SQL Server database.
4. Select the existing `AspNetUsers` table and the three new tables.
5. Select the existing `ServiceAccount` table only if it is already part of your generated model.
6. Preserve your existing entity and context naming.
7. Generate the entities and update the existing context.

The new generated entities should correspond to:

```
UserPasswordHistory
UserAuditLog
UserServiceAccount
```

The exact generated property and navigation names will be determined by the database schema and Power Tools settings.

Do not manually write these POCOs.

## **8. What comes next**

The remaining implementation will be delivered in this dependency order:

| Part | Implementation                                                        |
| ---- | --------------------------------------------------------------------- |
| 1    | SQL Server and database-first preparation                             |
| 2    | Data project: interfaces, services, Identity integration and auditing |
| 3    | API: local authentication, AD authentication and JIT provisioning     |
| 4    | API: authorization, controllers and service-account delegation        |
| 5    | Portal: cookie authentication, login, logout and UI integration       |
| 6    | Configuration, dependency injection and security settings             |
| 7    | xUnit v3 tests using Microsoft Testing Platform and Shouldly          |

The next part will address an important integration issue: your generated `AspNetUser` is a database-first POCO, so we need to integrate it with ASP.NET Core Identity without assuming it inherits from `IdentityUser` or changing the generated class.

**Part 1 is the database foundation.** Once the generated model is available in your project, the remaining code can be written against its actual database-first structure without requiring you to disclose those files.

====================================================================

# **Part 2 — Data project**

This part implements the data-access layer for the authentication and authorization design.

It builds on the SQL tables from Part 1 and your existing database-first EF Core model.

**Important design decision:** We will not make your generated `AspNetUser` inherit from `IdentityUser`. Instead, we will keep the generated entity as it is and use an application-owned Identity integration. This avoids modifying generated code and avoids assuming the structure of your existing `ExistingDatabaseContext`.

## **1. Data project structure**

Add the following application-owned folders and files:

```
MyApplication.Data/
│
├── Contexts/
│   └── ExistingDatabaseContext.cs       (generated)
│
├── Entities/
│   ├── AspNetUser.cs                     (generated)
│   ├── ServiceAccount.cs                 (generated)
│   ├── UserPasswordHistory.cs             (generated)
│   ├── UserAuditLog.cs                    (generated)
│   └── UserServiceAccount.cs              (generated)
│
├── Interfaces/
│   ├── IApplicationUserStore.cs
│   ├── IUserPasswordHistoryRepository.cs
│   ├── IUserAuditRepository.cs
│   └── IUserServiceAccountRepository.cs
│
├── Models/
│   ├── ApplicationUserRecord.cs
│   ├── UserPasswordHistoryRecord.cs
│   ├── UserAuditRecord.cs
│   └── UserServiceAccountRecord.cs
│
├── Repositories/
│   ├── ApplicationUserRepository.cs
│   ├── UserPasswordHistoryRepository.cs
│   ├── UserAuditRepository.cs
│   └── UserServiceAccountRepository.cs
│
└── Infrastructure/
    └── IocStartup.cs
```

The generated files remain generated. All new application-owned code is kept separate.

## **2. NuGet packages**

Install the following packages into `MyApplication.Data`:

```
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
  <PackageReference Include="Microsoft.AspNetCore.Identity" Version="2.3.1" />
</ItemGroup>
```

Use versions aligned with your actual .NET 10 and EF Core installation. In particular, don't downgrade EF Core packages generated by your existing project.

The Identity package is used for Identity interfaces and password hashing contracts. We are not using its standard EF `UserStore` against your arbitrary database-first context.

## **3. Application user record**

Create `Models/ApplicationUserRecord.cs`.

This is an application-owned data-transfer model. It is not an EF entity.

```

namespace MyApplication.Data.Models;

/// <summary>
/// Represents the application user information required by authentication
/// and authorization services.
/// </summary>
public sealed class ApplicationUserRecord
{
    /// <summary>
    /// Gets or sets the application user identifier.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the normalized username.
    /// </summary>
    public string? NormalizedUserName { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the normalized email address.
    /// </summary>
    public string? NormalizedEmail { get; set; }

    /// <summary>
    /// Gets or sets the password hash.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Gets or sets the security stamp.
    /// </summary>
    public string? SecurityStamp { get; set; }

    /// <summary>
    /// Gets or sets the concurrency stamp.
    /// </summary>
    public string? ConcurrencyStamp { get; set; }

    /// <summary>
    /// Gets or sets the authentication provider.
    /// Zero represents local authentication and one represents Active Directory.
    /// </summary>
    public byte AuthenticationProvider { get; set; }

    /// <summary>
    /// Gets or sets the Active Directory object identifier.
    /// </summary>
    public string? ActiveDirectoryObjectId { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the department.
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Gets or sets whether the application account is active.
    /// </summary>
    public bool IsActive { get; set; }
}

```

This record deliberately contains only the fields required by this design. Additional existing Identity properties can be added to the adapter when required.

## **4. Authentication provider constants**

Create `Models/AuthenticationProvider.cs`.

```

namespace MyApplication.Data.Models;

/// <summary>
/// Defines the authentication providers supported by the application.
/// </summary>
public enum AuthenticationProvider : byte
{
    /// <summary>
    /// Authentication using the application's local password.
    /// </summary>
    Local = 0,

    /// <summary>
    /// Authentication using Active Directory.
    /// </summary>
    ActiveDirectory = 1
}

```

Use this enum in application logic rather than scattering numeric values throughout the code.

## **5. Repository interfaces**

Create `Interfaces/IApplicationUserStore.cs`.

```

using MyApplication.Data.Models;

namespace MyApplication.Data.Interfaces;

/// <summary>
/// Defines application user data operations.
/// </summary>
public interface IApplicationUserStore
{
    /// <summary>
    /// Finds an application user by username.
    /// </summary>
    Task<ApplicationUserRecord?> FindByUserNameAsync(
        string normalizedUserName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an application user by identifier.
    /// </summary>
    Task<ApplicationUserRecord?> FindByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an Active Directory user by object identifier.
    /// </summary>
    Task<ApplicationUserRecord?> FindByActiveDirectoryObjectIdAsync(
        string objectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an application user.
    /// </summary>
    Task CreateAsync(
        ApplicationUserRecord user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an application user.
    /// </summary>
    Task UpdateAsync(
        ApplicationUserRecord user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's password hash and security stamp.
    /// </summary>
    Task UpdatePasswordAsync(
        string userId,
        string passwordHash,
        string securityStamp,
        string concurrencyStamp,
        CancellationToken cancellationToken = default);
}

```

Create `Interfaces/IUserPasswordHistoryRepository.cs`.

```

using MyApplication.Data.Models;

namespace MyApplication.Data.Interfaces;

/// <summary>
/// Defines password history operations.
/// </summary>
public interface IUserPasswordHistoryRepository
{
    /// <summary>
    /// Gets the most recent password hashes for a user.
    /// </summary>
    Task<IReadOnlyList<UserPasswordHistoryRecord>> GetRecentAsync(
        string userId,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a previous password hash.
    /// </summary>
    Task AddAsync(
        string userId,
        string passwordHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes password history records beyond the retention limit.
    /// </summary>
    Task RetainLatestAsync(
        string userId,
        int count,
        CancellationToken cancellationToken = default);
}

```

Create `Interfaces/IUserAuditRepository.cs`.

```

using MyApplication.Data.Models;

namespace MyApplication.Data.Interfaces;

/// <summary>
/// Defines user audit operations.
/// </summary>
public interface IUserAuditRepository
{
    /// <summary>
    /// Writes an audit record.
    /// </summary>
    Task AddAsync(
        UserAuditRecord record,
        CancellationToken cancellationToken = default);
}

```

Create `Interfaces/IUserServiceAccountRepository.cs`.

```

using MyApplication.Data.Models;

namespace MyApplication.Data.Interfaces;

/// <summary>
/// Defines user service-account assignment operations.
/// </summary>
public interface IUserServiceAccountRepository
{
    /// <summary>
    /// Gets active service-account assignments for a user and page.
    /// </summary>
    Task<IReadOnlyList<UserServiceAccountRecord>> GetActiveAssignmentsAsync(
        string userId,
        string? pageKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a service-account assignment is active for a user
    /// and the requested page.
    /// </summary>
    Task<bool> HasActiveAssignmentAsync(
        string userId,
        string serviceAccountId,
        string? pageKey,
        CancellationToken cancellationToken = default);
}

```

## **6. Correct the NuGet package references**

One correction to the package list in the previous section: use the .NET 10 Identity Core package rather than the older `Microsoft.AspNetCore.Identity` 2.3.1 package.

For `MyApplication.Data.csproj`:

```
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
  <PackageReference Include="Microsoft.Extensions.Identity.Core" Version="10.0.0" />
</ItemGroup>
```

Keep these versions aligned with the versions used by your solution.

## **7. Additional data models**

Create `Models/UserPasswordHistoryRecord.cs`.

```

namespace MyApplication.Data.Models;

/// <summary>
/// Represents a previously used password hash.
/// </summary>
public sealed class UserPasswordHistoryRecord
{
    /// <summary>
    /// Gets or sets the password history identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the password hash.
    /// </summary>
    public required string PasswordHash { get; set; }

    /// <summary>
    /// Gets or sets the date and time the password was changed.
    /// </summary>
    public DateTime ChangedUtc { get; set; }
}

```

Create `Models/UserAuditRecord.cs`.

```

namespace MyApplication.Data.Models;

/// <summary>
/// Represents an application audit event.
/// </summary>
public sealed class UserAuditRecord
{
    /// <summary>
    /// Gets or sets the affected user identifier.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user who performed the action.
    /// </summary>
    public string? ChangedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the service account identifier.
    /// </summary>
    public string? ServiceAccountId { get; set; }

    /// <summary>
    /// Gets or sets the authentication provider.
    /// </summary>
    public byte? AuthenticationProvider { get; set; }

    /// <summary>
    /// Gets or sets the audit action.
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Gets or sets the affected property name.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets or sets the previous value.
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// Gets or sets the new value.
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp.
    /// </summary>
    public DateTime ChangedUtc { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier.
    /// </summary>
    public string? CorrelationId { get; set; }
}

```

Create `Models/UserServiceAccountRecord.cs`.

```

namespace MyApplication.Data.Models;

/// <summary>
/// Represents a user's service-account assignment.
/// </summary>
public sealed class UserServiceAccountRecord
{
    /// <summary>
    /// Gets or sets the assignment identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the application user identifier.
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Gets or sets the service-account identifier.
    /// </summary>
    public required string ServiceAccountId { get; set; }

    /// <summary>
    /// Gets or sets the optional page key.
    /// </summary>
    public string? PageKey { get; set; }

    /// <summary>
    /// Gets or sets whether this is the default assignment.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets whether the assignment is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets when the assignment was created.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Gets or sets when the assignment was revoked.
    /// </summary>
    public DateTime? RevokedUtc { get; set; }
}

```

## **8. Make password changes atomic**

There is an important issue with the original password-history interface: adding a history record and changing `AspNetUsers.PasswordHash` as separate operations could leave inconsistent data if one operation succeeds and the other fails.

Replace `Interfaces/IUserPasswordHistoryRepository.cs` with this version.

```

using MyApplication.Data.Models;

namespace MyApplication.Data.Interfaces;

/// <summary>
/// Defines password history and atomic password change operations.
/// </summary>
public interface IUserPasswordHistoryRepository
{
    /// <summary>
    /// Gets the most recent password history records for a user.
    /// </summary>
    Task<IReadOnlyList<UserPasswordHistoryRecord>> GetRecentAsync(
        string userId,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically stores the previous password hash, updates the current
    /// password hash and retains the configured number of history records.
    /// </summary>
    Task ChangePasswordAsync(
        string userId,
        string expectedConcurrencyStamp,
        string newPasswordHash,
        string newSecurityStamp,
        string newConcurrencyStamp,
        int historyCount,
        CancellationToken cancellationToken = default);
}

```

The implementation must use one SQL transaction with appropriate concurrency protection. It should:

1. Lock and retrieve the current user password and concurrency stamp.
2. Verify the expected concurrency stamp.
3. Insert the existing password hash into `UserPasswordHistory`.
4. Update `AspNetUsers.PasswordHash`, `SecurityStamp` and `ConcurrencyStamp`.
5. Retain only the configured number of historical hashes.
6. Commit the transaction.

The service that calls this method must perform the password-history comparison using `IPasswordHasher<TUser>` before invoking the change operation.

This prevents two concurrent password changes from silently overwriting one another.

## **9. Identity integration boundary**

The application needs to use ASP.NET Core's password hasher without forcing your generated entity to inherit from `IdentityUser`.

The Identity interfaces and password hasher will be used by application services. The generated entity remains a normal EF Core entity.

For example, the password hasher is registered using the Identity Core package:

```
services.AddScoped<IPasswordHasher<ApplicationUserRecord>,
    PasswordHasher<ApplicationUserRecord>>();
```

The authentication service will call:

```
var verificationResult = passwordHasher.VerifyHashedPassword(
    user,
    user.PasswordHash!,
    proposedPassword);
```

The result must be checked against both:

- The current password hash.
- The most recent 19 historical hashes.

Never compare newly generated hashes directly to determine whether two passwords are the same. Password hashing algorithms use salts, so equivalent passwords can produce different hash strings.

## **10. Repository implementation strategy**

Because your `ExistingDatabaseContext` and generated `AspNetUser` property definitions are not available to this implementation, I am deliberately separating the repository contracts from the generated EF model.

For this application, the repositories can use the existing `DbContext.Database.GetDbConnection()` API and parameterised SQL commands against the known database tables.

This has three advantages:

- It does not require assumptions about generated `DbSet` property names.
- It does not require modifying generated POCOs.
- It allows the repositories to use the existing database context.

The implementations will use the existing `DbContext` instance supplied by dependency injection.

There is one limitation: the service-account repository can validate assignment records, but the check that the underlying service account is active must use your actual generated `ServiceAccount` model and its real active-status property. That integration belongs in the next API/service layer.

## **11. Dependency injection**

Create `Infrastructure/IocStartup.cs`.

This registers the repository contracts while allowing your existing application to register its generated context normally.

```

using Microsoft.Extensions.DependencyInjection;
using MyApplication.Data.Interfaces;
using MyApplication.Data.Repositories;

namespace MyApplication.Data.Infrastructure;

/// <summary>
/// Provides dependency injection registration for the Data project.
/// </summary>
public static class IocStartup
{
    /// <summary>
    /// Registers application data services.
    /// The application's existing database context must already be registered.
    /// </summary>
    public static IServiceCollection AddApplicationData(
        this IServiceCollection services)
    {
        services.AddScoped<IApplicationUserStore,
            ApplicationUserRepository>();

        services.AddScoped<IUserPasswordHistoryRepository,
            UserPasswordHistoryRepository>();

        services.AddScoped<IUserAuditRepository,
            UserAuditRepository>();

        services.AddScoped<IUserServiceAccountRepository,
            UserServiceAccountRepository>();

        return services;
    }
}

```

Call it from the API's `Program.cs` after registering the existing database context:

```
builder.Services.AddApplicationData();
```

The repository implementations should receive the existing context through `DbContext` injection. If necessary, register the existing context as its base type:

```
builder.Services.AddScoped<DbContext>(serviceProvider =>
    serviceProvider.GetRequiredService<ExistingDatabaseContext>());
```

Use the actual namespace of your generated context in this registration.

## **12. Part 2 completion checklist**

Implementation checklist

- Install the correct .NET 10 package references.
- Generate the three new EF Core entities using Power Tools.
- Add the application-owned record models.
- Add the repository interfaces.
- Implement atomic password-history updates.
- Implement the user repository against the actual `AspNetUsers` schema.
- Implement the audit repository.
- Implement service-account assignment queries.
- Register the repositories in dependency injection.
- Confirm the generated context is registered only once.

**A note about completeness:** The interfaces and models above establish the Data project's contracts, but they are not yet a complete compilable repository implementation. I have not invented the generated context's namespace or the existing entity's property names. The concrete repository classes must be added against the database contract; the service-account active-status check also needs the actual existing table definition.

================================================================================================================================================================
## **Next: Part 3 — API authentication**
================================================================================================================================================================
