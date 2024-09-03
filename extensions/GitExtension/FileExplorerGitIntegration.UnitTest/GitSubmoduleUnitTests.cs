﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FileExplorerGitIntegration.Models;

namespace FileExplorerGitIntegration.UnitTest;

[TestClass]
public class GitSubmoduleUnitTests
{
    private const string FolderStatusProp = "System.VersionControl.CurrentFolderStatus";
    private const string StatusProp = "System.VersionControl.Status";
    private const string ShaProp = "System.VersionControl.LastChangeID";

    private static SandboxHelper? _sandbox;
    private static GitLocalRepository? _repo;

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
        _sandbox = new();
        var repopath = _sandbox.CreateSandbox("submodules");
        _sandbox.CreateSandbox("submodules_target");
        _repo = new GitLocalRepository(repopath);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        if (_sandbox is not null)
        {
            _sandbox.Cleanup();
            _sandbox = null;
        }

        _repo = null;
    }

    [TestMethod]
    [DataRow("", FolderStatusProp, "Branch: main | +1 ~1 -0 | +0 ~7 -0")]
    [DataRow(".gitmodules", StatusProp, "Staged, Modified")]
    [DataRow("README.txt", StatusProp, "")]
    [DataRow("sm_added_and_uncommitted", StatusProp, "Staged")]
    [DataRow("sm_changed_file", StatusProp, "Submodule dirty")]
    [DataRow("sm_changed_head", StatusProp, "Submodule changed")]
    [DataRow("sm_changed_index", StatusProp, "Submodule dirty")]
    [DataRow("sm_changed_untracked_file", StatusProp, "Submodule dirty")]
    [DataRow("sm_missing_commits", StatusProp, "Submodule changed")]
    [DataRow("sm_missing_commits_detached", StatusProp, "Submodule changed")]
    [DataRow("sm_unchanged", StatusProp, "")]
    [DataRow("sm_unchanged_detached", StatusProp, "")]
    public void RootFolderStatus(string path, string property, string value)
    {
        Assert.IsNotNull(_repo);
        var result = _repo.GetProperties([property], path);
        Assert.IsNotNull(result);
        Assert.AreEqual(value, result[property]);
    }

    [TestMethod]

    [DataRow("", ShaProp, "d8ebdc0b3c1d5240d4fc1c4cd3728ff561e714ad")]
    [DataRow(".gitmodules", ShaProp, "d8ebdc0b3c1d5240d4fc1c4cd3728ff561e714ad")]
    [DataRow("README.txt", ShaProp, "74b157c3bfd2f24323c3bc6e5e96639a424f157f")]
    [DataRow("sm_added_and_uncommitted", ShaProp, "")]
    [DataRow("sm_changed_file", ShaProp, "d8ebdc0b3c1d5240d4fc1c4cd3728ff561e714ad")]
    [DataRow("sm_changed_head", ShaProp, "d8ebdc0b3c1d5240d4fc1c4cd3728ff561e714ad")]
    [DataRow("sm_changed_index", ShaProp, "d8ebdc0b3c1d5240d4fc1c4cd3728ff561e714ad")]
    [DataRow("sm_changed_untracked_file", ShaProp, "d8ebdc0b3c1d5240d4fc1c4cd3728ff561e714ad")]
    [DataRow("sm_missing_commits", ShaProp, "d8ebdc0b3c1d5240d4fc1c4cd3728ff561e714ad")]
    [DataRow("sm_missing_commits_detached", ShaProp, "d8ebdc0b3c1d5240d4fc1c4cd3728ff561e714ad")]
    [DataRow("sm_unchanged", ShaProp, "d8ebdc0b3c1d5240d4fc1c4cd3728ff561e714ad")]
    [DataRow("sm_unchanged_detached", ShaProp, "d8ebdc0b3c1d5240d4fc1c4cd3728ff561e714ad")]
    public void RootFolderCommit(string path, string property, string value)
    {
        Assert.IsNotNull(_repo);
        var result = _repo.GetProperties([property], path);
        Assert.IsNotNull(result);
        if (result.TryGetValue(property, out var actual))
        {
            Assert.AreEqual(value, actual);
        }
        else
        {
            Assert.AreEqual(value, string.Empty);
        }
    }

    [TestMethod]
    [DataRow("sm_added_and_uncommitted\\file_to_modify", ShaProp, "e9a899083a7e2b25d7a41e69463ce083bf9ef6ef")]
    [DataRow("sm_changed_file\\file_to_modify", ShaProp, "e9a899083a7e2b25d7a41e69463ce083bf9ef6ef")]
    [DataRow("sm_changed_head\\file_to_modify", ShaProp, "1973c02c4ac6e5743199e14fa4311746f9b39fbe")]
    [DataRow("sm_changed_index\\file_to_modify", ShaProp, "e9a899083a7e2b25d7a41e69463ce083bf9ef6ef")]
    [DataRow("sm_changed_untracked_file\\file_to_modify", ShaProp, "e9a899083a7e2b25d7a41e69463ce083bf9ef6ef")]
    [DataRow("sm_missing_commits\\file_to_modify", ShaProp, "8e623bcf5aeceb8af7c0f0b22b82322f6c82fd4b")]
    [DataRow("sm_missing_commits_detached\\file_to_modify", ShaProp, "8e623bcf5aeceb8af7c0f0b22b82322f6c82fd4b")]
    [DataRow("sm_unchanged\\file_to_modify", ShaProp, "e9a899083a7e2b25d7a41e69463ce083bf9ef6ef")]
    [DataRow("sm_unchanged_detached\\file_to_modify", ShaProp, "e9a899083a7e2b25d7a41e69463ce083bf9ef6ef")]
    public void SubmoduleFilesCommit(string path, string property, string value)
    {
        Assert.IsNotNull(_repo);
        var result = _repo.GetProperties([property], path);
        Assert.IsNotNull(result);
        if (result.TryGetValue(property, out var actual))
        {
            Assert.AreEqual(value, actual);
        }
        else
        {
            Assert.AreEqual(value, string.Empty);
        }
    }
}