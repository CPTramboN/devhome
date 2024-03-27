// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Customization.Models.Environments;
using DevHome.Customization.ViewModels.Environments;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml.Media;

namespace DevHome.Customization.ViewModels;

public partial class DevDriveInsightsViewModel : SetupPageViewModelBase
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private readonly ObservableCollection<DevDrivesListViewModel> _devDriveViewModelList = new();

    private readonly ObservableCollection<DevDriveOptimizersListViewModel> _devDriveOptimizerViewModelList = new();

    private readonly ObservableCollection<DevDriveOptimizedListViewModel> _devDriveOptimizedViewModelList = new();

    [ObservableProperty]
    private bool _shouldShowCollectionView;

    [ObservableProperty]
    private bool _shouldShowOptimizerCollectionView;

    [ObservableProperty]
    private bool _shouldShowOptimizedCollectionView;

    [ObservableProperty]
    private bool _shouldShowShimmerBelowList;

    [ObservableProperty]
    private bool _shouldShowShimmerBelowOptimizerList;

    [ObservableProperty]
    private bool _shouldShowShimmerBelowOptimizedList;

    [ObservableProperty]
    private bool _devDriveLoadingCompleted;

    [ObservableProperty]
    private bool _devDriveOptimizerLoadingCompleted;

    [ObservableProperty]
    private bool _devDriveOptimizedLoadingCompleted;

    public AdvancedCollectionView DevDrivesCollectionView { get; private set; }

    public AdvancedCollectionView DevDriveOptimizersCollectionView { get; private set; }

    public AdvancedCollectionView DevDriveOptimizedCollectionView { get; private set; }

    public IDevDriveManager DevDriveManagerObj { get; private set; }

    private IEnumerable<IDevDrive> ExistingDevDrives { get; set; } = Enumerable.Empty<IDevDrive>();

    public DevDriveInsightsViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowViewModel setupflowModel,
        SetupFlowOrchestrator orchestrator,
        IDevDriveManager devDriveManager,
        ToastNotificationService toastNotificationService)
        : base(stringResource, orchestrator)
    {
        // Add AdvancedCollectionView to make filtering and sorting the list of DevDrivesListViewModels easier.
        DevDrivesCollectionView = new AdvancedCollectionView(_devDriveViewModelList, true);

        // Add AdvancedCollectionView to make filtering and sorting the list of DevDrivesOptimizersListViewModels easier.
        DevDriveOptimizersCollectionView = new AdvancedCollectionView(_devDriveOptimizerViewModelList, true);

        // Add AdvancedCollectionView to make filtering and sorting the list of DevDrivesOptimizedListViewModels easier.
        DevDriveOptimizedCollectionView = new AdvancedCollectionView(_devDriveOptimizedViewModelList, true);

        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        DevDriveManagerObj = devDriveManager;

        _ = this.OnFirstNavigateToAsync();
    }

    /// <summary>
    /// Compares two strings and returns true if they are equal. This method is case insensitive.
    /// </summary>
    /// <param name="text">First string to use in comparison</param>
    /// <param name="text2">Second string to use in comparison</param>
    private bool CompareStrings(string text, string text2)
    {
        return string.Equals(text, text2, StringComparison.OrdinalIgnoreCase);
    }

    public bool CanEnableSyncButton()
    {
        return DevDriveLoadingCompleted;
    }

    [RelayCommand(CanExecute = nameof(CanEnableSyncButton))]
    public void SyncDevDrives()
    {
        GetDevDrives();
    }

    /// <summary>
    /// Make sure we only get the list of DevDrives from the DevDriveManager once when the page is first navigated to.
    /// All other times will be through the use of the sync button.
    /// </summary>
    protected async override Task OnFirstNavigateToAsync()
    {
        // Do nothing, but we need to override this as the base expects a task to be returned.
        await Task.CompletedTask;

        GetDevDrives();
        GetDevDriveOptimizers();
        GetDevDriveOptimizeds();
    }

    public void UpdateNextButtonState()
    {
        SyncDevDrivesCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Starts the process of getting the list of DevDriveOptimizers. the sync and next
    /// buttons should be disabled when work is being done.
    /// </summary>
    private void GetDevDriveOptimizers()
    {
        // Remove any existing DevDriveOptimizersListViewModels from the list if they exist.
        RemoveDevDriveOptimizersListViewModels();

        // Disable the sync and next buttons while we're getting the dev drives.
        DevDriveOptimizerLoadingCompleted = false;

        // load the dev drives so we can show them in the UI.
        LoadAllDevDriveOptimizersInTheUI();

        // Enable the sync and next buttons when we're done getting the dev drives.
        UpdateNextButtonState();

        DevDriveOptimizersCollectionView.Refresh();
    }

    /// <summary>
    /// Removes all DevDriveOptimizersListViewModels from the list view model list and removes the dev drive
    /// selected to apply the configuration to. This should refresh the UI to show no dev drives.
    /// </summary>
    private void RemoveDevDriveOptimizersListViewModels()
    {
        var totalLists = _devDriveOptimizerViewModelList.Count;
        for (var i = totalLists - 1; i >= 0; i--)
        {
            _devDriveOptimizerViewModelList.RemoveAt(i);
        }

        ShouldShowOptimizerCollectionView = false;
        DevDriveOptimizersCollectionView.Refresh();
    }

    /// <summary>
    /// Starts the process of getting the list of DevDriveOptimizedCards.
    /// </summary>
    private void GetDevDriveOptimizeds()
    {
        // Remove any existing DevDriveOptimizedListViewModels from the list if they exist.
        RemoveDevDriveOptimizedListViewModels();

        // Disable the sync and next buttons while we're getting the dev drives.
        DevDriveOptimizedLoadingCompleted = false;

        // load the dev drives so we can show them in the UI.
        LoadAllDevDriveOptimizedsInTheUI();

        // Enable the sync and next buttons when we're done getting the dev drives.
        UpdateNextButtonState();

        DevDriveOptimizedCollectionView.Refresh();
    }

    /// <summary>
    /// Removes all DevDriveOptimizedListViewModels from the list view model list and removes the dev drive
    /// selected to apply the configuration to. This should refresh the UI to show no dev drives.
    /// </summary>
    private void RemoveDevDriveOptimizedListViewModels()
    {
        var totalLists = _devDriveOptimizedViewModelList.Count;
        for (var i = totalLists - 1; i >= 0; i--)
        {
            _devDriveOptimizedViewModelList.RemoveAt(i);
        }

        ShouldShowOptimizedCollectionView = false;
        DevDriveOptimizedCollectionView.Refresh();
    }

    /// <summary>
    /// Starts the process of getting the list of DevDrives from all providers. the sync and next
    /// buttons should be disabled when work is being done.
    /// </summary>
    private void GetDevDrives()
    {
        // Remove any existing DevDrivesListViewModels from the list if they exist. E.g when sync button is
        // pressed.
        RemoveDevDrivesListViewModels();

        // Disable the sync and next buttons while we're getting the dev drives.
        DevDriveLoadingCompleted = false;
        UpdateNextButtonState();

        // load the dev drives so we can show them in the UI.
        LoadAllDevDrivesInTheUI();

        // Enable the sync and next buttons when we're done getting the dev drives.
        UpdateNextButtonState();
        DevDrivesCollectionView.Refresh();
    }

    /// <summary>
    /// Removes all DevDrivesListViewModels from the list view model list and removes the dev drive
    /// selected to apply the configuration to. This should refresh the UI to show no dev drives.
    /// </summary>
    private void RemoveDevDrivesListViewModels()
    {
        var totalLists = _devDriveViewModelList.Count;
        for (var i = totalLists - 1; i >= 0; i--)
        {
            _devDriveViewModelList.RemoveAt(i);
        }

        // Reset the filter text and the selected provider name.
        ShouldShowCollectionView = false;
        DevDrivesCollectionView.Refresh();
    }

    /// <summary>
    /// Adds a DevDrivesListViewModel from the DevDriveManager.
    /// </summary>
    private void AddListViewModelToList(DevDrivesListViewModel listViewModel)
    {
        // listViewModel doesn't exist so add it to the list.
        _devDriveViewModelList.Add(listViewModel);
        ShouldShowCollectionView = true;
    }

    /// <summary>
    /// Adds a DevDriveOptimizersListViewModel.
    /// </summary>
    private void AddOptimizerListViewModelToList(DevDriveOptimizersListViewModel listViewModel)
    {
        // listViewModel doesn't exist so add it to the list.
        _devDriveOptimizerViewModelList.Add(listViewModel);
        ShouldShowOptimizerCollectionView = true;
    }

    /// <summary>
    /// Adds a DevDriveOptimizersListViewModel.
    /// </summary>
    private void AddOptimizedListViewModelToList(DevDriveOptimizedListViewModel listViewModel)
    {
        // listViewModel doesn't exist so add it to the list.
        _devDriveOptimizedViewModelList.Add(listViewModel);
        ShouldShowOptimizedCollectionView = true;
    }

    /// <summary>
    /// Loads all the DevDrives from all providers and updates the UI with the results.
    /// </summary>
    public void LoadAllDevDrivesInTheUI()
    {
        try
        {
            ExistingDevDrives = DevDriveManagerObj.GetAllDevDrivesThatExistOnSystem();
            UpdateListViewModelList();
        }
        catch (Exception /*ex*/)
        {
            // Log.Logger?.ReportError(Log.Component.SetupTarget, $"Error loading DevDriveViewModels data", ex);
        }

        ShouldShowShimmerBelowList = false;
    }

    /// <summary>
    /// Loads all the DevDriveOptimizers and updates the UI with the results.
    /// </summary>
    public void LoadAllDevDriveOptimizersInTheUI()
    {
        try
        {
            if (!ExistingDevDrives.Any())
            {
                ExistingDevDrives = DevDriveManagerObj.GetAllDevDrivesThatExistOnSystem();
            }

            UpdateOptimizerListViewModelList();
        }
        catch (Exception /*ex*/)
        {
            // Log.Logger?.ReportError(Log.Component.SetupTarget, $"Error loading DevDriveViewModels data", ex);
        }

        ShouldShowShimmerBelowOptimizerList = false;
    }

    /// <summary>
    /// Loads all the DevDriveOptimizedCards and updates the UI with the results.
    /// </summary>
    public void LoadAllDevDriveOptimizedsInTheUI()
    {
        try
        {
            if (!ExistingDevDrives.Any())
            {
                ExistingDevDrives = DevDriveManagerObj.GetAllDevDrivesThatExistOnSystem();
            }

            UpdateOptimizedListViewModelList();
        }
        catch (Exception /*ex*/)
        {
            // Log.Logger?.ReportError(Log.Component.SetupTarget, $"Error loading DevDriveViewModels data", ex);
        }

        ShouldShowShimmerBelowOptimizedList = false;
    }

    private void RemoveSelectedItemIfNotInUI(DevDrivesListViewModel listViewModel)
    {
        UpdateNextButtonState();
    }

    public void UpdateListViewModelList()
    {
        var curListViewModel = new DevDrivesListViewModel();
        foreach (var existingDevDrive in ExistingDevDrives)
        {
            var card = new DevDriveCardViewModel(existingDevDrive, DevDriveManagerObj);
            curListViewModel.DevDriveCardCollection.Add(card);
        }

        AddListViewModelToList(curListViewModel);
        DevDriveLoadingCompleted = true;
        ShouldShowShimmerBelowList = true;
    }

    private string? GetExistingCacheLocation(string rootDirectory, string targetDirectoryName)
    {
        var fullDirectoryPath = rootDirectory + targetDirectoryName;
        if (Directory.Exists(fullDirectoryPath))
        {
            return fullDirectoryPath;
        }
        else
        {
            var subDirPrefix = rootDirectory + "\\Packages\\PythonSoftwareFoundation.Python";
            var subDirectories = Directory.GetDirectories(rootDirectory + "\\Packages", "*", SearchOption.TopDirectoryOnly);
            var matchingSubdirectory = subDirectories.FirstOrDefault(subdir => subdir.StartsWith(subDirPrefix, StringComparison.OrdinalIgnoreCase));
            var alternateFullDirectoryPath = matchingSubdirectory + "\\localcache\\local" + targetDirectoryName;
            if (Directory.Exists(alternateFullDirectoryPath))
            {
                return alternateFullDirectoryPath;
            }
        }

        return null;
    }

    private bool CacheInDevDrive(string existingPipCacheLocation)
    {
        foreach (var existingDrive in ExistingDevDrives)
        {
            if (existingPipCacheLocation.StartsWith(existingDrive.DriveLetter.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public void UpdateOptimizerListViewModelList()
    {
        var curOptimizerListViewModel = new DevDriveOptimizersListViewModel();
        var cacheSubDir = "\\pip\\cache";
        var environmentVariable = "PIP_CACHE_DIR";
        var localAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var existingPipCacheLocation = GetExistingCacheLocation(localAppDataDir.ToString(), cacheSubDir);
        if (existingPipCacheLocation != null && !CacheInDevDrive(existingPipCacheLocation))
        {
            var card = new DevDriveOptimizerCardViewModel(
                "Pip cache (Python)",
                existingPipCacheLocation,
                "D:\\packages" + cacheSubDir /*example location on dev drive to move cache to*/,
                environmentVariable /*environmentVariableToBeSet*/);
            curOptimizerListViewModel.DevDriveOptimizerCardCollection.Add(card);

            AddOptimizerListViewModelToList(curOptimizerListViewModel);
            DevDriveOptimizerLoadingCompleted = true;
            ShouldShowShimmerBelowOptimizerList = true;
        }
    }

    public void UpdateOptimizedListViewModelList()
    {
        var environmentVariable = "PIP_CACHE_DIR";

        // We retrieve the cache location from environment variable, because if the cache might have already moved.
        var movedPipCacheLocation = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrEmpty(movedPipCacheLocation) && CacheInDevDrive(movedPipCacheLocation))
        {
            // Cache already in dev drive, show the "Optimized" card
            var curOptimizedListViewModel = new DevDriveOptimizedListViewModel();
            var card = new DevDriveOptimizedCardViewModel("Pip cache (Python)", movedPipCacheLocation, environmentVariable);
            curOptimizedListViewModel.DevDriveOptimizedCardCollection.Add(card);

            AddOptimizedListViewModelToList(curOptimizedListViewModel);
            DevDriveOptimizedLoadingCompleted = true;
            ShouldShowShimmerBelowOptimizedList = true;
        }
    }
}