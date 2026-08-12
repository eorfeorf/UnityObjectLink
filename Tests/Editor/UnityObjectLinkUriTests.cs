using NUnit.Framework;

namespace UnityObjectLink.Tests
{
    public sealed class UnityObjectLinkUriTests
    {
        private const string GlobalId = "GlobalObjectId_V1-1-0123456789abcdef0123456789abcdef-123456789-0";

        [Test]
        public void CreateAndParse_RoundTripsEncodedValues()
        {
            UnityObjectLinkUri created;
            string error;
            Assert.That(UnityObjectLinkUri.TryCreate("Unity-Object-Link", "sample_project", GlobalId, out created, out error), Is.True, error);

            UnityObjectLinkUri parsed;
            Assert.That(UnityObjectLinkUri.TryParse(created.ToString(), "unity-object-link", "sample_project", out parsed, out error), Is.True, error);
            Assert.That(parsed.Scheme, Is.EqualTo("unity-object-link"));
            Assert.That(parsed.ProjectId, Is.EqualTo("sample_project"));
            Assert.That(parsed.GlobalObjectId, Is.EqualTo(GlobalId));
        }

        [TestCase("1bad")]
        [TestCase("has space")]
        [TestCase("a_underscore")]
        [TestCase("")]
        public void Create_RejectsInvalidScheme(string scheme)
        {
            UnityObjectLinkUri link;
            string error;
            Assert.That(UnityObjectLinkUri.TryCreate(scheme, "project", GlobalId, out link, out error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [TestCase("../project")]
        [TestCase("a..b")]
        [TestCase("project/name")]
        [TestCase("project name")]
        [TestCase("")]
        public void Create_RejectsUnsafeProjectId(string projectId)
        {
            UnityObjectLinkUri link;
            string error;
            Assert.That(UnityObjectLinkUri.TryCreate("unity-object-link", projectId, GlobalId, out link, out error), Is.False);
        }

        [Test]
        public void Create_AcceptsMaximumFieldLengths()
        {
            string scheme = "s" + new string('a', 31);
            string projectId = "p" + new string('a', 63);
            string objectId = "GlobalObjectId_V1-" + new string('a', 512 - "GlobalObjectId_V1-".Length);
            UnityObjectLinkUri link;
            string error;

            Assert.That(UnityObjectLinkUri.TryCreate(scheme, projectId, objectId, out link, out error), Is.True, error);
        }

        [Test]
        public void Create_RejectsFieldsOverMaximumLengths()
        {
            UnityObjectLinkUri link;
            string error;
            string validObject = "GlobalObjectId_V1-1-a-1-0";

            Assert.That(UnityObjectLinkUri.TryCreate("s" + new string('a', 32), "project", validObject, out link, out error), Is.False);
            Assert.That(UnityObjectLinkUri.TryCreate("scheme", "p" + new string('a', 64), validObject, out link, out error), Is.False);
            Assert.That(UnityObjectLinkUri.TryCreate("scheme", "project", "GlobalObjectId_V1-" + new string('a', 512 - "GlobalObjectId_V1-".Length + 1), out link, out error), Is.False);
        }

        [TestCase("unity-object-link://select?v=2&project=sample&object=GlobalObjectId_V1-1-a-1-0")]
        [TestCase("unity-object-link://select?v=1&project=sample")]
        [TestCase("unity-object-link://select?v=1&project=sample&object=GlobalObjectId_V1-1-a-1-0&extra=x")]
        [TestCase("unity-object-link://select?v=1&v=1&project=sample&object=GlobalObjectId_V1-1-a-1-0")]
        [TestCase("unity-object-link://other?v=1&project=sample&object=GlobalObjectId_V1-1-a-1-0")]
        [TestCase("unity-object-link://select/path?v=1&project=sample&object=GlobalObjectId_V1-1-a-1-0")]
        [TestCase("unity-object-link://select?v=1&project=sample&object=%ZZ")]
        public void Parse_RejectsMalformedUri(string uri)
        {
            UnityObjectLinkUri link;
            string error;
            Assert.That(UnityObjectLinkUri.TryParse(uri, "unity-object-link", "sample", out link, out error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void Parse_RejectsWrongSchemeAndProject()
        {
            string uri = "other-scheme://select?v=1&project=other&object=" + GlobalId;
            UnityObjectLinkUri link;
            string error;
            Assert.That(UnityObjectLinkUri.TryParse(uri, "unity-object-link", "sample", out link, out error), Is.False);

            uri = "unity-object-link://select?v=1&project=other&object=" + GlobalId;
            Assert.That(UnityObjectLinkUri.TryParse(uri, "unity-object-link", "sample", out link, out error), Is.False);
            Assert.That(error, Does.Contain("different Unity project"));
        }

        [Test]
        public void Parse_DecodesPercentEncodedObjectText()
        {
            string uri = "unity-object-link://select?v=1&project=sample&object=%47lobalObjectId_V1-1-0123456789abcdef0123456789abcdef-1-0";
            UnityObjectLinkUri link;
            string error;
            Assert.That(UnityObjectLinkUri.TryParse(uri, "unity-object-link", "sample", out link, out error), Is.True, error);
            Assert.That(link.GlobalObjectId, Does.StartWith("GlobalObjectId_V1-"));
        }

        [Test]
        public void Parse_AcceptsOsCanonicalTrailingSlash()
        {
            string uri = "unity-object-link://select/?v=1&project=sample&object=" + GlobalId;
            UnityObjectLinkUri link;
            string error;
            Assert.That(UnityObjectLinkUri.TryParse(uri, "unity-object-link", "sample", out link, out error), Is.True, error);
        }

        [Test]
        public void Parse_RejectsOversizedInput()
        {
            string uri = "unity-object-link://select?v=1&project=sample&object=GlobalObjectId_V1-" + new string('a', UnityObjectLinkUri.MaximumUriLength);
            UnityObjectLinkUri link;
            string error;
            Assert.That(UnityObjectLinkUri.TryParse(uri, null, null, out link, out error), Is.False);
        }
    }
}
