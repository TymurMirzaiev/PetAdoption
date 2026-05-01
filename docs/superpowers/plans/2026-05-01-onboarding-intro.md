# Onboarding Intro Implementation Plan [COMPLETED]

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a multi-step onboarding dialog for first-time users that explains swipe mechanics, favorites, and adoption responsibility before they start discovering pets.

**Architecture:** A single `OnboardingDialog.razor` component in `Components/Shared/` uses MudDialog with internal step navigation. The Discover page checks `localStorage` for a `petadoption_onboarding_seen` flag on load and shows the dialog if absent. A "Show tutorial" button on the Discover page allows replaying the onboarding at any time. No backend changes required.

**Tech Stack:** Blazor WASM, MudBlazor 8.x, localStorage

**Dependencies:** None -- this is a frontend-only feature.

---

## File Structure

### New files:
- `src/Web/PetAdoption.Web.BlazorApp/Components/Shared/OnboardingDialog.razor` — Multi-step onboarding walkthrough dialog

### Modified files:
- `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Discover.razor` — Add onboarding check on load + "Show tutorial" button
- `src/Web/PetAdoption.Web.BlazorApp/wwwroot/css/app.css` — Add onboarding animation keyframes

---

## Chunk 1: OnboardingDialog Component

### Task 1: Create the OnboardingDialog component

**Files:**
- Create: `src/Web/PetAdoption.Web.BlazorApp/Components/Shared/OnboardingDialog.razor`

- [ ] **Step 1: Create the OnboardingDialog.razor file**

Create `src/Web/PetAdoption.Web.BlazorApp/Components/Shared/OnboardingDialog.razor`:

