
- A top‑level `DevOps/` folder  
- A `src/` folder containing all projects  
- A single solution file  
- No optional parameters  
- Automatic renaming of **Dark.Mode** everywhere  
- A single root `.template.config/template.json`  
- Full compatibility with both **Visual Studio** and **dotnet new**

Everything below is the final, authoritative version for your new structure.

---

# ✅ 1. Final Directory Structure (Updated)

```
DarkMode.Template/
│
├── DarkMode.Template.csproj
│
├── DevOps/
│   ├── main.yaml
│   ├── develop.yaml
│
└── src/
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

✔ **This structure is perfect for Visual Studio templates**  
✔ **All projects included**  
✔ **DevOps folder included**  
✔ **No nested template configs needed**  

---

# 🎛️ 2. Root template.json (the only template file you need)

Place this inside:

```
DarkMode.Template/.template.config/template.json
```

This template:

- Renames **Dark.Mode** everywhere (solution, projects, namespaces)
- Copies *everything* under `src/` and `DevOps/`
- Excludes build artifacts
- Has **no optional parameters**
- Works in **Visual Studio** and **dotnet new**

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
- Namespaces inside all `.cs` files  
- Folder names  
- Project references  

No additional configuration is required.

---

# 📦 4. Packaging project (root)

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
    <Authors>CodeTile</Authors>
    <Description>Dark.Mode multi-project solution template</Description>
  </PropertyGroup>
</Project>
```

Build + install:

```
dotnet pack
dotnet new -i bin/Debug/DarkMode.Template.1.0.0.nupkg
```

Visual Studio will automatically detect it.

---

# 🚀 5. Using your template

```
dotnet new darkmode -n MyNewSolution
```

This generates:

```
MyNewSolution/
│
├── DevOps/
│   ├── main.yaml
│   ├── develop.yaml
│
├── src/
│   ├── MyNewSolution.API/
│   ├── MyNewSolution.API.Tests/
│   ├── MyNewSolution.Services.Tests/
│   ├── MyNewSolution.Common/
│   ├── MyNewSolution.Common.Tests/
│   ├── MyNewSolution.Blazor/
│   ├── MyNewSolution.Blazor.Tests/
│   └── MyNewSolution.sln
```

#######################################################################################

### Test and Create solution

Save as Test-template.ps1

```
# ================================
# Ask for solution name
# ================================
$solutionName = Read-Host "Enter solution name (default: MyTestSolution)"
if ([string]::IsNullOrWhiteSpace($solutionName)) {
    $solutionName = "MyTestSolution"
}

Write-Host "Using solution name: $solutionName"

# ================================
# Clean build folders
# ================================
Write-Host "=== Cleaning build folders ==="
if (Test-Path ".\bin") { Remove-Item ".\bin\*" -Recurse -Force }
if (Test-Path ".\obj") { Remove-Item ".\obj\*" -Recurse -Force }

# ================================
# Uninstall all existing versions
# ================================
Write-Host "=== Uninstalling existing DarkMode.Template packages ==="
$installed = dotnet new uninstall | Select-String "DarkMode.Template"
while ($installed) {
    dotnet new uninstall DarkMode.Template | Out-Null
    $installed = dotnet new uninstall | Select-String "DarkMode.Template"
}

# ================================
# Pack template
# ================================
Write-Host "=== Packing template ==="
dotnet pack .\DarkMode.Template.csproj -o .\nupkg

# ================================
# Locate newest .nupkg
# ================================
Write-Host "=== Locating package ==="
$pkg = Get-ChildItem .\nupkg\DarkMode.Template*.nupkg | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $pkg) {
    Write-Error "No .nupkg file found. Pack failed."
    exit 1
}

Write-Host "=== Installing template: $($pkg.Name) ==="
dotnet new install $pkg.FullName

# ================================
# Prepare test output folder
# ================================
Write-Host "=== Preparing test output folder ==="
if (Test-Path ".\TestOutput") { Remove-Item ".\TestOutput" -Recurse -Force }
New-Item -ItemType Directory -Path ".\TestOutput" | Out-Null
Set-Location ".\TestOutput"

# ================================
# Generate solution
# ================================
Write-Host "=== Generating test solution ==="
dotnet new darkmode -n $solutionName

# ================================
# Validate folder structure
# ================================
Write-Host "=== Validating folder structure ==="

$expectedFolders = @(
    "DevOps",
    "src",
    "src/$solutionName.API",
    "src/$solutionName.Blazor",
    "src/$solutionName.Common",
    "src/$solutionName.API.Tests",
    "src/$solutionName.Blazor.Tests",
    "src/$solutionName.Common.Tests"
)

foreach ($folder in $expectedFolders) {
    if (-not (Test-Path $folder)) {
        Write-Error "Missing expected folder: $folder"
        exit 1
    }
}

# ================================
# Validate solution file
# ================================
Write-Host "=== Validating solution file ==="
if (-not (Test-Path "src/$solutionName.sln")) {
    Write-Error "Solution file missing or incorrectly named."
    exit 1
}

# ================================
# Check for leftover template names
# ================================
Write-Host "=== Checking for leftover 'Dark.Mode' references ==="
$leftovers = Get-ChildItem -Recurse -File | Select-String "Dark\.Mode"

if ($leftovers) {
    Write-Error "Found leftover 'Dark.Mode' references:"
    $leftovers | ForEach-Object { Write-Host $_.Path }
    exit 1
}

Write-Host "=== Folder and project name validation passed ==="

# ================================
# Run unit tests
# ================================
Write-Host "=== Running unit tests ==="
Set-Location ".\$solutionName"

$testResult = dotnet test --no-build

if ($LASTEXITCODE -ne 0) {
    Write-Error "Unit tests failed."
    exit 1
}

Write-Host "=== All tests passed successfully ==="
Write-Host "=== Template test completed successfully ==="


```