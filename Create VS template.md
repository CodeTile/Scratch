

# ✅ 1. Final Directory Structure (Flattened)

```
Dark.Mode.Template/
│
├── .template.config/
│   └── template.json
│
├── Dark.Mode.API/
│   └── Dark.Mode.API.csproj
│
├── Dark.Mode.API.Tests/
│   └── Dark.Mode.API.Tests.csproj
│
├── Dark.Mode.Services.Tests/
│   └── Dark.Mode.Services.Tests.csproj
│
├── Dark.Mode.Common/
│   └── Dark.Mode.Common.csproj
│
├── Dark.Mode.Common.Tests/
│   └── Dark.Mode.Common.Tests.csproj
│
├── Dark.Mode.Blazor/
│   └── Dark.Mode.Blazor.csproj
│
├── Dark.Mode.Blazor.Tests/
│   └── Dark.Mode.Blazor.Tests.csproj
│
└── Dark.Mode.sln
```

No nested folders. No options. Everything is always included.

---

# 🎛️ 2. Root template.json (the only template file you need)

Place this inside:

```
Dark.Mode.Template/.template.config/template.json
```

This template:

- Renames **Dark.Mode** everywhere (solution, projects, namespaces)
- Copies all folders
- Excludes build artifacts
- Has **no optional parameters**

```json
{
  "$schema": "http://json.schemastore.org/template",
  "author": "CodeTile",
  "classifications": [ "Solution", "MultiProject" ],
  "identity": "Dark.Mode.Solution.Template",
  "name": "Dark.Mode Solution Template",
  "shortName": "darkmode",
  "sourceName": "Dark.Mode",
  "preferNameDirectory": true,

  "symbols": {
    "SolutionName": {
      "type": "parameter",
      "datatype": "string",
      "defaultValue": "Dark.Mode",
      "replaces": "Dark.Mode"
    }
  },

  "sources": [
    {
      "modifiers": [
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

That’s it — **no project‑level template.json files are needed** because you are not offering options and you want everything copied as‑is.

---

# 🔄 3. Automatic renaming behavior

Because the template uses:

```
"sourceName": "Dark.Mode"
```

The following are all renamed automatically:

- Dark.Mode.API  
- Dark.Mode.API.Tests  
- Dark.Mode.Services.Tests  
- Dark.Mode.Common  
- Dark.Mode.Common.Tests  
- Dark.Mode.Blazor  
- Dark.Mode.Blazor.Tests  
- Dark.Mode.sln  

Every namespace, folder name, and project name containing **Dark.Mode** will be replaced with the new solution name.

---

# 📦 4. Packaging project (root)

Create:

```
Dark.Mode.Template/Dark.Mode.Template.csproj
```

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PackageType>Template</PackageType>
    <PackageId>Dark.Mode.Template</PackageId>
    <Version>1.0.0</Version>
    <Authors>CodeTile</Authors>
    <Description>Dark.Mode multi-project solution template</Description>
  </PropertyGroup>
</Project>
```

Build + install:

```
dotnet pack
dotnet new -i bin/Debug/Dark.Mode.Template.1.0.0.nupkg
```

---

# 🚀 5. Using your template

```
dotnet new darkmode -n MyNewSolution
```

This will generate:

```
MyNewSolution/
├── MyNewSolution.API/
├── MyNewSolution.API.Tests/
├── MyNewSolution.Services.Tests/
├── MyNewSolution.Common/
├── MyNewSolution.Common.Tests/
├── MyNewSolution.Blazor/
├── MyNewSolution.Blazor.Tests/
└── MyNewSolution.sln
```

All namespaces and project names will be updated accordingly.

---

# Want me to generate a **ZIP‑ready version** of this template?

I can output the entire folder structure with placeholder files so you can drop it straight into your repo.

Choose one:

- Generate ZIP‑ready template folder
- Add optional parameters (API only, Blazor only, Tests toggle)
- Add CI/CD templates (GitHub Actions, Azure DevOps)

Just tell me what you want next.