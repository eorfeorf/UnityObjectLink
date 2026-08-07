using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityObjectLink.Tests
{
    public sealed class UnityObjectLinkApiTests
    {
        private sealed class TemporaryAsset : ScriptableObject
        {
        }

        [Test]
        public void TryCreateLink_RejectsNullAndTemporaryObjects()
        {
            string uri;
            string error;
            Assert.That(UnityObjectLinkApi.TryCreateLink(null, out uri, out error), Is.False);
            Assert.That(error, Does.Contain("No Unity object"));

            var temporary = ScriptableObject.CreateInstance<TemporaryAsset>();
            try
            {
                Assert.That(UnityObjectLinkApi.TryCreateLink(temporary, out uri, out error), Is.False);
                Assert.That(error, Does.Contain("temporary"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }

        [Test]
        public void Settings_RequiresPreviousSchemeCleanupBeforeAnotherChange()
        {
            UnityObjectLinkSettings settings = UnityObjectLinkSettings.instance;
            string originalScheme = settings.Scheme;
            string originalProjectId = settings.ProjectId;
            string originalPreviousScheme = settings.PreviousScheme;
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            string secondScheme = "uol-second-" + suffix;
            string thirdScheme = "uol-third-" + suffix;

            try
            {
                settings.RestoreSerializedState(originalScheme, originalProjectId, string.Empty);
                string error;
                Assert.That(settings.TryUpdate(secondScheme, originalProjectId, out error), Is.True, error);
                Assert.That(settings.PreviousScheme, Is.EqualTo(originalScheme));

                Assert.That(settings.TryUpdate(thirdScheme, originalProjectId, out error), Is.False);
                Assert.That(error, Does.Contain("Unregister the previous scheme"));

                settings.ClearPreviousScheme(originalScheme);
                Assert.That(settings.TryUpdate(thirdScheme, originalProjectId, out error), Is.True, error);
            }
            finally
            {
                settings.RestoreSerializedState(originalScheme, originalProjectId, originalPreviousScheme);
            }
        }

        [TestCase("Project..Name")]
        [TestCase("日本語プロジェクト")]
        [TestCase("---")]
        [TestCase("A folder with spaces")]
        public void Settings_GeneratedProjectIdIsAlwaysValid(string projectName)
        {
            string projectId = UnityObjectLinkSettings.CreateProjectId(projectName, "01234567");
            UnityObjectLinkUri link;
            string error;

            Assert.That(projectId, Does.Not.Contain(".."));
            Assert.That(UnityObjectLinkUri.TryCreate(
                UnityObjectLinkSettings.DefaultScheme,
                projectId,
                "GlobalObjectId_V1-1-0123456789abcdef0123456789abcdef-1-0",
                out link,
                out error), Is.True, error);
        }

        [Test]
        public void LinkHandled_IsolatesFailingListeners()
        {
            Action<UnityObjectLinkResult> listener = delegate { throw new InvalidOperationException("listener failure"); };
            UnityObjectLinkApi.LinkHandled += listener;
            try
            {
                LogAssert.Expect(LogType.Error, new Regex("^\\[UnityObjectLink\\] A LinkHandled listener failed:"));
                Assert.DoesNotThrow(delegate
                {
                    UnityObjectLinkApi.RaiseLinkHandled(new UnityObjectLinkResult(UnityObjectLinkStatus.Success, "ok", null, null));
                });
            }
            finally
            {
                UnityObjectLinkApi.LinkHandled -= listener;
            }
        }
    }
}
