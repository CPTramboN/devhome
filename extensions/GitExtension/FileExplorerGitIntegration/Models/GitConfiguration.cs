﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Helpers;
using DevHome.Common.Services;
using Serilog;
using Windows.Storage;

namespace FileExplorerGitIntegration.Models;

public class GitConfiguration : IDisposable
{
    public GitExecutableConfigOptions GitExecutableConfigOptions { get; set; }

    private readonly FileService fileService;

    private string GitExeInstallPath { get; set; } = string.Empty;

    private readonly FileSystemWatcher fileWatcher;

    private readonly object fileLock = new();

    private readonly ILogger log = Log.ForContext<GitDetect>();

    private readonly string tempConfigurationFileName = "TemporaryGitConfiguration.json";

    public GitConfiguration(string? path)
    {
        if (RuntimeHelper.IsMSIX)
        {
            GitExecutableConfigOptions = new GitExecutableConfigOptions
            {
                GitExecutableConfigFolderPath = ApplicationData.Current.LocalFolder.Path,
            };
        }
        else
        {
            GitExecutableConfigOptions = new GitExecutableConfigOptions
            {
                GitExecutableConfigFolderPath = path ?? string.Empty,
            };
        }

        fileService = new FileService();
        EnsureConfigFileCreation();

        fileWatcher = new FileSystemWatcher(GitExecutableConfigOptions.GitExecutableConfigFolderPath, GitExecutableConfigOptions.GitExecutableConfigFileName);
        FileWatcherSettings();
    }

    private void FileWatcherSettings()
    {
        fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
        fileWatcher.Changed += OnFileChanged;
        fileWatcher.EnableRaisingEvents = true;
        log.Debug("FileSystemWatcher initialized for configuration file");
    }

    private void OnFileChanged(object sender, FileSystemEventArgs args)
    {
        if (args.Name == GitExecutableConfigOptions.GitExecutableConfigFileName)
        {
            ReadInstallPath();
        }
    }

    public string ReadInstallPath()
    {
        lock (fileLock)
        {
            GitExeInstallPath = fileService.Read<string>(GitExecutableConfigOptions.GitExecutableConfigFolderPath, GitExecutableConfigOptions.GitExecutableConfigFileName);
            return GitExeInstallPath;
        }
    }

    public void EnsureConfigFileCreation()
    {
        lock (fileLock)
        {
            if (!Directory.Exists(GitExecutableConfigOptions.GitExecutableConfigFolderPath))
            {
                Directory.CreateDirectory(GitExecutableConfigOptions.GitExecutableConfigFolderPath);
            }

            var configFileFullPath = Path.Combine(GitExecutableConfigOptions.GitExecutableConfigFolderPath, GitExecutableConfigOptions.GitExecutableConfigFileName);
            if (!File.Exists(configFileFullPath))
            {
                fileService.Save(GitExecutableConfigOptions.GitExecutableConfigFolderPath, GitExecutableConfigOptions.GitExecutableConfigFileName, string.Empty);
                log.Information("The git configuration file did not exists and has just been created");
            }
        }
    }

    public bool IsGitExeInstallPathSet()
    {
        return !string.IsNullOrEmpty(GitExeInstallPath);
    }

    public bool StoreGitExeInstallPath(string path)
    {
        lock (fileLock)
        {
            log.Information("Setting Git Exe Install Path");
            GitExeInstallPath = path;

            fileService.Save(GitExecutableConfigOptions.GitExecutableConfigFolderPath, tempConfigurationFileName, GitExeInstallPath);
            File.Replace(Path.Combine(GitExecutableConfigOptions.GitExecutableConfigFolderPath, tempConfigurationFileName), Path.Combine(GitExecutableConfigOptions.GitExecutableConfigFolderPath, GitExecutableConfigOptions.GitExecutableConfigFileName), null);
            log.Information("Git Exe Install Path stored successfully");
            return true;
        }
    }

    public void Dispose()
    {
        fileWatcher.Dispose();
        GC.SuppressFinalize(this);
    }
}
