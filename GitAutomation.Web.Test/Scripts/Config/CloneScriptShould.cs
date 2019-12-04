﻿using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Newtonsoft.Json;
using System.Linq;
using GitAutomation.DomainModels;
using GitAutomation.DomainModels.Configuration.Actions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using GitAutomation.State;
using Microsoft.Extensions.Logging;
using GitAutomation.Web;

namespace GitAutomation.Scripts.Config
{
    [Collection("GitConfiguration collection")]
    public class CloneScriptShould
    {
        private readonly ConfigGitDirectory workingGitDirectory;

        public CloneScriptShould(ConfigGitDirectory workingGitDirectory)
        {
            this.workingGitDirectory = workingGitDirectory;
        }

        [Fact]
        public async Task RecognizeAnEmptyBranch()
        {
            using (var directory = new GitDirectory())
            using (var tempDir = new TemporaryDirectory())
            {
                // Arrange it to already have a clone
                LibGit2Sharp.Repository.Clone(directory.Path, tempDir.Path);

                // Act to receive the expected FSA's
                var timestamp = DateTimeOffset.Now;
                var result = await Invoke(directory.TemporaryDirectory, tempDir, timestamp);

                // Assert that we're correct
                var standardAction = Assert.Single(result);
                var action = Assert.IsType<GitNoBranchAction>(standardAction.Payload);
                Assert.Equal(timestamp, action.StartTimestamp);
            }
        }

        [Fact]
        public async Task HandleAlreadyClonedDirectories()
        {
            using (var directory = workingGitDirectory.CreateCopy())
            // Arrange it to already have a clone
            using (var tempDir = directory.CreateCopy("--branch git-config"))
            {
                // Act to receive the expected FSA's
                var timestamp = DateTimeOffset.Now;
                var result = await Invoke(directory.TemporaryDirectory, tempDir.TemporaryDirectory, timestamp);

                // Assert that we're correct
                var standardAction = Assert.Single(result);
                var action = Assert.IsType<ReadyToLoadAction>(standardAction.Payload);
                Assert.Equal(timestamp, action.StartTimestamp);
            }
        }

        [Fact]
        public async Task CloneFreshDirectories()
        {
            using (var directory = workingGitDirectory.CreateCopy())
            using (var tempDir = new TemporaryDirectory())
            {
                // Act to receive the expected FSA's
                var timestamp = DateTimeOffset.Now;
                var result = await Invoke(directory.TemporaryDirectory, tempDir, timestamp);

                // Assert that we're correct
                var standardAction = Assert.Single(result);
                var action = Assert.IsType<ReadyToLoadAction>(standardAction.Payload);
                Assert.Equal(timestamp, action.StartTimestamp);
            }
        }

        [Fact]
        public async Task HandleBadTargetDirectories()
        {
            using (var directory = new TemporaryDirectory())
            using (var tempDir = new TemporaryDirectory())
            {
                // Act to receive the expected FSA's
                var timestamp = DateTimeOffset.Now;
                var result = await Invoke(directory, tempDir, timestamp);

                // Assert that we're correct
                var standardAction = Assert.Single(result);
                var action = Assert.IsType<GitPasswordIncorrectAction>(standardAction.Payload);
                Assert.Equal(timestamp, action.StartTimestamp);
            }
        }

        private static TargetRepositoryOptions StandardParameters(TemporaryDirectory repository, TemporaryDirectory checkout)
        {
            return new TargetRepositoryOptions
            {
                Repository = repository.Path,
                Password = "",
                UserEmail = "author@example.com",
                UserName = "A U Thor",
                CheckoutPath = checkout.Path,
                Remotes =
                {
                    { "origin", new RemoteRepositoryOptions { Repository = repository.Path } }
                }
            };
        }

        private async Task<IList<StateUpdateEvent<IStandardAction>>> Invoke(TemporaryDirectory repository, TemporaryDirectory checkout, DateTimeOffset timestamp)
        {
            var resultList = new List<StateUpdateEvent<IStandardAction>>();
            var script = new CloneScript(
                Options.Create(StandardParameters(repository, checkout)),
                new DispatchToList(resultList)
            );
            await script.Run(
                new CloneScript.CloneScriptParams(timestamp, "git-config"),
                LoggerFactory.Create(_ => { }).CreateLogger(this.GetType().FullName),
                SystemAgent.Instance
                );
            return resultList;
        }
    }

    internal class DispatchToList : IDispatcher
    {
        private List<StateUpdateEvent<IStandardAction>> resultList;

        public DispatchToList(List<StateUpdateEvent<IStandardAction>> resultList)
        {
            this.resultList = resultList;
        }

        public void Dispatch(StateUpdateEvent<IStandardAction> ev)
        {
            resultList.Add(ev);
        }
    }
}
