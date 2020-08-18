﻿using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;
using Statiq.Testing;

namespace Statiq.Common.Tests.Documents
{
    [TestFixture]
    public class DocumentFixture : BaseFixture
    {
        public class ConstructorTests : DocumentFixture
        {
            [Test]
            public void IdIsNotTheSameForDifferentDocuments()
            {
                // Given, When
                Document a = new Document();
                Document b = new Document();

                // Then
                Assert.AreNotEqual(a.Id, b.Id);
            }
        }

        public class CloneTests : DocumentFixture
        {
            [Test]
            public void IdIsTheSameAfterClone()
            {
                // Given
                IDocument document = new Document();

                // When
                IDocument cloned = document.Clone(null);

                // Then
                Assert.AreEqual(document.Id, cloned.Id);
            }

            [Test]
            public void DocumentTypeTheSameAfterClone()
            {
                // Given
                IDocument document = new CustomDocument();

                // When
                IDocument cloned = document.Clone(null);

                // Then
                cloned.ShouldBeOfType<CustomDocument>();
            }

            [Test]
            public void MembersAreCloned()
            {
                // Given
                IDocument document = new CustomDocument
                {
                    Foo = "abc"
                };

                // When
                IDocument cloned = document.Clone(null);

                // Then
                ((CustomDocument)cloned).Foo.ShouldBe("abc");
            }
        }

        public class MetadataTests : DocumentFixture
        {
            [Test]
            public void MetadataOverwritesSettings()
            {
                // Given
                TestConfigurationSettings settings = new TestConfigurationSettings
                {
                    { "A", "a" }
                };
                IDocument document = new Document(settings, null, null, null, null);
                IDocument cloned = document.Clone(new MetadataItems { { "A", "b" } });

                // When
                string initialValue = document.GetString("A");
                string clonedValue = cloned.GetString("A");

                // Then
                initialValue.ShouldBe("a");
                clonedValue.ShouldBe("b");
            }

            [Test]
            public void GetsPropertyMetadata()
            {
                // Given
                IDocument document = new CustomDocument
                {
                    Foo = "abc"
                };
                IDocument cloned = document.Clone(new MetadataItems { { "Foo", "xyz" } });

                // When
                string initialValue = document.GetString("Foo");
                string clonedValue = cloned.GetString("Foo");

                // Then
                initialValue.ShouldBe("abc");
                clonedValue.ShouldBe("xyz");
            }

            [Test]
            public void MetadataOverridesProperties()
            {
                // Given
                IDocument document = new CustomDocument(new MetadataItems { { "Foo", "xyz" } })
                {
                    Foo = "abc"
                };

                // When
                string value = document.GetString("Foo");

                // Then
                value.ShouldBe("xyz");
            }

            [Test]
            public void MetadataDoesNotOverrideIDocumentProperties()
            {
                // Given
                IDocument document = new Document(new NormalizedPath("Foo.bar"));
                IDocument cloned = document.Clone(new MetadataItems { { nameof(IDocument.Destination), new NormalizedPath("Fizz.buzz") } });

                // When
                string initialValue = document.GetString(nameof(IDocument.Destination));
                string clonedValue = cloned.GetString(nameof(IDocument.Destination));

                // Then
                initialValue.ShouldBe("Foo.bar");
                clonedValue.ShouldBe("Foo.bar");
            }

            [Test]
            public void IncludesBaseDocumentProperties()
            {
                // Given, When
                Document document = new Document();

                // Then
                document.Keys.ShouldBe(new[] { "Source", "Destination", "ContentProvider" }, true);
            }
        }

        public class CountTests : DocumentFixture
        {
            [Test]
            public void GetsCorrectCount()
            {
                // Given
                MetadataItems initialMetadata = new MetadataItems
                {
                    { "A", "a" },
                    { "B", "b" },
                    { "C", "c" }
                };
                CustomDocument document = new CustomDocument(initialMetadata);

                // When
                int count = document.Count;

                // Then
                count.ShouldBe(7);
            }

            [Test]
            public void GetsCorrectCountWithProperty()
            {
                // Given
                MetadataItems initialMetadata = new MetadataItems
                {
                    { "A", "a" },
                    { "B", "b" },
                    { "C", "c" },
                    { "Foo", "foo" }
                };
                CustomDocument document = new CustomDocument(initialMetadata);

                // When
                int count = document.Count;

                // Then
                count.ShouldBe(7);
            }
        }

        private class CustomDocument : Document<CustomDocument>
        {
            public CustomDocument()
            {
            }

            public CustomDocument(IEnumerable<KeyValuePair<string, object>> items)
                : base(items)
            {
            }

            public string Foo { get; set; }
        }
    }
}
