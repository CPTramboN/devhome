﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Serilog;

namespace FileExplorerGitIntegration.Models;

public class WslIntegrator
{
    private static readonly string[] _wslPathPrefixes = { "wsl$", "wsl.localhost" };
    private static readonly ILogger _log = Log.ForContext<WslIntegrator>();

    public static bool IsWSLRepo(string repositoryPath)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            _log.Debug("The repository path is empty");
            return false;
        }

        if (repositoryPath.Contains(Path.AltDirectorySeparatorChar))
        {
            _log.Debug("The repository path is not in the expected format");
            return false;
        }

        string[] pathParts = repositoryPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        // Check if the repository path contains any of the WSL path prefixes
        foreach (string prefix in _wslPathPrefixes)
        {
            if (pathParts[0].Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        _log.Debug(repositoryPath + " is not a WSL path");
        return false;
    }

    public static string GetWslDistributionName(string repositoryPath)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            _log.Debug("The repository path is empty");
            throw new ArgumentNullException(nameof(repositoryPath));
        }

        Debug.Assert(IsWSLRepo(repositoryPath), "the repository path must be a valid wsl path");

        // Parse the repository path to get the distribution name
        string[] pathParts = repositoryPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length > 1)
        {
            return pathParts[1];
        }

        _log.Debug("Failed to get the distribution name from the repository path");
        throw new ArgumentException("Failed to get the distribution name from the repository path");
    }

    public static string GetWorkingDirectory(string repositoryPath)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            throw new ArgumentNullException(nameof(repositoryPath));
        }

        Debug.Assert(IsWSLRepo(repositoryPath), "the repository path must be a valid wsl path");

        string[] pathParts = repositoryPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        // Ensure the first part is replaced with "\\wsl$"
        if (pathParts.Length > 0)
        {
            pathParts[0] = Path.DirectorySeparatorChar + "\\wsl$";
        }

        var workingDirPath = string.Join(Path.DirectorySeparatorChar.ToString(), pathParts);
        return workingDirPath;
    }

    public static string GetNormalizedLinuxPath(string repositoryPath)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            _log.Debug("The repository path is empty");
            return string.Empty;
        }

        string[] pathParts = repositoryPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var workingDirPath = string.Join(Path.DirectorySeparatorChar.ToString(), pathParts.Skip(2));
        workingDirPath = workingDirPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        workingDirPath = workingDirPath.Insert(0, Path.AltDirectorySeparatorChar.ToString());
        return workingDirPath;
    }

    public static string GetArgumentPrefixForWsl(string repositoryPath)
    {
        if (!IsWSLRepo(repositoryPath))
        {
            _log.Debug("The repository path is not a WSL path");
            return string.Empty;
        }

        var distributionName = GetWslDistributionName(repositoryPath);

        string argumentPrefix = $"-d {distributionName} git ";
        return argumentPrefix;
    }
}
