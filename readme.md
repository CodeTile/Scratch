iframe previewer

There is one caveat: an iframe `load` event alone cannot confirm successful rendering for third-party websites. But we can use it as a best-effort test.

## **1. Admin callback**

```
private void HandlePreviewTestCompleted(bool canDisplay)
{
    Card.OpenInNewTab = !canDisplay;
}
```

## **2. IframePreview\.razor**

The important part is to default to failure and only change the result when the iframe fires its load event.

```
@implements IAsyncDisposable

<div class="iframe-preview">

    @if (IsTesting)
    {
        <p>Testing website preview...</p>
    }
    else if (CanDisplay)
    {
        <p>Preview loaded.</p>
    }
    else
    {
        <p>Website cannot be previewed.</p>
    }

    @if (IsValidUrl)
    {
        <iframe @key="Url"
                src="@Url"
                title="Website preview"
                class="iframe-preview-frame"
                @onload="HandleLoadAsync">
        </iframe>
    }

</div>

@code {
    [Parameter, EditorRequired]
    public string Url { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<bool> OnTestCompleted { get; set; }

    private bool IsTesting { get; set; }

    private bool CanDisplay { get; set; }

    private bool IsValidUrl { get; set; }

    private bool TestCompleted { get; set; }

    private CancellationTokenSource? TimeoutSource;

    protected override async Task OnParametersSetAsync()
    {
        TimeoutSource?.Cancel();
        TimeoutSource?.Dispose();

        TestCompleted = false;
        CanDisplay = false;
        IsTesting = false;

        IsValidUrl =
            Uri.TryCreate(Url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttps ||
             uri.Scheme == Uri.UriSchemeHttp);

        if (!IsValidUrl)
        {
            TestCompleted = true;
            await OnTestCompleted.InvokeAsync(false);
            return;
        }

        IsTesting = true;

        TimeoutSource = new CancellationTokenSource();

        _ = MonitorTimeoutAsync(TimeoutSource.Token);
    }

    private async Task MonitorTimeoutAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(10),
                cancellationToken);

            await InvokeAsync(async () =>
            {
                if (!TestCompleted)
                {
                    await CompleteTestAsync(false);
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Expected when the test completes.
        }
    }

    private async Task HandleLoadAsync()
    {
        if (!TestCompleted)
        {
            await CompleteTestAsync(true);
        }
    }

    private async Task CompleteTestAsync(bool canDisplay)
    {
        if (TestCompleted)
        {
            return;
        }

        TestCompleted = true;
        IsTesting = false;
        CanDisplay = canDisplay;

        TimeoutSource?.Cancel();

        await OnTestCompleted.InvokeAsync(canDisplay);
    }

    public ValueTask DisposeAsync()
    {
        TimeoutSource?.Cancel();
        TimeoutSource?.Dispose();

        return ValueTask.CompletedTask;
    }
}
```

## **3. Admin page**

```
<IframePreview @key="Card.Url"
               Url="@Card.Url"
               OnTestCompleted="HandlePreviewTestCompleted" />
```

```
private void HandlePreviewTestCompleted(bool canDisplay)
{
    Card.OpenInNewTab = !canDisplay;
}
```

**Important:** This implementation handles invalid URLs and timeouts correctly, but a website that blocks iframe embedding can still trigger `onload`. There is no universally reliable browser-only way to distinguish that case for arbitrary external websites.

For your admin workflow, I'd treat this as a best-effort preview check and let the administrator manually override `OpenInNewTab` if necessary.
=========================================================================================
=========================================================================================
=========================================================================================

#Navigation control

1. NavigableFluentCard.razor – your reusable Fluent UI card, accepting your existing model.
2. WebsiteViewer.razor – a reusable website viewer that displays an external website in an iframe, with close and refresh controls.

The implementation targets .NET 10, C# 14 and Fluent UI Blazor v5.

It does not introduce another model or DTO. You only need to substitute `YourExistingCardModel` and its property names with those from your application.

