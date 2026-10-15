Below is the regenerated `Categories.razor` code and CSS, incorporating the changes discussed:

- Save changes is disabled until a field changes.
- Save becomes disabled again if all fields are restored to their original values.
- Save and the form fields are disabled while saving.
- The Display order input is fixed at 60px wide.
- The original category is copied separately so editing doesn't modify the grid data before saving.

## `Categories.razor`

```

@using Microsoft.AspNetCore.Components.Forms

@if (_dialogOpen && _edit is not null)
{
    <div class="edit-modal-backdrop">
        <section class="edit-modal"
                 role="dialog"
                 aria-modal="true"
                 aria-labelledby="edit-category-title">

            <header class="edit-modal-header">
                <h2 id="edit-category-title">Edit category</h2>

                <button type="button"
                        class="close-button"
                        aria-label="Close"
                        disabled="@_saving"
                        @onclick="CloseEdit">
                    &times;
                </button>
            </header>

            <EditForm Model="_edit" OnValidSubmit="SaveEditAsync">
                <DataAnnotationsValidator />

                <div class="edit-modal-body">

                    <div class="form-field">
                        <label for="category-name">Name</label>
                        <InputText id="category-name"
                                   class="form-control"
                                   @bind-Value="_edit.Name"
                                   disabled="@_saving" />
                        <ValidationMessage For="@(() => _edit.Name)" />
                    </div>

                    <div class="form-field">
                        <label for="category-icon">Fluent UI icon name</label>
                        <InputText id="category-icon"
                                   class="form-control"
                                   @bind-Value="_edit.IconName"
                                   disabled="@_saving" />
                        <ValidationMessage For="@(() => _edit.IconName)" />
                    </div>

                    <div class="form-field">
                        <label for="category-parent">Parent category</label>
                        <InputSelect id="category-parent"
                                     class="form-control"
                                     @bind-Value="_edit.ParentId"
                                     disabled="@_saving">
                            <option value="">No parent (root)</option>

                            @foreach (var parent in AvailableParents())
                            {
                                <option value="@parent.Id">
                                    @parent.Name
                                </option>
                            }
                        </InputSelect>
                    </div>

                    <div class="form-field">
                        <label for="category-order">Display order</label>
                        <InputNumber id="category-order"
                                     class="form-control category-order-input"
                                     @bind-Value="_edit.DisplayOrder"
                                     disabled="@_saving" />
                        <ValidationMessage For="@(() => _edit.DisplayOrder)" />
                    </div>

                    <div class="form-checkbox">
                        <InputCheckbox id="category-enabled"
                                       @bind-Value="_edit.IsEnabled"
                                       disabled="@_saving" />
                        <label for="category-enabled">Enabled</label>
                    </div>

                    @if (!string.IsNullOrWhiteSpace(_formError))
                    {
                        <div class="form-error" role="alert">
                            @_formError
                        </div>
                    }
                </div>

                <footer class="edit-modal-footer">
                    <button type="button"
                            class="button-secondary"
                            disabled="@_saving"
                            @onclick="CloseEdit">
                        Cancel
                    </button>

                    <button type="submit"
                            class="button-primary"
                            disabled="@(_saving || !HasChanges)">
                        @(_saving ? "Saving..." : "Save changes")
                    </button>
                </footer>
            </EditForm>
        </section>
    </div>
}

@code {
    private CategoryDto? _edit;
    private CategoryDto? _original;

    private bool _dialogOpen;
    private bool _saving;
    private string? _formError;

    private bool HasChanges =>
        _edit is not null &&
        _original is not null &&
        (
            _edit.Name != _original.Name ||
            _edit.IconName != _original.IconName ||
            _edit.IsEnabled != _original.IsEnabled ||
            _edit.DisplayOrder != _original.DisplayOrder ||
            _edit.ParentId != _original.ParentId
        );

    private void OpenEdit(CategoryDto category)
    {
        _edit = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            IconName = category.IconName,
            IsEnabled = category.IsEnabled,
            DisplayOrder = category.DisplayOrder,
            ParentId = category.ParentId
        };

        _original = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            IconName = category.IconName,
            IsEnabled = category.IsEnabled,
            DisplayOrder = category.DisplayOrder,
            ParentId = category.ParentId
        };

        _formError = null;
        _dialogOpen = true;
    }

    private void CloseEdit()
    {
        if (_saving)
            return;

        _dialogOpen = false;
        _edit = null;
        _original = null;
        _formError = null;
    }

    private IEnumerable<CategoryDto> AvailableParents()
    {
        if (_edit is null)
            return Enumerable.Empty<CategoryDto>();

        var excludedIds = GetDescendantIds(_edit.Id);

        return _categories
            .Where(c => c.Id != _edit.Id &&
                        !excludedIds.Contains(c.Id))
            .OrderBy(c => c.Name);
    }

    private HashSet<int> GetDescendantIds(int categoryId)
    {
        var descendants = new HashSet<int>();
        var pending = new Stack<int>();
        pending.Push(categoryId);

        while (pending.Count > 0)
        {
            var currentId = pending.Pop();

            foreach (var child in _categories.Where(
                         c => c.ParentId == currentId))
            {
                if (descendants.Add(child.Id))
                    pending.Push(child.Id);
            }
        }

        return descendants;
    }

    private async Task SaveEditAsync()
    {
        if (_edit is null || _saving || !HasChanges)
            return;

        _formError = null;

        if (_edit.ParentId == _edit.Id ||
            (_edit.ParentId.HasValue &&
             GetDescendantIds(_edit.Id)
                 .Contains(_edit.ParentId.Value)))
        {
            _formError =
                "A category cannot be its own parent or a descendant of itself.";
            return;
        }

        _saving = true;

        try
        {
            await CategoryApi.UpdateAsync(_edit);

            var index = _categories.FindIndex(
                c => c.Id == _edit.Id);

            if (index >= 0)
                _categories[index] = _edit;

            var sorted = _categories
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToList();

            _categories.Clear();
            _categories.AddRange(sorted);

            _dialogOpen = false;
            _edit = null;
            _original = null;
        }
        catch (HttpRequestException)
        {
            _formError =
                "Unable to save the category. Please check the API connection.";
        }
        catch (Exception)
        {
            _formError =
                "An unexpected error occurred while saving the category.";
        }
        finally
        {
            _saving = false;
        }
    }
}

```

