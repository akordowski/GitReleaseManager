//-----------------------------------------------------------------------
// <copyright file="ReleaseNotesBuilderTests.cs" company="GitTools Contributors">
//     Copyright (c) 2015 - Present - GitTools Contributors
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using GitReleaseManager.Core.ReleaseNotes;

namespace GitReleaseManager.Tests
{
    using System;
    using System.Linq;
    using ApprovalTests;
    using GitReleaseManager.Core.Configuration;
    using GitReleaseManager.Core.Helpers;
    using GitReleaseManager.Core.Model;
    using GitReleaseManager.Core.Provider;
    using NSubstitute;
    using NUnit.Framework;
    using Serilog;

    [TestFixture]
    public class ReleaseNotesBuilderTests
    {
        [Test]
        public void NoCommitsNoIssues()
        {
            var exception = Assert.Throws<AggregateException>(() => AcceptTest(0));
            Assert.That(exception.InnerException, Is.Not.Null.And.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void NoCommitsSomeIssues()
        {
            AcceptTest(0, CreateIssue(1, "Bug"), CreateIssue(2, "Feature"), CreateIssue(3, "Improvement"));
            Assert.True(true); // Just to make sonarlint happy
        }

        [Test]
        public void SomeCommitsNoIssues()
        {
            var exception = Assert.Throws<AggregateException>(() => AcceptTest(5));
            Assert.That(exception.InnerException, Is.Not.Null.And.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void SomeCommitsSomeIssues()
        {
            AcceptTest(5, CreateIssue(1, "Bug"), CreateIssue(2, "Feature"), CreateIssue(3, "Improvement"));
            Assert.True(true); // Just to make sonarlint happy
        }

        [Test]
        public void SingularCommitsNoIssues()
        {
            var exception = Assert.Throws<AggregateException>(() => AcceptTest(1));
            Assert.That(exception.InnerException, Is.Not.Null.And.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void SingularCommitsSomeIssues()
        {
            AcceptTest(1, CreateIssue(1, "Bug"), CreateIssue(2, "Feature"), CreateIssue(3, "Improvement"));
            Assert.True(true); // Just to make sonarlint happy
        }

        [Test]
        public void SingularCommitsSingularIssues()
        {
            AcceptTest(1, CreateIssue(1, "Bug"));
            Assert.True(true); // Just to make sonarlint happy
        }

        [Test]
        public void NoCommitsSingularIssues()
        {
            AcceptTest(0, CreateIssue(1, "Bug"));
            Assert.True(true); // Just to make sonarlint happy
        }

        [Test]
        public void SomeCommitsSingularIssues()
        {
            AcceptTest(5, CreateIssue(1, "Bug"));
            Assert.True(true); // Just to make sonarlint happy
        }

        [Test]
        public void SingularCommitsWithHeaderLabelAlias()
        {
            var config = new Config();
            config.LabelAliases.Add(new LabelAlias
            {
                Name = "Bug",
                Header = "Foo",
            });

            AcceptTest(1, config, CreateIssue(1, "Bug"));
            Assert.True(true); // Just to make sonarlint happy
        }

        [Test]
        public void SomeCommitsWithPluralizedLabelAlias()
        {
            var config = new Config();
            config.LabelAliases.Add(new LabelAlias
            {
                Name = "Help Wanted",
                Plural = "Bar",
            });

            AcceptTest(5, config, CreateIssue(1, "Help Wanted"), CreateIssue(2, "Help Wanted"));
            Assert.True(true); // Just to make sonarlint happy
        }

        [Test]
        public void SomeCommitsWithoutPluralizedLabelAlias()
        {
            AcceptTest(5, CreateIssue(1, "Help Wanted"), CreateIssue(2, "Help Wanted"));
            Assert.True(true); // Just to make sonarlint happy
        }

        [Test]
        public void NoCommitsWrongIssueLabel()
        {
            var exception = Assert.Throws<AggregateException>(() => AcceptTest(0, CreateIssue(1, "Test")));
            Assert.That(exception.InnerException, Is.Not.Null.And.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void SomeCommitsWrongIssueLabel()
        {
            var exception = Assert.Throws<AggregateException>(() => AcceptTest(5, CreateIssue(1, "Test")));
            Assert.That(exception.InnerException, Is.Not.Null.And.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void CorrectlyExcludeIssues()
        {
            AcceptTest(5, CreateIssue(1, "Build"), CreateIssue(2, "Bug"));
            Assert.True(true); // Just to make sonarlint happy
        }

        private static void AcceptTest(int commits, params Issue[] issues)
        {
            AcceptTest(commits, null, issues);
            Assert.True(true); // Just to make sonarlint happy
        }

        private static void AcceptTest(int commits, Config config, params Issue[] issues)
        {
            var owner = "TestUser";
            var repository = "FakeRepository";
            var milestoneNumber = 1;
            var milestoneTitle = "1.2.3";

            var vcsService = new VcsServiceMock();
            var logger = Substitute.For<ILogger>();
            var fileSystem = new FileSystem();
            var currentDirectory = Environment.CurrentDirectory;
            var configuration = config ?? ConfigurationProvider.Provide(currentDirectory, fileSystem);

            vcsService.Milestones.Add(CreateMilestone(milestoneTitle));

            vcsService.NumberOfCommits = commits;

            foreach (var issue in issues)
            {
                vcsService.Issues.Add(issue);
            }

            var vcsProvider = Substitute.For<IVcsProvider>();
            vcsProvider.GetCommitsCount(owner, repository, Arg.Any<string>(), Arg.Any<string>())
                .Returns(Task.FromResult(vcsService.NumberOfCommits));

            vcsProvider.GetCommitsUrl(owner, repository, Arg.Any<string>(), Arg.Any<string>())
                .Returns(o => new GitHubProvider(null, null).GetCommitsUrl((string)o[0], (string)o[1], (string)o[2], (string)o[3]));

            vcsProvider.GetIssuesAsync(owner, repository, milestoneNumber, ItemStateFilter.Closed)
                .Returns(Task.FromResult((IEnumerable<Issue>)vcsService.Issues));

            vcsProvider.GetMilestonesAsync(owner, repository, Arg.Any<ItemStateFilter>())
                .Returns(Task.FromResult((IEnumerable<Milestone>)vcsService.Milestones));

            var builder = new ReleaseNotesBuilder(vcsProvider, logger, configuration);
            var notes = builder.BuildReleaseNotes(owner, repository, milestoneTitle).Result;

            Approvals.Verify(notes);
        }

        private static Milestone CreateMilestone(string version)
        {
            return new Milestone
            {
                Title = version,
                Number = 1,
                HtmlUrl = "https://github.com/gep13/FakeRepository/issues?q=milestone%3A" + version,
                Version = new Version(version),
            };
        }

        private static Issue CreateIssue(int number, params string[] labels)
        {
            return new Issue
            {
                Number = number,
                Labels = labels.Select(l => new Label { Name = l }).ToList(),
                HtmlUrl = "http://example.com/" + number,
                Title = "Issue " + number,
            };
        }
    }
}