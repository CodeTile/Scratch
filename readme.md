Here is just the Edit Category page for your Fluent UI Blazor v4.x application. It uses a single popup to edit the category's name, Fluent UI icon name, enabled status, display order, and parent category.

This assumes you already have `_categories`, `CategoryDto`, and `CategoryApiClient` implemented.

## `Categories.razor` — Edit popup

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
                                     class="form-control"
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
                            disabled="@_saving">
                        @(_saving ? "Saving..." : "Save changes")
                    </button>
                </footer>
            </EditForm>
        </section>
    </div>
}

@code {
    private CategoryDto? _edit;
    private bool _dialogOpen;
    private bool _saving;
    private string? _formError;

    private void OpenEdit(CategoryDto category)
    {
        // Copy the record so Cancel doesn't modify the grid data.
        _edit = new CategoryDto
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
        if (_edit is null || _saving)
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

            // Keep the grid sorted.
            var sorted = _categories
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToList();

            _categories.Clear();
            _categories.AddRange(sorted);

            _dialogOpen = false;
            _edit = null;
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

Add this to your page's stylesheet or your existing category CSS file.

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

## Connect it to your existing grid

Add an Edit button to each row, passing the category for that row:

```
<FluentButton Appearance="Appearance.Accent"
              OnClick="@(() => OpenEdit(context))">
    Edit
</FluentButton>
```

Replace `context` with your grid's actual row variable if it has a different name.

Important: This code assumes `_categories` is a `List<CategoryDto>` and `CategoryApi.UpdateAsync` accepts a `CategoryDto`. Keep your existing API client and grid; you only need to integrate the popup, its state, and the Edit button.