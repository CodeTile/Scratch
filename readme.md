$appSettingsFile = "$(System.DefaultWorkingDirectory)\_MyBuild\drop\appsettings.json"

Write-Host "Loading: $appSettingsFile"

if (-not (Test-Path $appSettingsFile)) {
    throw "appsettings.json not found: $appSettingsFile"
}

# Load JSON
$appSettings = Get-Content $appSettingsFile -Raw | ConvertFrom-Json

function Set-JsonValue {
    param (
        [Parameter(Mandatory)]
        [object]$Object,

        [Parameter(Mandatory)]
        [string[]]$Path,

        [Parameter(Mandatory)]
        [string]$Value
    )

    $current = $Object

    # Navigate to the parent object
    for ($i = 0; $i -lt ($Path.Count - 1); $i++) {

        $property = $Path[$i]

        if ($null -eq $current.PSObject.Properties[$property]) {
            throw "JSON property '$($Path[0..$i] -join '.')' does not exist."
        }

        $current = $current.$property
    }

    $property = $Path[-1]

    if ($null -eq $current.PSObject.Properties[$property]) {
        throw "JSON property '$($Path -join '.')' does not exist."
    }

    # Preserve the existing property's JSON type
    $existingValue = $current.$property

    if ($existingValue -is [bool]) {
        $current.$property = [bool]::Parse($Value)
    }
    elseif ($existingValue -is [int]) {
        $current.$property = [int]$Value
    }
    elseif ($existingValue -is [long]) {
        $current.$property = [long]$Value
    }
    elseif ($existingValue -is [decimal]) {
        $current.$property = [decimal]$Value
    }
    elseif ($existingValue -is [double]) {
        $current.$property = [double]$Value
    }
    elseif ($existingValue -is [array]) {
        $current.$property = $Value | ConvertFrom-Json
    }
    else {
        $current.$property = $Value
    }
}

# Find all APPSETTING_* environment variables
$settings = Get-ChildItem Env:APPSETTING_*

foreach ($setting in $settings) {

    $name = $setting.Name.Substring("APPSETTING_".Length)
    $value = $setting.Value

    # __ represents nested JSON properties
    $path = $name -split "__"

    Write-Host "Applying APPSETTING_$name"

    Set-JsonValue `
        -Object $appSettings `
        -Path $path `
        -Value $value
}

# Write JSON back to disk
$appSettings |
    ConvertTo-Json -Depth 50 |
    Set-Content -Path $appSettingsFile -Encoding UTF8

Write-Host "appsettings.json updated successfully."