## CSS

```

.edit-modal-backdrop {
    position: fixed;
    inset: 0;
    z-index: 1000;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 1rem;
    background: rgb(0 0 0 / 45%);
}

.edit-modal {
    width: 100%;
    max-width: 520px;
    max-height: 90vh;
    overflow-y: auto;
    background: var(--neutral-layer-1, #fff);
    color: var(--neutral-foreground-rest, #242424);
    border-radius: 8px;
    box-shadow: 0 12px 40px rgb(0 0 0 / 25%);
}

.edit-modal-header,
.edit-modal-footer {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    padding: 1rem 1.25rem;
}

.edit-modal-header {
    border-bottom: 1px solid #ddd;
}

.edit-modal-header h2 {
    margin: 0;
    font-size: 1.25rem;
}

.edit-modal-body {
    display: grid;
    gap: 1rem;
    padding: 1.25rem;
}

.form-field {
    display: grid;
    gap: 0.4rem;
}

.form-field label,
.form-checkbox label {
    font-weight: 600;
}

.form-control {
    box-sizing: border-box;
    width: 100%;
    min-height: 38px;
    padding: 0.5rem 0.65rem;
    border: 1px solid #888;
    border-radius: 4px;
    background: white;
    color: #242424;
    font: inherit;
}

/* Display order input: fixed width of 60px */
.form-control.category-order-input {
    width: 60px;
    min-width: 60px;
    max-width: 60px;
}

.form-checkbox {
    display: flex;
    align-items: center;
    gap: 0.6rem;
}

.form-error {
    padding: 0.75rem;
    color: #a4262c;
    background: #fde7e9;
    border-radius: 4px;
}

.edit-modal-footer {
    justify-content: flex-end;
    border-top: 1px solid #ddd;
}

.button-primary,
.button-secondary,
.close-button {
    padding: 0.55rem 1rem;
    border-radius: 4px;
    cursor: pointer;
    font: inherit;
}

.button-primary {
    color: white;
    background: #0078d4;
    border: 1px solid #0078d4;
}

.button-secondary {
    color: #242424;
    background: white;
    border: 1px solid #888;
}

.close-button {
    padding: 0.2rem 0.6rem;
    background: transparent;
    border: none;
    font-size: 1.5rem;
}

button:disabled,
.form-control:disabled {
    cursor: not-allowed;
    opacity: 0.6;
}

```