## 1. Component architecture

Your existing card model

URL · Title · OpenInNewTab · Other properties

NavigableFluentCard.razor

Generic reusable FluentCard component

New tab

Native anchor navigation

Embedded

WebsiteViewer component

An improvement here: I'm making the custom card component generic (`TCard`). This means you can pass your existing model without having to tell the component what your model class is called or create another DTO.

The component receives selectors for the URL, title and navigation preference.

## 2. NavigableFluentCard.razor

```

@typeparam TCard

@using Microsoft.AspNetCore.Components.Web
@using Microsoft.FluentUI.AspNetCore.Components

<div class="navigable-card-container">

    @if (ShouldOpenInNewTab)
    {
        <a href="@CardUrl"
           target="_blank"
           rel="noopener noreferrer"
           class="navigable-card-link">

            <FluentCard Width="@CardWidth"
                        Height="@CardHeight">
                @ChildContent(Card)
            </FluentCard>

        </a>
    }
    else
    {
        <FluentCard Width="@CardWidth"
                    Height="@CardHeight"
                    OnClick="HandleCardClickAsync">

            @ChildContent(Card)

        </FluentCard>
    }

    @if (!ShouldOpenInNewTab && IsViewerVisible)
    {
        <WebsiteViewer @key="CardUrl"
                       Url="@CardUrl"
                       Title="@CardTitle"
                       OnClose="CloseViewerAsync" />
    }

</div>

@code {
    [Parameter, EditorRequired]
    public TCard Card { get; set; } = default!;

    [Parameter, EditorRequired]
    public Func<TCard, string> UrlSelector { get; set; } = default!;

    [Parameter, EditorRequired]
    public Func<TCard, string> TitleSelector { get; set; } = default!;

    [Parameter, EditorRequired]
    public Func<TCard, bool> OpenInNewTabSelector { get; set; } = default!;

    [Parameter, EditorRequired]
    public RenderFragment<TCard> ChildContent { get; set; } = default!;

    [Parameter]
    public string? CardWidth { get; set; }

    [Parameter]
    public string? CardHeight { get; set; }

    private bool IsViewerVisible { get; set; }

    private string CardUrl => UrlSelector(Card);

    private string CardTitle => TitleSelector(Card);

    private bool ShouldOpenInNewTab =>
        OpenInNewTabSelector(Card);

    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Card);
        ArgumentNullException.ThrowIfNull(UrlSelector);
        ArgumentNullException.ThrowIfNull(TitleSelector);
        ArgumentNullException.ThrowIfNull(OpenInNewTabSelector);
        ArgumentNullException.ThrowIfNull(ChildContent);

        if (!Uri.TryCreate(CardUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "The card URL must be an absolute HTTP or HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(CardTitle))
        {
            throw new InvalidOperationException(
                "The card title cannot be empty.");
        }

        if (ShouldOpenInNewTab)
        {
            IsViewerVisible = false;
        }
    }

    private Task HandleCardClickAsync(MouseEventArgs args)
    {
        IsViewerVisible = true;
        return Task.CompletedTask;
    }

    private Task CloseViewerAsync()
    {
        IsViewerVisible = false;
        return Task.CompletedTask;
    }
}

```

## 3. WebsiteViewer.razor

This is the actual embedded website viewer.

It includes:

- A header showing the website title.
- A refresh button.
- An open-in-new-tab button.
- A close button.
- A full-width iframe.
- A responsive layout.

