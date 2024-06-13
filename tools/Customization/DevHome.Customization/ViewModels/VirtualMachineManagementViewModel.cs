﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using DevHome.Common.Helpers;
using DevHome.Common.Models;
using DevHome.Common.Scripts;
using DevHome.Common.Services;
using DevHome.Customization.Views;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.Customization.ViewModels;

public partial class VirtualMachineManagementViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(VirtualMachineManagementViewModel));

    private readonly StringResource _stringResource;

    private readonly bool _isUserAdministrator = WindowsIdentityHelper.IsUserAdministrator();

    private readonly Window _window;

    private readonly ModifyFeaturesDialog _modifyFeaturesDialog;

    private OptionalFeatureNotificationHelper? _notificationsHelper;

    public IAsyncRelayCommand LoadFeaturesCommand { get; }

    public bool FeaturesLoaded => !LoadFeaturesCommand.IsRunning;

    public IAsyncRelayCommand ApplyChangesCommand { get; }

    public IAsyncRelayCommand DiscardChangesCommand { get; }

    public bool ChangesCanBeApplied => HasFeatureChanges && !ApplyChangesCommand.IsRunning;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ObservableCollection<OptionalFeatureState> Features { get; } = new();

    public bool HasFeatureChanges => _isUserAdministrator && FeaturesLoaded && Features.Any(f => f.HasChanged);

    public bool CanDismissNotifications => _isUserAdministrator;

    private bool _restartNeeded;

    public VirtualMachineManagementViewModel(Window window)
    {
        _stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        _window = window;

        Breadcrumbs =
        [
            new(_stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!),
            new(_stringResource.GetLocalized("VirtualMachineManagement_Header"), typeof(VirtualMachineManagementViewModel).FullName!)
        ];

        LoadFeaturesCommand = new AsyncRelayCommand(LoadFeaturesAsync);
        LoadFeaturesCommand.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(LoadFeaturesCommand.IsRunning))
            {
                await OnFeaturesChanged();
            }
        };

        ApplyChangesCommand = new AsyncRelayCommand(ApplyChangesAsync);
        ApplyChangesCommand.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(ApplyChangesCommand.IsRunning))
            {
                await OnFeaturesChanged();
            }
        };

        DiscardChangesCommand = new AsyncRelayCommand(DiscardChangesAsync);
        DiscardChangesCommand.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(DiscardChangesCommand.IsRunning))
            {
                await OnFeaturesChanged();
            }
        };

        _modifyFeaturesDialog = new ModifyFeaturesDialog(ApplyChangesCommand, DiscardChangesCommand)
        {
            XamlRoot = _window.Content.XamlRoot,
        };

        _ = LoadFeaturesCommand.ExecuteAsync(null);
    }

    internal void Initialize(StackedNotificationsBehavior notificationQueue)
    {
        _notificationsHelper = new(notificationQueue, _log);

        if (!_isUserAdministrator)
        {
            _window.DispatcherQueue.EnqueueAsync(_notificationsHelper.ShowNonAdminUserNotification);
        }

        if (_restartNeeded)
        {
            _window.DispatcherQueue.EnqueueAsync(_notificationsHelper.ShowRestartNotification);
        }
    }

    internal void Uninitialize()
    {
        _notificationsHelper = null;
    }

    private async void FeatureState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OptionalFeatureState.IsEnabled))
        {
            await OnFeaturesChanged();
        }
    }

    private async Task LoadFeaturesAsync()
    {
        await Task.Run(async () =>
        {
            await _window.DispatcherQueue.EnqueueAsync(() =>
            {
                Features.Clear();
            });

            foreach (var featureName in WindowsOptionalFeatureNames.VirtualMachineFeatures)
            {
                var feature = ManagementInfrastructureHelper.GetWindowsFeatureDetails(featureName);
                if (feature != null && feature.IsAvailable)
                {
                    var featureState = new OptionalFeatureState(feature, _isUserAdministrator, ApplyChangesCommand);
                    featureState.PropertyChanged += FeatureState_PropertyChanged;

                    await _window.DispatcherQueue.EnqueueAsync(() =>
                    {
                        Features.Add(featureState);
                    });
                }
            }
        });
    }

    private async Task ApplyChangesAsync()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        _modifyFeaturesDialog.ViewModel.SetCommittingChanges(cancellationTokenSource);
        var showDialogTask = _modifyFeaturesDialog.ShowAsync();

        await _window.DispatcherQueue.EnqueueAsync(async () =>
        {
            var exitCode = await ModifyWindowsOptionalFeatures.ModifyFeaturesAsync(Features, _notificationsHelper, _log, cancellationTokenSource.Token);

            // Handle the exit code as needed, for example:
            switch (exitCode)
            {
                case ModifyWindowsOptionalFeatures.ExitCode.Success:
                    // Mark that changes have been applied and a restart is needed. This allows for a persistent notification
                    // to be displayed when the user navigates away from the page and returns.
                    _modifyFeaturesDialog.ViewModel.SetComplete();
                    _restartNeeded = true;
                    break;
                case ModifyWindowsOptionalFeatures.ExitCode.NoChange:
                case ModifyWindowsOptionalFeatures.ExitCode.Failure:
                    // Do nothing for these error conditions, the InfoBar will be updated by ModifyFeaturesAsync
                    // in these cases.
                    _modifyFeaturesDialog.Hide();
                    break;
            }

            await LoadFeaturesCommand.ExecuteAsync(null);
        });

        await showDialogTask;
    }

    private async Task DiscardChangesAsync()
    {
        await _window.DispatcherQueue.EnqueueAsync(() =>
        {
            foreach (var feature in Features)
            {
                feature.IsEnabled = feature.Feature.IsEnabled;
            }
        });
    }

    private async Task OnFeaturesChanged()
    {
        await _window.DispatcherQueue.EnqueueAsync(() =>
        {
            OnPropertyChanged(nameof(FeaturesLoaded));
            OnPropertyChanged(nameof(HasFeatureChanges));
            OnPropertyChanged(nameof(ChangesCanBeApplied));
        });
    }
}
