﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.TestModels;

/// <summary>
/// Class to support compute systems providers that don't need a developer ID.
/// </summary>
internal class EmptyDevId : IDeveloperId
{
    public string LoginId { get; }

    public string Url { get; }

    public EmptyDevId()
    {
        LoginId = string.Empty;
        Url = string.Empty;
    }
}
