Below is your complete **`README.md`**—fully regenerated, structured, and ready to drop into your repo. It documents the entire YAML‑based deployment system, including multi‑stage pipelines, templates, IIS deployment, environment transforms, and health checks.

---

# YAML‑Based IIS Deployment Pipeline  
Multi‑App, Multi‑Server, Multi‑Environment (PingPong Example)

This repository contains a full Azure DevOps YAML deployment solution for two applications:

- **API** (running on dedicated API servers)  
- **Portal** (running on dedicated App servers)

Both applications:

- Run in **separate IIS Application Pools**  
- Support **Active Directory** or **Impersonation** authentication  
- Are deployed to **physical on‑prem servers** behind a load balancer  
- Use **environment‑specific appsettings.json transforms**  
- Are built in a separate YAML pipeline that publishes zipped artifacts  
- Are deployed using **YAML only** (no Classic Release)

The example environment used throughout this README is **PingPong**.

---

## 📦 Build Pipeline (Artifacts)

Your build pipeline produces two zipped artifacts:

```
$(Build.ArtifactStagingDirectory)/api
$(Build.ArtifactStagingDirectory)/portal
```

These artifacts are consumed by the release pipeline described below.

---

## 🚀 Release Pipeline Overview

The release pipeline is a **multi‑stage YAML pipeline** that:

1. Downloads the API and Portal artifacts  
2. Expands them  
3. Applies environment‑specific transforms  
4. Deploys to multiple IIS servers  
5. Restarts app pools  
6. Performs **per‑server health checks**  
7. Fails the deployment if any node is unhealthy  

---

## 🧱 Folder Structure

```
/azure-pipelines-release.yml
/templates/
    deploy-iis.yml
/README.md
```

---

## 🔐 Environment Variables (PingPong)

Environment‑specific values are stored in an Azure DevOps **Variable Group** named `PingPong`.

Example variables:

```
ApiKey = 12345-ENV-PINGPONG
ConnectionStrings__Default = Server=...;Database=...;
Auth__Mode = ActiveDirectory
```

---

## 🏗️ Release Pipeline (`azure-pipelines-release.yml`)

```yaml
trigger: none

resources:
  pipelines:
  - pipeline: buildApiPortal
    source: api-portal-build-pipeline
    trigger: none

variables:
- group: PingPong

stages:
- stage: Deploy_PingPong
  displayName: Deploy to PingPong
  jobs:

  - deployment: Deploy_API_PingPong
    displayName: Deploy API to PingPong
    environment: PingPong
    strategy:
      runOnce:
        deploy:
          steps:
          - template: templates/deploy-iis.yml
            parameters:
              appName: 'MyAPI'
              artifactName: 'api'
              servers:
                - 'myAPIServer1'
                - 'myAPIServer2'
                - 'myAPIServer3'
              sitePhysicalPath: 'C:\inetpub\wwwroot\MyAPI'
              appPoolName: 'MyAPI_AppPool'
              appSettingsPath: 'C:\inetpub\wwwroot\MyAPI\appsettings.json'
              environmentName: 'PingPong'
              authMode: 'ActiveDirectory'

  - deployment: Deploy_Portal_PingPong
    displayName: Deploy Portal to PingPong
    environment: PingPong
    strategy:
      runOnce:
        deploy:
          steps:
          - template: templates/deploy-iis.yml
            parameters:
              appName: 'MyPortal'
              artifactName: 'portal'
              servers:
                - 'myAppServer1'
                - 'myAppServer2'
              sitePhysicalPath: 'C:\inetpub\wwwroot\MyPortal'
              appPoolName: 'MyPortal_AppPool'
              appSettingsPath: 'C:\inetpub\wwwroot\MyPortal\appsettings.json'
              environmentName: 'PingPong'
              authMode: 'Impersonation'
```

---

## 🧩 IIS Deployment Template (`templates/deploy-iis.yml`)

