I'll adapt the category-management page to Fluent UI Blazor v4.x with .NET 10 and Interactive Server, keeping your REST API / `HttpClient` architecture and all five fields:

- Fluent UI icon name
- Name
- `IsEnabled` — inline toggle
- Display order
- `ParentId` — dropdown

I'll also keep the Add/Edit dialog, row actions menu, validation and delete confirmation. The main changes are the package version and ensuring the component APIs match v4 rather than v5.

The latest stable v4 release I found is 4.14.5, which supports .NET 10.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://www.nuget.org\&sz=32)

Microsoft.FluentUI.AspNetCore.Components 4.14.5



## 1. Change the package version

```
dotnet add package Microsoft.FluentUI.AspNetCore.Components --version 4.14.5
```

If you already installed the v5 icon package, either align its version with the v4 release or remove it if you don't use it. The category page below stores icon names as strings, so it doesn't require the separate icon package.

## 2. Use the v4 setup

In `Program.cs`, retain your existing .NET 10 Interactive Server configuration and register Fluent UI and your API client:

```
builder.Services.AddHttpClient();

builder.Services.AddFluentUIComponents();

builder.Services.AddHttpClient<CategoryApiClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ApiBaseUrl"]
        ?? "https://localhost:7001/");
});
```

In `Components/_Imports.razor`:

```
@using Microsoft.FluentUI.AspNetCore.Components
@using YourApp.Models
@using YourApp.Services
@using Microsoft.AspNetCore.Components.Forms
```

In `MainLayout.razor`, use the v4 providers:

```
<FluentToastProvider />
<FluentDialogProvider />
<FluentTooltipProvider />
<FluentMessageBarProvider />
```

Keep the CSS bundle reference in the document head:

```
<link href="_content/Microsoft.FluentUI.AspNetCore.Components/Microsoft.FluentUI.AspNetCore.Components.bundle.scp.css"
      rel="stylesheet" />
```

## 3. What changes in the category page?

Your model (`CategoryDto.cs`) and REST API client (`CategoryApiClient.cs`) can remain unchanged. The main UI changes are these:

- Use `Appearance.Accent` and `Appearance.Neutral` for v4 button styling.
- Keep `FluentDataGrid`, `PropertyColumn` and `TemplateColumn`.
- Keep the custom modal and row menu; neither depends on v5's dialog API.
- Keep the `InputCheckbox` for the enabled state and `InputSelect` for `ParentId`.

For example, the v4 Add button is:

```
<FluentButton Appearance="Appearance.Accent"
              OnClick="OpenCreateDialog">
    Add category
</FluentButton>
```

And the v4 grid retains this structure:

```
<FluentDataGrid TGridItem="CategoryDto"
                Items="@_categories.AsQueryable()"
                ItemKey="@(c => c.Id)">

    <PropertyColumn TGridItem="CategoryDto"
                    Property="@(c => c.IconName)"
                    Title="Icon name" />

    <PropertyColumn TGridItem="CategoryDto"
                    Property="@(c => c.Name)"
                    Title="Name" />

    <TemplateColumn TGridItem="CategoryDto" Title="Enabled">
        <InputCheckbox @bind-Value="context.IsEnabled" />
    </TemplateColumn>

    <PropertyColumn TGridItem="CategoryDto"
                    Property="@(c => c.DisplayOrder)"
                    Title="Order" />

    <TemplateColumn TGridItem="CategoryDto" Title="Parent">
        @ParentName(context.ParentId)
    </TemplateColumn>

    <TemplateColumn TGridItem="CategoryDto" Title="Actions">
        @* Keep the Edit/Delete row menu from the earlier page. *@
    </TemplateColumn>
</FluentDataGrid>
```

Important: the checkbox above only changes the bound object. To persist the change through your REST API, keep the earlier `ToggleEnabledAsync` handler and use `Value`, `ValueChanged` and `ValueExpression` rather than a simple `@bind-Value`.

The rest of the earlier `Categories.razor` page and its CSS can be retained, subject to compiling against your exact v4 package. The complete API client, validation, parent-cycle checks and save/delete handlers don't need to be rewritten just to change Fluent UI versions.

For the v4 component API reference, use the [Fluent UI Blazor v4 documentation](https://www.fluentui-blazor.net/), and verify the project with:

```
dotnet restore
dotnet build
```