```razor
<MudDialog>
    <DialogContent>
        <div class="onboarding-container">
            @switch (_step)
            {
                case 0:
                    <div class="onboarding-step @(_animating ? "onboarding-step-enter" : "")">
                        <div class="d-flex justify-center mb-4">
                            <MudIcon Icon="@Icons.Material.Filled.Pets" Size="Size.Large" Color="Color.Primary"
                                     Style="font-size: 4rem;" />
                        </div>
                        <MudText Typo="Typo.h4" Align="Align.Center" Class="mb-2" Color="Color.Primary">
                            Welcome to PetAdoption!
                        </MudText>
                        <MudText Typo="Typo.body1" Align="Align.Center" Color="Color.Secondary" Class="mb-4">
                            Every pet deserves a loving home. Let's find yours!
                        </MudText>
                        <MudText Typo="Typo.body2" Align="Align.Center">
                            We'll show you how everything works in just a few steps.
                        </MudText>
                    </div>
                    break;

                case 1:
                    <div class="onboarding-step @(_animating ? "onboarding-step-enter" : "")">
                        <MudText Typo="Typo.h5" Align="Align.Center" Class="mb-4" Color="Color.Primary">
                            How It Works
                        </MudText>

                        <MudPaper Class="pa-4 mb-3" Elevation="0"
                                  Style="background: rgba(76, 175, 80, 0.08); border: 1px solid rgba(76, 175, 80, 0.3); border-radius: 12px;">
                            <div class="d-flex align-center gap-3">
                                <MudIcon Icon="@Icons.Material.Filled.SwipeRight" Color="Color.Success"
                                         Style="font-size: 2.5rem;" />
                                <div>
                                    <MudText Typo="Typo.subtitle1" Style="font-weight: 600;">Swipe Right</MudText>
                                    <MudText Typo="Typo.body2" Color="Color.Secondary">
                                        Love it! Add this pet to your favorites.
                                    </MudText>
                                </div>
                            </div>
                        </MudPaper>

                        <MudPaper Class="pa-4 mb-3" Elevation="0"
                                  Style="background: rgba(244, 67, 54, 0.08); border: 1px solid rgba(244, 67, 54, 0.3); border-radius: 12px;">
                            <div class="d-flex align-center gap-3">
                                <MudIcon Icon="@Icons.Material.Filled.SwipeLeft" Color="Color.Error"
                                         Style="font-size: 2.5rem;" />
                                <div>
                                    <MudText Typo="Typo.subtitle1" Style="font-weight: 600;">Swipe Left</MudText>
                                    <MudText Typo="Typo.body2" Color="Color.Secondary">
                                        Not for me right now. Skip to the next pet.
                                    </MudText>
                                </div>
                            </div>
                        </MudPaper>

                        <MudPaper Class="pa-4" Elevation="0"
                                  Style="background: rgba(33, 150, 243, 0.08); border: 1px solid rgba(33, 150, 243, 0.3); border-radius: 12px;">
                            <div class="d-flex align-center gap-3">
                                <MudIcon Icon="@Icons.Material.Filled.TouchApp" Color="Color.Info"
                                         Style="font-size: 2.5rem;" />
                                <div>
                                    <MudText Typo="Typo.subtitle1" Style="font-weight: 600;">Tap the Card</MudText>
                                    <MudText Typo="Typo.body2" Color="Color.Secondary">
                                        Curious? Tap to see more details about a pet.
                                    </MudText>
                                </div>
                            </div>
                        </MudPaper>
                    </div>
                    break;

                case 2:
                    <div class="onboarding-step @(_animating ? "onboarding-step-enter" : "")">
                        <div class="d-flex justify-center mb-4">
                            <MudIcon Icon="@Icons.Material.Filled.FavoriteBorder" Color="Color.Error"
                                     Style="font-size: 3.5rem;" />
                        </div>
                        <MudText Typo="Typo.h5" Align="Align.Center" Class="mb-4" Color="Color.Primary">
                            Your Favorites
                        </MudText>
                        <MudText Typo="Typo.body1" Align="Align.Center" Class="mb-3">
                            Every pet you swipe right on is saved to your
                            <MudIcon Icon="@Icons.Material.Filled.Favorite" Size="Size.Small" Color="Color.Error" />
                            <strong>Favorites</strong> list.
                        </MudText>
                        <MudText Typo="Typo.body1" Align="Align.Center" Class="mb-3">
                            Visit your favorites anytime from the top menu. When you've found the one,
                            you can <strong>reserve</strong> a pet and begin the adoption process.
                        </MudText>
                        <MudText Typo="Typo.body2" Align="Align.Center" Color="Color.Secondary">
                            Take your time -- there's no rush. The right match is worth the wait.
                        </MudText>
                    </div>
                    break;

                case 3:
                    <div class="onboarding-step @(_animating ? "onboarding-step-enter" : "")">
                        <div class="d-flex justify-center mb-4">
                            <MudIcon Icon="@Icons.Material.Filled.VolunteerActivism" Color="Color.Warning"
                                     Style="font-size: 3.5rem;" />
                        </div>
                        <MudText Typo="Typo.h5" Align="Align.Center" Class="mb-3" Color="Color.Primary">
                            A Promise, Not Just a Match
                        </MudText>
                        <MudText Typo="Typo.body1" Align="Align.Center" Class="mb-3">
                            Behind every card is a real animal with a beating heart, a wagging tail,
                            or a gentle purr -- waiting for someone to call their own.
                        </MudText>
                        <MudText Typo="Typo.body1" Align="Align.Center" Class="mb-3">
                            Adopting a pet is one of the most beautiful things you can do.
                            But it's also a promise -- a promise of morning walks and midnight comfort,
                            of vet visits and belly rubs, of patience on hard days and joy on all the rest.
                        </MudText>
                        <MudText Typo="Typo.body1" Align="Align.Center" Class="mb-4" Style="font-style: italic;">
                            They will love you unconditionally. They will depend on you for everything.
                            And in return, they will fill your life with a kind of warmth that nothing else can.
                        </MudText>

                        <MudPaper Class="pa-4" Elevation="0"
                                  Style="background: rgba(255, 167, 38, 0.08); border: 1px solid rgba(255, 167, 38, 0.3); border-radius: 12px;">
                            <div class="d-flex align-center gap-2">
                                <MudCheckBox @bind-Value="_acceptedResponsibility" Color="Color.Warning" />
                                <MudText Typo="Typo.body2">
                                    I understand that adoption is a lifelong commitment, and I'm here with good intentions.
                                </MudText>
                            </div>
                        </MudPaper>
                    </div>
                    break;
            }

            @* Step indicator dots *@
            <div class="d-flex justify-center gap-2 mt-4">
                @for (var i = 0; i < TotalSteps; i++)
                {
                    var stepIndex = i;
                    <div class="onboarding-dot @(stepIndex == _step ? "onboarding-dot--active" : "")" />
                }
            </div>
        </div>
    </DialogContent>
    <DialogActions>
        <div class="d-flex justify-space-between flex-grow-1 px-2">
            @if (_step > 0)
            {
                <MudButton OnClick="PreviousStep" Variant="Variant.Text">
                    <MudIcon Icon="@Icons.Material.Filled.ArrowBack" Size="Size.Small" Class="mr-1" /> Back
                </MudButton>
            }
            else
            {
                <MudSpacer />
            }

            @if (_step < TotalSteps - 1)
            {
                <MudButton OnClick="NextStep" Variant="Variant.Filled" Color="Color.Primary">
                    Next <MudIcon Icon="@Icons.Material.Filled.ArrowForward" Size="Size.Small" Class="ml-1" />
                </MudButton>
            }
            else
            {
                <MudButton OnClick="Complete" Variant="Variant.Filled" Color="Color.Success"
                           Disabled="@(!_acceptedResponsibility)">
                    <MudIcon Icon="@Icons.Material.Filled.Pets" Size="Size.Small" Class="mr-1" />
                    Start Exploring!
                </MudButton>
            }
        </div>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    private int _step;
    private bool _animating;
    private bool _acceptedResponsibility;
    private const int TotalSteps = 4;

    private async Task NextStep()
    {
        if (_step >= TotalSteps - 1) return;
        _animating = true;
        _step++;
        StateHasChanged();
        await Task.Delay(50);
        _animating = false;
        StateHasChanged();
    }

    private async Task PreviousStep()
    {
        if (_step <= 0) return;
        _animating = true;
        _step--;
        StateHasChanged();
        await Task.Delay(50);
        _animating = false;
        StateHasChanged();
    }

    private void Complete()
    {
        MudDialog.Close(DialogResult.Ok(true));
    }
}
```