```yaml
parameters:
  appName: ''
  artifactName: ''
  servers: []
  sitePhysicalPath: ''
  appPoolName: ''
  appSettingsPath: ''
  environmentName: ''
  authMode: ''

steps:
- checkout: self

- download: current
  artifact: ${{ parameters.artifactName }}

- task: PowerShell@2
  displayName: Expand artifact
  inputs:
    targetType: inline
    script: |
      $artifactPath = "$(Pipeline.Workspace)\${{ parameters.artifactName }}"
      $deployTemp = "$(Pipeline.Workspace)\deploy\${{ parameters.appName }}"
      New-Item -ItemType Directory -Force -Path $deployTemp | Out-Null
      Add-Type -AssemblyName System.IO.Compression.FileSystem
      [System.IO.Compression.ZipFile]::ExtractToDirectory($artifactPath, $deployTemp)

- task: PowerShell@2
  displayName: Transform appsettings.json
  inputs:
    targetType: inline
    script: |
      $appSettingsPath = Join-Path "$(Pipeline.Workspace)\deploy\${{ parameters.appName }}" "appsettings.json"
      $json = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
      $json.ApiKey = "$(ApiKey)"
      $json.AuthenticationMode = "${{ parameters.authMode }}"
      $json | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath -Encoding UTF8

- task: PowerShell@2
  displayName: Deploy to IIS servers
  inputs:
    targetType: inline
    script: |
      $servers = @(
        ${{ join(parameters.servers, ',') }}
      )

      $sourcePath = "$(Pipeline.Workspace)\deploy\${{ parameters.appName }}"
      $sitePath   = "${{ parameters.sitePhysicalPath }}"
      $appPool    = "${{ parameters.appPoolName }}"

      foreach ($server in $servers) {
        $session = New-PSSession -ComputerName $server

        Invoke-Command -Session $session -ScriptBlock {
          param($sitePath, $appPool)
          Import-Module WebAdministration
          Stop-WebAppPool -Name $appPool
          Remove-Item -Path $sitePath\* -Recurse -Force -ErrorAction SilentlyContinue
        } -ArgumentList $sitePath, $appPool

        robocopy $sourcePath "\\$server\$($sitePath.Replace(':','$'))" /MIR

        Invoke-Command -Session $session -ScriptBlock {
          param($appPool)
          Import-Module WebAdministration
          Start-WebAppPool -Name $appPool
        } -ArgumentList $appPool

        Remove-PSSession $session
      }

- task: PowerShell@2
  displayName: Health check for deployed servers
  inputs:
    targetType: inline
    script: |
      $servers = @(
        ${{ join(parameters.servers, ',') }}
      )

      $endpoint = "/health"
      $envName  = "${{ parameters.environmentName }}"
      $maxRetries = 5
      $delaySeconds = 5

      foreach ($server in $servers) {
        $url = "http://$server$endpoint"
        $healthy = $false

        for ($i = 1; $i -le $maxRetries; $i++) {
          try {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
            if ($response.StatusCode -eq 200) {
              $healthy = $true
              break
            }
          }
          catch {
            Write-Host "Attempt $i failed for $server"
          }
          Start-Sleep -Seconds $delaySeconds
        }

        if (-not $healthy) {
          throw "Health check failed for $server"
        }
      }
```

---

## 🩺 Health Check Endpoint (Recommended)

Your API should expose:

```
GET /health
```

Example JSON:

```json
{
  "status": "Healthy",
  "environment": "PingPong"
}
```

Your Portal can expose:

```
GET /status
```

---

## 🔄 Authentication Modes

### Active Directory
- App pool identity uses domain account  
- `AuthenticationMode = "ActiveDirectory"` in appsettings.json  

### Impersonation
- IIS web.config contains `<identity impersonate="true" />`  
- `AuthenticationMode = "Impersonation"` in appsettings.json  

---

## 📘 Extending the Pipeline

You can add:

- Rolling deployments  
- Blue‑green deployments  
- Warm‑up scripts  
- Load balancer health checks  
- Canary nodes  

If you want any of these added to the README, just tell me.

---

## 🧭 Next Step

Would you like me to generate:

- A **PingPong‑specific health controller** for your API  
- A **Portal health page**  
- A **diagram** of the full deployment flow  
- A **version** of this README with multiple environments (Dev/Test/Staging/Prod)

Just pick one:  
API health controller, Portal health page, or Deployment diagram.