### Important integration notes

- Keep your existing `_categories`, `CategoryDto`, and `CategoryApi` definitions.
- Keep your existing grid Edit button calling `OpenEdit(category)`.
- The CSS class `category-order-input` ensures the 60px width overrides `.form-control` without changing other fields.
- `HasChanges` checks all five editable properties, including changes that are subsequently reverted.

I have preserved the types and API calls from your supplied code. The code has not been compiled against your application, so a completely error-free build cannot be guaranteed without checking the actual project and DTO definitions.

You can use Blazor's built-in `EditContext` validation state to add a red border around fields that have validation errors, while keeping the existing 60px Display order input and Save button change detection.

## 1. Update the form fields

Add the `class` attribute conditionally using `EditContext` and `FieldIdentifier`.

For example, replace the Name field with:

```

<div class="form-field">
    <label for="category-name">Name</label>
    <InputText id="category-name"
               class="@GetFieldCssClass(() => _edit!.Name)"
               @bind-Value="_edit.Name"
               disabled="@_saving" />
    <ValidationMessage For="@(() => _edit.Name)" />
</div>

```

Apply the same pattern to the other fields:

```

<div class="form-field">
    <label for="category-icon">Fluent UI icon name</label>
    <InputText id="category-icon"
               class="@GetFieldCssClass(() => _edit!.IconName)"
               @bind-Value="_edit.IconName"
               disabled="@_saving" />
    <ValidationMessage For="@(() => _edit.IconName)" />
</div>

<div class="form-field">
    <label for="category-parent">Parent category</label>
    <InputSelect id="category-parent"
                 class="@GetFieldCssClass(() => _edit!.ParentId)"
                 @bind-Value="_edit.ParentId"
                 disabled="@_saving">
        <option value="">No parent (root)</option>
        @foreach (var parent in AvailableParents())
        {
            <option value="@parent.Id">@parent.Name</option>
        }
    </InputSelect>
</div>

<div class="form-field">
    <label for="category-order">Display order</label>
    <InputNumber id="category-order"
                 class="@GetFieldCssClass(() => _edit!.DisplayOrder) category-order-input"
                 @bind-Value="_edit.DisplayOrder"
                 disabled="@_saving" />
    <ValidationMessage For="@(() => _edit.DisplayOrder)" />
</div>

```

## 2. Add the validation helper

Add this method inside your existing `@code` block:

```

private string GetFieldCssClass<TValue>(
    System.Linq.Expressions.Expression<Func<TValue>> accessor)
{
    if (_edit is null)
        return "form-control";

    var field = FieldIdentifier.Create(accessor);

    var hasErrors = _editContext is not null &&
                    _editContext.GetValidationMessages(field).Any();

    return hasErrors
        ? "form-control field-error"
        : "form-control";
}

```

Add this field as well:

```
private EditContext? _editContext;
```

Then, in `OpenEdit()`, after creating `_edit` and `_original`, initialise the context:

