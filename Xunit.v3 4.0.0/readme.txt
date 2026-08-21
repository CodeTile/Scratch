Absolutely. Based on your setup — .NET SDK 10.0.204, .slnx, xUnit v3 4.0.0, MTP, and a Windows Azure DevOps agent — these are the changes I'd make.

1. global.json

Keep it as:

{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}


Do not add the SDK section unless you specifically want to pin the SDK.

2. Test .csproj

Keep your existing xUnit package for now:

<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="xunit.runner.json"
             CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage"
                      Version="18.10.0" />

    <PackageReference Include="Shouldly"
                      Version="4.3.0" />

    <PackageReference Include="xunit.v3.mtp-v2"
                      Version="4.0.0" />

    <PackageReference Include="Microsoft.Testing.Extensions.AzureDevOpsReport"
                      Version="2.3.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyApplication\MyApplication.csproj" />
  </ItemGroup>

</Project>


I would not change xunit.v3 to xunit.v3.mtp-v2 yet. Your current package setup can be tested first, and changing that isn't necessary to fix the shell problem you've just identified.

3. Azure DevOps pipeline

This is the important change.

Because you're running on a Windows agent, don't use \ for line continuation.

I'd actually start with a single line to remove any ambiguity:

- script: |
    dotnet test --solution "$(solutionPath)" --configuration "$(buildConfiguration)" --no-build --coverage --coverage-output-format cobertura --report-azdo --publish-azdo-test-results
  displayName: 'Execute xUnit v3 (4.0.0) MTP Tests & Gather Coverage'


The important differences from your original are:

- dotnet test $(solutionPath) \
-   --configuration $(buildConfiguration) \
-   --no-build \
-   --coverage \
-   --coverage-output-format cobertura \
-   --report-azdo \
-   --publish-azdo-test-results

+ dotnet test --solution "$(solutionPath)" --configuration "$(buildConfiguration)" --no-build --coverage --coverage-output-format cobertura --report-azdo --publish-azdo-test-results

If you want multiline

You can use Windows cmd.exe continuation:

- script: |
    dotnet test --solution "$(solutionPath)" ^
      --configuration "$(buildConfiguration)" ^
      --no-build ^
      --coverage ^
      --coverage-output-format cobertura ^
      --report-azdo ^
      --publish-azdo-test-results
  displayName: 'Execute xUnit v3 (4.0.0) MTP Tests & Gather Coverage'


I'd use the single-line version initially.

4. Make sure the pipeline has .NET 10

Since your server has SDK 10.0.204, you don't necessarily need this if that's definitely the SDK being used by the agent.

But if this is a Microsoft-hosted or separately provisioned agent, I'd explicitly install/select .NET 10:

- task: UseDotNet@2
  displayName: 'Use .NET 10 SDK'
  inputs:
    packageType: 'sdk'
    version: '10.0.x'


Then:

- script: |
    dotnet --info
    dotnet test --solution "$(solutionPath)" --configuration "$(buildConfiguration)" --no-build --coverage --coverage-output-format cobertura --report-azdo --publish-azdo-test-results
  displayName: 'Execute xUnit v3 (4.0.0) MTP Tests & Gather Coverage'


The dotnet --info is useful while you're getting this working because you'll immediately see which SDK the pipeline is actually using.

Final setup

I'd therefore have:

global.json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}

Test project
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="xunit.runner.json"
             CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage"
                      Version="18.10.0" />
    <PackageReference Include="Shouldly"
                      Version="4.3.0" />
    <PackageReference Include="xunit.v3"
                      Version="4.0.0" />
    <PackageReference Include="Microsoft.Testing.Extensions.AzureDevOpsReport"
                      Version="2.3.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyApplication\MyApplication.csproj" />
  </ItemGroup>

</Project>

Azure DevOps
- task: UseDotNet@2
  displayName: 'Use .NET 10 SDK'
  inputs:
    packageType: 'sdk'
    version: '10.0.x'

- script: |
    dotnet --info
    dotnet test --solution "$(solutionPath)" --configuration "$(buildConfiguration)" --no-build --coverage --coverage-output-format cobertura --report-azdo --publish-azdo-test-results
  displayName: 'Execute xUnit v3 (4.0.0) MTP Tests & Gather Coverage'


The key fix is --solution "$(solutionPath)" + no \ line continuations on Windows.

If the next error is now something like Unrecognized command or argument '--coverage', that's a different issue: it would mean we've successfully reached MTP but one of the MTP extensions isn't being loaded. At that point, the exact error will tell us which package/configuration needs adjusting.