```

@using Microsoft.FluentUI.AspNetCore.Components

<div class="website-viewer">

    <div class="website-viewer-header">

        <div class="website-viewer-title">
            <FluentLabel Typo="Typography.PaneHeader">
                @Title
            </FluentLabel>

            <span class="website-viewer-address">
                @Url
            </span>
        </div>

        <div class="website-viewer-actions">

            <FluentButton Appearance="Appearance.Stealth"
                          Title="Refresh website"
                          OnClick="RefreshAsync">
                <FluentIcon Value="@(new Icons.Regular.Size20.ArrowClockwise())" />
            </FluentButton>

            <a href="@Url"
               target="_blank"
               rel="noopener noreferrer"
               class="website-viewer-external-link"
               title="Open in new tab">
                <FluentIcon Value="@(new Icons.Regular.Size20.Open())" />
            </a>

            <FluentButton Appearance="Appearance.Stealth"
                          Title="Close viewer"
                          OnClick="CloseAsync">
                <FluentIcon Value="@(new Icons.Regular.Size20.Dismiss())" />
            </FluentButton>

        </div>

    </div>

    <div class="website-viewer-content">

        <iframe @key="RefreshVersion"
                src="@Url"
                title="@Title"
                class="website-viewer-iframe"
                loading="eager"
                referrerpolicy="strict-origin-when-cross-origin">
        </iframe>

    </div>

</div>

@code {
    [Parameter, EditorRequired]
    public string Url { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public EventCallback OnClose { get; set; }

    private int RefreshVersion { get; set; }

    protected override void OnParametersSet()
    {
        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "The viewer URL must be an absolute HTTP or HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidOperationException(
                "The viewer title cannot be empty.");
        }
    }

    private Task RefreshAsync()
    {
        RefreshVersion++;
        return Task.CompletedTask;
    }

    private Task CloseAsync()
    {
        return OnClose.InvokeAsync();
    }
}

```

## 4. NavigableFluentCard.razor.css

```

.navigable-card-container {
    display: flex;
    flex-direction: column;
    gap: 12px;
    width: 100%;
}

.navigable-card-link {
    display: block;
    color: inherit;
    text-decoration: none;
}

.navigable-card-link fluent-card {
    cursor: pointer;
}

.website-viewer {
    display: flex;
    flex-direction: column;
    width: 100%;
    min-height: 650px;
    overflow: hidden;
    border: 1px solid var(--neutral-stroke-divider-rest);
    border-radius: 8px;
    background: var(--neutral-layer-1);
}

.website-viewer-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    padding: 12px 16px;
    border-bottom: 1px solid var(--neutral-stroke-divider-rest);
}

.website-viewer-title {
    display: flex;
    flex-direction: column;
    min-width: 0;
    gap: 4px;
}

.website-viewer-address {
    overflow: hidden;
    color: var(--neutral-foreground-hint);
    font-size: 12px;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.website-viewer-actions {
    display: flex;
    flex-shrink: 0;
    align-items: center;
    gap: 8px;
}

.website-viewer-actions button,
.website-viewer-external-link {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-height: 36px;
    padding: 6px 10px;
    border: 1px solid var(--neutral-stroke-divider-rest);
    border-radius: 6px;
    background: var(--neutral-layer-2);
    color: var(--neutral-foreground-rest);
    font: inherit;
    font-size: 13px;
    text-decoration: none;
    cursor: pointer;
}

.website-viewer-actions button:hover,
.website-viewer-external-link:hover {
    background: var(--neutral-layer-3);
}

.website-viewer-content {
    flex: 1;
    min-height: 600px;
}

.website-viewer-iframe {
    display: block;
    width: 100%;
    height: 600px;
    border: 0;
    background: white;
}

@media (max-width: 600px) {
    .website-viewer-header {
        align-items: flex-start;
        flex-direction: column;
    }

    .website-viewer-actions {
        width: 100%;
        justify-content: flex-end;
    }
}

```

## 5. WebsiteViewer.razor — small adjustment

To keep the viewer independent of Fluent UI icon APIs, use ordinary HTML buttons. Replace the `website-viewer-actions` section in the previous component with:

```

<div class="website-viewer-actions">

    <button type="button"
            title="Refresh website"
            @onclick="RefreshAsync">
        Refresh
    </button>

    <a href="@Url"
       target="_blank"
       rel="noopener noreferrer"
       class="website-viewer-external-link">
        Open in new tab
    </a>

    <button type="button"
            title="Close viewer"
            @onclick="CloseAsync">
        Close
    </button>

</div>

```

