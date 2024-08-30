using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;
using Statiq.Testing;

namespace Statiq.Common.Tests.Documents
{
    [TestFixture]
    public class ObjectDocumentFixture : BaseFixture
    {
        public class ConstructorTests : ObjectDocumentFixture
        {
            [Test]
            public void IdIsNotTheSameForDifferentDocuments()
            {
                // Given, When
                CustomObject obj = new CustomObject();
                IDocument a = new ObjectDocument<CustomObject>(obj);
                IDocument b = new ObjectDocument<CustomObject>(obj);

                // Then
                Assert.That(a.Id, Is.Not.EqualTo(b.Id));
            }
        }

        public class CloneTests : ObjectDocumentFixture
        {
            [Test]
            public void IdIsTheSameAfterClone()
            {
                // Given
                CustomObject obj = new CustomObject();
                IDocument document = new ObjectDocument<CustomObject>(obj);

                // When
                IDocument cloned = document.Clone(null);

                // Then
                Assert.That(document.Id, Is.EqualTo(cloned.Id));
            }

            [Test]
            public void DocumentTypeTheSameAfterClone()
            {
                // Given
                CustomObject obj = new CustomObject();
                IDocument document = new ObjectDocument<CustomObject>(obj);

                // When
                IDocument cloned = document.Clone(null);

                // Then
                cloned.ShouldBeOfType<ObjectDocument<CustomObject>>();
            }

            [Test]
            public void MembersAreCloned()
            {
                // Given
                CustomObject obj = new CustomObject
                {
                    Foo = "abc"
                };
                IDocument document = new ObjectDocument<CustomObject>(obj);

                // When
                IDocument cloned = document.Clone(null);

                // Then
                ((ObjectDocument<CustomObject>)cloned).Object.Foo.ShouldBe("abc");
            }
        }

        public class MetadataTests : ObjectDocumentFixture
        {
            [Test]
            public void DoesNotIncludeDocumentProperties()
            {
                // Given
                CustomObject obj = new CustomObject
                {
                    Foo = "abc"
                };
                MetadataItems settings = new MetadataItems
                {
                    { "A", "a" }
                };

                // When
                IDocument document = new ObjectDocument<CustomObject>(obj, new Metadata(settings));

                // Then
                document.Keys.ShouldBe(new[] { "Foo", "A" }, true);
            }

            [Test]
            public void MetadataOverwritesSettings()
            {
                // Given
                CustomObject obj = new CustomObject
                {
                    Foo = "abc"
                };
                MetadataItems settings = new MetadataItems
                {
                    { "A", "a" }
                };
                IDocument document = new ObjectDocument<CustomObject>(obj, new Metadata(settings));
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
                CustomObject obj = new CustomObject
                {
                    Foo = "abc"
                };
                IDocument document = new ObjectDocument<CustomObject>(obj);
                IDocument cloned = document.Clone(new MetadataItems { { "Foo", "xyz" } });

                // When
                string initialValue = document.GetString("Foo");
                string clonedValue = cloned.GetString("Foo");

                // Then
                initialValue.ShouldBe("abc");
                clonedValue.ShouldBe("xyz");
            }
        }

        public class CountTests : ObjectDocumentFixture
        {
            [Test]
            public void GetsCorrectCount()
            {
                // Given
                CustomObject obj = new CustomObject
                {
                    Foo = "abc"
                };
                MetadataItems initialMetadata = new MetadataItems
                {
                    { "A", "a" },
                    { "B", "b" },
                    { "C", "c" }
                };
                IDocument document = new ObjectDocument<CustomObject>(obj, initialMetadata);

                // When
                int count = document.Count;

                // Then
                count.ShouldBe(4);
            }

            [Test]
            public void GetsCorrectCountWithProperty()
            {
                // Given
                CustomObject obj = new CustomObject
                {
                    Foo = "abc"
                };
                MetadataItems initialMetadata = new MetadataItems
                {
                    { "A", "a" },
                    { "B", "b" },
                    { "C", "c" },
                    { "Foo", "foo" }
                };
                IDocument document = new ObjectDocument<CustomObject>(obj, initialMetadata);

                // When
                int count = document.Count;

                // Then
                count.ShouldBe(4);
            }
        }

        private class CustomObject
        {
            public string Foo { get; set; }
        }
    }
}