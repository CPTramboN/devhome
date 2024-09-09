// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DevHome.DevDiagnostics.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace DevHome.DevDiagnostics.Controls;

public sealed partial class InsightForMissingFileProcessTerminationControl : UserControl
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string ElevationButtonTextString { get; } = CommonHelper.GetLocalizedString("InsightsMissingFileEnableLoaderSnapsButtonLabel");

    public string ElevationButtonToolTipString { get; } = CommonHelper.GetLocalizedString("InsightsMissingFileEnableLoaderSnapsButtonToolTip");

    private string _text = string.Empty;

    public string Text
    {
        get => _text;

        set
        {
            _text = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }
    }

    private System.Windows.Input.ICommand? _command;

    public System.Windows.Input.ICommand? Command
    {
        get => _command;

        set
        {
            _command = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Command)));
        }
    }

    public InsightForMissingFileProcessTerminationControl()
    {
        this.InitializeComponent();
    }
}