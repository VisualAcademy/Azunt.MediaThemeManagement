﻿using Azunt.Components.Dialogs;
using Azunt.MediaThemeManagement;
using Azunt.Web.Components.Pages.MediaThemes.Components;
using Azunt.Web.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;

namespace Azunt.Web.Pages.MediaThemes;

public partial class Manage : ComponentBase
{
    public bool SimpleMode { get; set; } = false;
    private int timeZoneOffsetMinutes;

    #region Parameters
    [Parameter] public int ParentId { get; set; } = 0;
    [Parameter] public string ParentKey { get; set; } = "";
    [Parameter] public string UserId { get; set; } = "";
    [Parameter] public string UserName { get; set; } = "";
    #endregion

    #region Injectors
    [Inject] public NavigationManager Nav { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntimeInjector { get; set; } = null!;
    [Inject] public IMediaThemeRepository RepositoryReference { get; set; } = null!;
    [Inject] public IConfiguration Configuration { get; set; } = null!;
    [Inject] public MediaThemeAppDbContextFactory DbContextFactory { get; set; } = null!;
    [Inject] public UserManager<ApplicationUser> UserManagerRef { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthenticationStateProviderRef { get; set; } = null!;
    #endregion

    #region Properties
    public string EditorFormTitle { get; set; } = "CREATE";
    public ModalForm EditorFormReference { get; set; } = null!;
    public DeleteDialog DeleteDialogReference { get; set; } = null!;
    protected List<MediaTheme> models = new();
    protected MediaTheme model = new();
    public bool IsInlineDialogShow { get; set; } = false;
    private string searchQuery = "";
    private string sortOrder = "";
    protected DulPager.DulPagerBase pager = new()
    {
        PageNumber = 1,
        PageIndex = 0,
        PageSize = 10,
        PagerButtonCount = 5
    };
    #endregion

    #region Lifecycle
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JSRuntimeInjector.InvokeVoidAsync(
                "Azunt.UI.applyTruncation",
                ".text-truncate-name", 10, 150, true);
        }

        if (firstRender)
        {
            timeZoneOffsetMinutes = await JSRuntimeInjector.InvokeAsync<int>("Azunt.TimeZone.getLocalOffsetMinutes");
            StateHasChanged(); // UI에 반영되도록
        }
    }

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(UserName))
            await GetUserIdAndUserName();

        await DisplayData();
    }
    #endregion

    #region Data Load
    private async Task DisplayData()
    {
        var articleSet = ParentKey != ""
            ? await RepositoryReference.GetAllAsync<string>(pager.PageIndex, pager.PageSize, "", searchQuery, sortOrder, ParentKey)
            : await RepositoryReference.GetAllAsync<int>(pager.PageIndex, pager.PageSize, "", searchQuery, sortOrder, ParentId);

        pager.RecordCount = articleSet.TotalCount;
        models = articleSet.Items.ToList();
        StateHasChanged();
    }

    protected async void PageIndexChanged(int pageIndex)
    {
        pager.PageIndex = pageIndex;
        pager.PageNumber = pageIndex + 1;
        await DisplayData();
    }
    #endregion

    #region CRUD Events
    protected void ShowEditorForm()
    {
        EditorFormTitle = "CREATE";
        model = new MediaTheme();
        EditorFormReference.Show();
    }

    protected void EditBy(MediaTheme m)
    {
        EditorFormTitle = "EDIT";
        model = m;
        EditorFormReference.Show();
    }

    protected void DeleteBy(MediaTheme m)
    {
        model = m;
        DeleteDialogReference.Show();
    }

    protected async void CreateOrEdit()
    {
        EditorFormReference.Hide();
        await Task.Delay(50);
        model = new MediaTheme();
        await DisplayData();
    }

    protected async void DeleteClick()
    {
        var connectionString = Configuration.GetConnectionString("DefaultConnection");
        await RepositoryReference.DeleteAsync(model.Id);
        DeleteDialogReference.Hide();
        model = new MediaTheme();
        await DisplayData();
    }
    #endregion

    #region Toggle Active
    protected void ToggleBy(MediaTheme m)
    {
        model = m;
        IsInlineDialogShow = true;
    }

    protected void ToggleClose()
    {
        IsInlineDialogShow = false;
        model = new MediaTheme();
    }

    protected async void ToggleClick()
    {
        var connectionString = Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("DefaultConnection is not configured.");

        await using var context = DbContextFactory.CreateDbContext(connectionString);
        model.Active = !model.Active;
        context.MediaThemes.Update(model);
        await context.SaveChangesAsync();

        IsInlineDialogShow = false;
        model = new MediaTheme();
        await DisplayData();
    }
    #endregion

    #region Search & Sort
    protected async void Search(string query)
    {
        pager.PageIndex = 0;
        searchQuery = query;
        await DisplayData();
    }

    protected async void SortByName()
    {
        sortOrder = sortOrder switch
        {
            "" => "Name",
            "Name" => "NameDesc",
            _ => ""
        };

        await DisplayData();
    }
    #endregion

    #region User Info
    private async Task GetUserIdAndUserName()
    {
        var authState = await AuthenticationStateProviderRef.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            var currentUser = await UserManagerRef.GetUserAsync(user);
            UserId = currentUser?.Id ?? "";
            UserName = user.Identity?.Name ?? "Anonymous";
        }
        else
        {
            UserId = "";
            UserName = "Anonymous";
        }
    }
    #endregion

    private async Task MoveUp(long id)
    {
        await RepositoryReference.MoveUpAsync(id);
        await DisplayData();
    }

    private async Task MoveDown(long id)
    {
        await RepositoryReference.MoveDownAsync(id);
        await DisplayData();
    }
}