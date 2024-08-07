﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using Microsoft.UI.Xaml;

namespace DevHome.PI.Models;

// This class contains a collection of all of the analysis reports generated by the
// different tools that are registered as crash analyzers
public partial class WERAnalysisReport : ObservableObject
{
    private readonly ExternalToolsHelper _externalTools;
    private readonly Dictionary<Tool, WERAnalysis> _toolAnalyses = new();

    public WERReport Report { get; }

    // While we can have multiple failure buckets (1 from each tool), we have a "chosen" one here
    // that we can bind to for the UI
    [ObservableProperty]
    private string _failureBucket = string.Empty;

    public ReadOnlyDictionary<Tool, WERAnalysis> ToolAnalyses { get; private set; }

    public WERAnalysisReport(WERReport report)
    {
        Report = report;
        ToolAnalyses = new(_toolAnalyses);
        _externalTools = Application.Current.GetService<ExternalToolsHelper>();
        FailureBucket = report.FailureBucket;
    }

    public void SetFailureBucketTool(Tool? tool)
    {
        if (tool is null)
        {
            // Resetting to the internal failure bucket
            FailureBucket = Report.FailureBucket;
            return;
        }

        if (_toolAnalyses.TryGetValue(tool, out WERAnalysis? analysis))
        {
            FailureBucket = string.IsNullOrEmpty(analysis.FailureBucket) ? string.Empty : analysis.FailureBucket;
        }
        else
        {
            FailureBucket = string.Empty;
        }
    }

    public void RunToolAnalysis(Tool tool)
    {
        Debug.Assert(tool.Type.HasFlag(ToolType.DumpAnalyzer), "We should only be running dump analyzers on dumps - Not " + tool.Type);

        WERAnalysis analysis = new(tool, Report.CrashDumpPath);
        analysis.Run();
        if (analysis.Analysis is not null)
        {
            _toolAnalyses.Add(tool, analysis);
        }
    }

    public void RemoveToolAnalysis(Tool tool)
    {
        Debug.Assert(tool.Type.HasFlag(ToolType.DumpAnalyzer), "We should only be running dump analyzers on dumps  - Not " + tool.Type);

        if (_toolAnalyses.TryGetValue(tool, out WERAnalysis? analysis))
        {
            analysis.RemoveCachedResults();
            _toolAnalyses.Remove(tool);
        }
    }
}