This avoids any dependency on specific Fluent UI icon names or button appearance enums.

## 6. How to use it with your existing model

For this example, imagine your existing model has the following properties:

- `Url`
- `Title`
- `Description`
- `OpenInNewTab`

You don't need to change your model.

Example parent component:

```

@using Microsoft.FluentUI.AspNetCore.Components

@foreach (var item in Cards)
{
    <NavigableFluentCard TCard="YourExistingCardModel"
                         Card="@item"
                         UrlSelector="@(card => card.Url)"
                         TitleSelector="@(card => card.Title)"
                         OpenInNewTabSelector="@(card => card.OpenInNewTab)"
                         CardWidth="100%"
                         CardHeight="150px">

        <ChildContent Context="card">

            <FluentStack Padding="16">
                <FluentLabel Typo="Typography.PaneHeader">
                    @card.Title
                </FluentLabel>

                <FluentLabel>
                    @card.Description
                </FluentLabel>
            </FluentStack>

        </ChildContent>

    </NavigableFluentCard>
}

```

Replace `YourExistingCardModel` with your actual class name and adjust the selectors if your property names differ.

## 7. Final WebsiteViewer.razor

For clarity, this is the complete corrected version to save as the actual file (use this instead of the earlier draft in section 3).

```

@using Microsoft.AspNetCore.Components

<div class="website-viewer">

    <div class="website-viewer-header">

        <div class="website-viewer-title">
            <strong>@Title</strong>
            <span class="website-viewer-address">@Url</span>
        </div>

        <div class="website-viewer-actions">

            <button type="button"
                    title="Refresh website"
                    @onclick="RefreshAsync">
                Refresh
            </button>

            <a href="@Url"
               target="_blank"
               rel="noopener noreferrer"
               class="website-viewer-external-link">
                Open in new tab
            </a>

            <button type="button"
                    title="Close viewer"
                    @onclick="CloseAsync">
                Close
            </button>

        </div>

    </div>

    <div class="website-viewer-content">

        <iframe @key="RefreshVersion"
                src="@Url"
                title="@Title"
                class="website-viewer-iframe"
                loading="eager"
                referrerpolicy="strict-origin-when-cross-origin">
        </iframe>

    </div>

</div>

@code {
    [Parameter, EditorRequired]
    public string Url { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public EventCallback OnClose { get; set; }

    private int RefreshVersion { get; set; }

    protected override void OnParametersSet()
    {
        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
             uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                "The viewer URL must be an absolute HTTP or HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidOperationException(
                "The viewer title cannot be empty.");
        }
    }

    private Task RefreshAsync()
    {
        RefreshVersion++;
        return Task.CompletedTask;
    }

    private Task CloseAsync()
    {
        return OnClose.InvokeAsync();
    }
}

```

## 8. Project requirements

Ensure your project already references:

```
<PackageReference Include="Microsoft.FluentUI.AspNetCore.Components"
                  Version="5.*" />
```

And that Fluent UI is registered in `Program.cs`:

```
builder.Services.AddFluentUIComponents();
```

The application must also have an interactive Blazor render mode for card click events to work.&#x20;

[image](https://www.google.com/s2/favicons?domain=https://github.com\&sz=32)

GitHub+1



## 9. Final file structure

```
Components/
    Cards/
        NavigableFluentCard.razor
        NavigableFluentCard.razor.css

    Viewers/
        WebsiteViewer.razor
```

Implementation summary

- Existing model retained.
- No additional DTOs.
- Generic card component.
- New-tab navigation supported.
- Embedded website viewer supported.
- Refresh and close controls.
- HTTP/HTTPS URL validation.
- Responsive layout.
- No JavaScript required.

One final technical caveat: Google and other websites may refuse iframe embedding. The implementation cannot override those restrictions.

For your work machine, the three files to copy are `NavigableFluentCard.razor`, `WebsiteViewer.razor` and `NavigableFluentCard.razor.css`.