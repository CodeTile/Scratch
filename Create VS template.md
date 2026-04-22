
# 1. Final Template Folder Structure

```
DarkMode.Template/
│
├── .template.config/
│   └── template.json
│
├── API/
│   ├── DarkMode.API.csproj
│   ├── DarkMode.API.Services.csproj
│   ├── Tests/
│   │   ├── DarkMode.API.Tests.csproj
│   │   ├── DarkMode.API.Reqnroll.Tests.csproj
│   │   ├── DarkMode.API.Services.Tests.csproj
│   │   ├── DarkMode.API.Services.Reqnroll.Tests.csproj
│   └── .template.config/
│       └── template.json
│
├── Portal/
│   ├── DarkMode.Blazor.csproj
│   ├── Tests/
│   │   ├── DarkMode.Blazor.Tests.csproj
│   │   ├── DarkMode.Blazor.Bunit.Tests.csproj
│   │   ├── DarkMode.Blazor.Reqnroll.Tests.csproj
│   └── .template.config/
│       └── template.json
│
└── DarkMode.sln
```

---

# 2. Root template.json (solution-level)

This file controls:

- Renaming of **solution**, **projects**, **namespaces**, **folders**
- Optional parameters:
  - `api`
  - `portal`
  - `tests`
- Conditional inclusion of folders

Create:

```
DarkMode.Template/.template.config/template.json
```

Paste:

```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "Bruce",
  "classifications": [ "Solution", "MultiProject" ],
  "identity": "DarkMode.Solution.Template",
  "name": "DarkMode Solution Template",
  "shortName": "darkmode",
  "sourceName": "DarkMode",
  "preferNameDirectory": true,

  "symbols": {
    "SolutionName": {
      "type": "parameter",
      "datatype": "string",
      "defaultValue": "DarkMode",
      "replaces": "DarkMode"
    },

    "api": {
      "type": "parameter",
      "datatype": "bool",
      "defaultValue": "true",
      "description": "Include API projects"
    },

    "portal": {
      "type": "parameter",
      "datatype": "bool",
      "defaultValue": "true",
      "description": "Include Portal (Blazor) projects"
    },

    "tests": {
      "type": "parameter",
      "datatype": "bool",
      "defaultValue": "true",
      "description": "Include test projects"
    }
  },

  "sources": [
    {
      "modifiers": [
        {
          "condition": "(!api)",
          "exclude": [ "API/**" ]
        },
        {
          "condition": "(!portal)",
          "exclude": [ "Portal/**" ]
        },
        {
          "condition": "(!tests)",
          "exclude": [
            "API/Tests/**",
            "Portal/Tests/**"
          ]
        },
        {
          "exclude": [
            "**/.template.config/**",
            "**/bin/**",
            "**/obj/**"
          ]
        }
      ]
    }
  ]
}
```

---

# 3. Project-level template.json files  
These ensure project names + namespaces update correctly.

---

## API template.json  
Create:

```
API/.template.config/template.json
```

```json
{
  "$schema": "http://json.schemastore.org/template",
  "identity": "DarkMode.API.Template",
  "name": "DarkMode API",
  "shortName": "darkmode-api",
  "sourceName": "DarkMode.API",
  "preferNameDirectory": false
}
```

---

## Portal template.json  
Create:

```
Portal/.template.config/template.json
```

```json
{
  "$schema": "http://json.schemastore.org/template",
  "identity": "DarkMode.Blazor.Template",
  "name": "DarkMode Blazor Portal",
  "shortName": "darkmode-portal",
  "sourceName": "DarkMode.Blazor",
  "preferNameDirectory": false
}
```

---

# 4. Automatic renaming of test projects  
Because `sourceName` cascades, all these will be renamed automatically:

- `DarkMode.API.Tests`  
- `DarkMode.API.Reqnroll.Tests`  
- `DarkMode.API.Services.Tests`  
- `DarkMode.API.Services.Reqnroll.Tests`  
- `DarkMode.Blazor.Tests`  
- `DarkMode.Blazor.Bunit.Tests`  
- `DarkMode.Blazor.Reqnroll.Tests`

No extra config needed — the root `sourceName: "DarkMode"` handles all of them.

---

# 5. Template packaging project

Create:

```
DarkMode.Template/DarkMode.Template.csproj
```

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PackageType>Template</PackageType>
    <PackageId>DarkMode.Template</PackageId>
    <Version>1.0.0</Version>
    <Authors>Bruce</Authors>
    <Description>DarkMode multi-project solution template</Description>
  </PropertyGroup>
</Project>
```

Build + install:

```
dotnet pack
dotnet new -i bin/Debug/DarkMode.Template.1.0.0.nupkg
```

---

# 6. Using your new template

### Full solution  
```
dotnet new darkmode -n MyProduct
```

### API only  
```
dotnet new darkmode -n MyProduct --portal false
```

### Portal only  
```
dotnet new darkmode -n MyProduct --api false
```

### No tests  
```
dotnet new darkmode -n MyProduct --tests false
```

### Everything off except API  
```
dotnet new darkmode -n MyProduct --portal false --tests false
```

---

#  Want me to generate the **entire template folder as a downloadable ZIP**?

I can output the full structure with every file ready to paste into your repo.  
Choose one:

- Generate ZIP‑ready folder structure
- Add more optional parameters (e.g., Swagger, Auth, Docker)
- Add CI/CD templates (GitHub Actions, Azure DevOps)

Just tell me which direction you want to take.
