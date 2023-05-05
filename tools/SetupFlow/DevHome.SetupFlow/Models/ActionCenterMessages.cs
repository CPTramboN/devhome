﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Messages to show in the action center part of the loading screen when an item encountered an error
/// </summary>
public class ActionCenterMessages
{
    /// <summary>
    /// Gets or sets the message to show to the user
    /// </summary>
    public string PrimaryMessage
    {
        get; set;
    }
}