- [ ] **Step 2: Verify the component builds**

Run: `dotnet build src/Web/PetAdoption.Web.BlazorApp`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Components/Shared/OnboardingDialog.razor
git commit -m "add onboarding dialog component with 4-step walkthrough"
```

---

## Chunk 2: CSS Animations for Onboarding

### Task 2: Add onboarding CSS styles

**Files:**
- Modify: `src/Web/PetAdoption.Web.BlazorApp/wwwroot/css/app.css`

- [ ] **Step 1: Add onboarding styles to app.css**

Append the following to the end of `src/Web/PetAdoption.Web.BlazorApp/wwwroot/css/app.css`:

```css
/* ──────────────────────────────────────────────
   Onboarding Dialog
   ────────────────────────────────────────────── */

.onboarding-container {
    min-height: 340px;
    display: flex;
    flex-direction: column;
    justify-content: center;
}

.onboarding-step {
    animation: onboarding-fade-in 0.35s ease-out;
}

.onboarding-step-enter {
    animation: onboarding-fade-in 0.35s ease-out;
}

@keyframes onboarding-fade-in {
    0%   { opacity: 0; transform: translateX(20px); }
    100% { opacity: 1; transform: translateX(0); }
}

/* Step indicator dots */

.onboarding-dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    background: rgba(255, 255, 255, 0.2);
    transition: all 0.3s ease;
}

