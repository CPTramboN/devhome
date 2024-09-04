﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using DevHome.Common.Services;
using LibGit2Sharp;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace FileExplorerGitIntegration.Models;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
#if CANARY_BUILD
[Guid("A65E46FF-F979-480d-A379-1FDA3EB5F7C5")]
#elif STABLE_BUILD
[Guid("8A962CBD-530D-4195-8FE3-F0DF3FDDF128")]
#else
[Guid("BDA76685-E749-4f09-8F13-C466D0802DA1")]
#endif
public class GitLocalRepositoryProviderFactory : ILocalRepositoryProvider
{
    private readonly RepositoryCache? _repositoryCache;

    public string DisplayName => "GitLocalRepositoryProviderFactory";

    private readonly StringResource _stringResource = new("FileExplorerGitIntegration.pri", "Resources");
    private readonly string _errorResourceKey = "OpenRepositoryError";

    GetLocalRepositoryResult ILocalRepositoryProvider.GetRepository(string rootPath)
    {
        var log = Log.ForContext("SourceContext", nameof(GitLocalRepositoryProviderFactory));
        try
        {
            return new GetLocalRepositoryResult(new GitLocalRepository(rootPath, _repositoryCache));
        }
        catch (RepositoryNotFoundException libGitEx)
        {
            log.Error("GitLocalRepositoryProviderFactory", "Failed to create GitLocalRepository", libGitEx);
            return new GetLocalRepositoryResult(libGitEx, _stringResource.GetLocalized("RepositoryNotFound"), $"Message: {libGitEx.Message} and HRESULT: {libGitEx.HResult}");
        }
        catch (Exception ex)
        {
            log.Error("GitLocalRepositoryProviderFactory", "Failed to create GitLocalRepository", ex);
            if (ex.Message.Contains("not owned by current user") || ex.Message.Contains("detected dubious ownership in repository"))
            {
                return new GetLocalRepositoryResult(ex, _stringResource.GetLocalized("RepositoryNotOwnedByCurrentUser"), $"Message: {ex.Message} and HRESULT: {ex.HResult}");
            }

            return new GetLocalRepositoryResult(ex, _stringResource.GetLocalized(_errorResourceKey), $"Message: {ex.Message} and HRESULT: {ex.HResult}");
        }
    }

    public GetLocalRepositoryResult GetRepository(string rootPath)
    {
        return ((ILocalRepositoryProvider)this).GetRepository(rootPath);
    }

    internal GitLocalRepositoryProviderFactory(RepositoryCache cache)
    {
        _repositoryCache = cache;
    }

    public GitLocalRepositoryProviderFactory()
    {
    }
}