```
_editContext = new EditContext(_edit);
```

Update your form to use that context:

```
<EditForm EditContext="_editContext"
          OnValidSubmit="SaveEditAsync">
    <DataAnnotationsValidator />
    ...
</EditForm>
```

When closing or successfully saving, clear the context:

```
_editContext = null;
```

## 3. Add the red-border CSS

Add this to your existing stylesheet:

```

/* Invalid fields */
.form-control.field-error {
    border: 2px solid #d13438;
    outline: none;
}

/* Keep the red border visible when focused */
.form-control.field-error:focus {
    border-color: #d13438;
    box-shadow: 0 0 0 1px #d13438;
}

/* Validation message beneath the field */
.validation-message {
    color: #d13438;
    font-size: 0.875rem;
}

/* Display order remains 60px wide */
.form-control.category-order-input {
    width: 60px;
    min-width: 60px;
    max-width: 60px;
    box-sizing: border-box;
}

```

Important: The helper checks the validation messages for each field. It will show a red border for errors reported by Blazor's validation system, such as `[Required]` or `[Range]` attributes. Your custom parent-category error is currently stored in `_formError`, not in the `EditContext`, so it will display the form-level error message but won't automatically mark a particular field red.

#########################################################################
The likely cause is that the Save button is checking `HasChanges`, but the snapshot or form state isn't being updated consistently. In Blazor, the safest fix is to compare the edited model with the original snapshot and make sure the component re-renders when an input changes.

You can keep your existing validation and red borders. The key is to make change detection reliable.

## 1. Add a change-tracking flag

Add this field to your `@code` block:

```
private bool _hasChanges;
```

Keep your existing `HasChanges` property:

```
private bool HasChanges =>
    _edit is not null &&
    _original is not null &&
    (
        _edit.Name != _original.Name ||
        _edit.IconName != _original.IconName ||
        _edit.IsEnabled != _original.IsEnabled ||
        _edit.DisplayOrder != _original.DisplayOrder ||
        _edit.ParentId != _original.ParentId
    );
```

## 2. Track changes through the EditContext

Add this method:

```
private void HandleFieldChanged(
    object? sender,
    FieldChangedEventArgs e)
{
    _hasChanges = HasChanges;
    _ = InvokeAsync(StateHasChanged);
}
```

In `OpenEdit()`, after creating `_edit` and `_original`, initialise the context and subscribe to changes:

```
_editContext = new EditContext(_edit);
_editContext.OnFieldChanged += HandleFieldChanged;
_hasChanges = false;
```

This explicitly recalculates the change state whenever a form field changes.

## 3. Update the Save button

Use `_hasChanges` instead of `HasChanges`:

```
<button type="submit"
        class="button-primary"
        disabled="@(_saving || !_hasChanges)">
    @(_saving ? "Saving..." : "Save changes")
</button>
```

Also update the start of `SaveEditAsync()`:

```
if (_edit is null || _saving || !_hasChanges)
    return;
```

## 4. Reset the state when closing

In `CloseEdit()`, unsubscribe before clearing the context:

```
private void CloseEdit()
{
    if (_saving)
        return;

    if (_editContext is not null)
        _editContext.OnFieldChanged -= HandleFieldChanged;

    _dialogOpen = false;
    _edit = null;
    _original = null;
    _editContext = null;
    _hasChanges = false;
    _formError = null;
}
```

After a successful save, also unsubscribe and reset:

```
if (_editContext is not null)
    _editContext.OnFieldChanged -= HandleFieldChanged;

_dialogOpen = false;
_edit = null;
_original = null;
_editContext = null;
_hasChanges = false;
```

Expected result: The Save button enables when a field changes, disables again when all values match the original snapshot, and stays disabled during saving.

Note that Blazor's standard `InputText` updates on the `change` event, normally when you leave the field. If you need the button to enable on every keystroke, the text inputs will also need to use `oninput` binding.