Here you go, Bruce — **a complete, clean, fully‑regenerated set of instructions** for building your multi‑project `.NET` solution template using your **new directory structure** with `src/`, including:

- Full folder layout  
- Root template.json  
- Project‑level template.json files  
- Optional parameters (API, Portal, Tests)  
- Automatic renaming of solution, projects, namespaces  
- Packaging instructions  

Everything is rewritten from scratch for clarity and correctness.

---

# ✅ 1. Final Template Folder Structure (with `src/`)

```
DarkMode.Template/
│
├── DarkMode.Template.csproj
│
└── src/
    ├── .template.config/
    │   └── template.json        <-- ROOT template
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
    │       └── template.json    <-- API project template
    │
    ├── Portal/
    │   ├── DarkMode.Blazor.csproj
    │   ├── Tests/
    │   │   ├── DarkMode.Blazor.Tests.csproj
    │   │   ├── DarkMode.Blazor.Bunit.Tests.csproj
    │   │   ├── DarkMode.Blazor.Reqnroll.Tests.csproj
    │   └── .template.config/
    │       └── template.json    <-- Portal project template
    │
    └── DarkMode.sln
```

---

# 🎛️ 2. Root template.json (inside `src/.template.config/`)

This controls:

- Solution renaming  
- Project + namespace renaming  
- Optional parameters  
- Conditional folder inclusion  
- Excluding build artifacts  

```
src/.template.config/template.json
```

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

# 🧩 3. Project‑level template.json files  
These ensure project names + namespaces update correctly.

---

## API template.json  
```
src/API/.template.config/template.json
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
```
src/Portal/.template.config/template.json
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

# 🔄 4. Automatic renaming  
Because the root template uses:

```
"sourceName": "DarkMode"
```

Every file, folder, namespace, and project name containing **DarkMode** will be replaced with the new solution name.

That includes:

- DarkMode.API  
- DarkMode.API.Services  
- DarkMode.API.Tests  
- DarkMode.API.Services.Reqnroll.Tests  
- DarkMode.Blazor  
- DarkMode.Blazor.Bunit.Tests  
- DarkMode.Blazor.Reqnroll.Tests  

No extra config needed.

---

# 📦 5. Packaging project (root)

```
DarkMode.Template.csproj
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

# 🚀 6. Using your template

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

---

# Want me to generate the **ZIP‑ready folder**, with placeholder files and everything arranged exactly as shown?

You can choose:

- Generate ZIP‑ready template folder
- Add optional features like Swagger, Auth, Docker
- Add CI/CD templates (GitHub Actions, Azure DevOps)

Just tell me which direction you want to go.