.onboarding-dot--active {
    background: var(--mud-palette-primary);
    transform: scale(1.3);
    box-shadow: 0 0 8px var(--mud-palette-primary);
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Web/PetAdoption.Web.BlazorApp`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/wwwroot/css/app.css
git commit -m "add onboarding dialog CSS animations and step indicator styles"
```

---

## Chunk 3: Integrate Onboarding into Discover Page

### Task 3: Add localStorage check and dialog trigger to Discover page

**Files:**
- Modify: `src/Web/PetAdoption.Web.BlazorApp/Pages/User/Discover.razor`

- [ ] **Step 1: Add IDialogService and IJSRuntime injection**

Add these two `@inject` lines after the existing `@inject ISnackbar Snackbar` line in `Discover.razor`:

```razor
@inject IDialogService DialogService
@inject IJSRuntime JS
```

- [ ] **Step 2: Add "Show tutorial" button to the page**

Add a "Show tutorial" link below the `MudSelect` filter (after the closing `</MudSelect>` tag, before the `@if (_loading)` block):

```razor
    <div class="d-flex justify-end mb-2">
        <MudButton Variant="Variant.Text" Size="Size.Small" Color="Color.Secondary"
                   StartIcon="@Icons.Material.Filled.HelpOutline" OnClick="ShowOnboarding">
            Show tutorial
        </MudButton>
    </div>
```

- [ ] **Step 3: Add onboarding logic to the @code block**

Add the following field after `private bool _hasMore = true;`:

```csharp
    private const string OnboardingSeenKey = "petadoption_onboarding_seen";
```

Add the following methods to the `@code` block:

```csharp
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await CheckOnboarding();
        }
    }

    private async Task CheckOnboarding()
    {
        try
        {
            var seen = await JS.InvokeAsync<string?>("localStorage.getItem", OnboardingSeenKey);
            if (string.IsNullOrEmpty(seen))
            {
                await ShowOnboarding();
            }
        }
        catch
        {
            // localStorage may not be available; skip silently
        }
    }

    private async Task ShowOnboarding()
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            NoHeader = true,
            CloseOnEscapeKey = false,
            BackdropClick = false
        };
        var dialog = await DialogService.ShowAsync<OnboardingDialog>("Welcome", options);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            try
            {
                await JS.InvokeVoidAsync("localStorage.setItem", OnboardingSeenKey, "true");
            }
            catch { }
        }
    }
```

- [ ] **Step 4: Verify build**

Run: `dotnet build src/Web/PetAdoption.Web.BlazorApp`
Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add src/Web/PetAdoption.Web.BlazorApp/Pages/User/Discover.razor
git commit -m "integrate onboarding dialog into discover page with localStorage detection"
```

---

## Chunk 4: Manual Testing Checklist

### Task 4: Verify the feature end-to-end

- [ ] **Step 1: Start the application**

Run: `dotnet run --project src/Aspire/PetAdoption.AppHost`

- [ ] **Step 2: Test first-time user flow**

1. Open the Blazor app in a browser
2. Register a new account or log in
3. Verify the onboarding dialog appears automatically on the Discover page
4. Step through all 4 steps:
   - Step 1: "Welcome to PetAdoption!" with pet icon
   - Step 2: "How It Works" with swipe right/left/tap explanations
   - Step 3: "Your Favorites" explaining the favorites workflow
   - Step 4: "A Promise, Not Just a Match" with responsibility checkbox
5. Verify the "Start Exploring!" button is disabled until the checkbox is checked
6. Check the checkbox and click "Start Exploring!"
7. Verify the dialog closes and the Discover page is fully functional

- [ ] **Step 3: Test localStorage persistence**

1. Refresh the Discover page
2. Verify the onboarding dialog does NOT appear again
3. Open browser DevTools > Application > Local Storage
4. Verify `petadoption_onboarding_seen` is set to `"true"`

- [ ] **Step 4: Test "Show tutorial" button**

1. Click the "Show tutorial" button on the Discover page
2. Verify the onboarding dialog opens again
3. Complete it again
4. Verify normal functionality continues

- [ ] **Step 5: Test fresh start**

1. Open browser DevTools > Application > Local Storage
2. Remove the `petadoption_onboarding_seen` key
3. Refresh the Discover page
4. Verify the onboarding dialog appears again

- [ ] **Step 6: Test dialog cannot be dismissed early**

1. Clear `petadoption_onboarding_seen` from localStorage
2. Refresh to trigger the onboarding
3. Verify clicking outside the dialog does NOT close it (`BackdropClick = false`)
4. Verify pressing Escape does NOT close it (`CloseOnEscapeKey = false`)
5. The user must complete all steps and accept responsibility

---

## Summary of All Changes

| File | Action | Description |
|------|--------|-------------|
| `Components/Shared/OnboardingDialog.razor` | **Create** | 4-step MudDialog walkthrough with welcome, swipe guide, favorites, and adoption disclaimer |
| `Pages/User/Discover.razor` | **Modify** | Add `IDialogService` + `IJSRuntime` injection, `OnAfterRenderAsync` localStorage check, `ShowOnboarding()` method, "Show tutorial" button |
| `wwwroot/css/app.css` | **Modify** | Add onboarding fade-in animation and step indicator dot styles |

**Total new files:** 1
**Total modified files:** 2
**Backend changes:** None
**Database changes:** None
