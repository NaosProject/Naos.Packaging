// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationTests.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.NuGet.Test
{
    using FakeItEasy;
    using FluentAssertions;
    using Naos.Packaging.Domain;
    using Naos.Packaging.Serialization.Bson;
    using Naos.Packaging.Serialization.Json;
    using OBeautifulCode.Serialization.Bson;
    using OBeautifulCode.Serialization.Json;
    using Xunit;

    public static class SerializationTests
    {
        [Fact]
        public static void Roundtrip_DefaultPackage()
        {
            // Arrange
            var expected = default(Package);
            var bsonSerializer = new ObcBsonSerializer<PackagingBsonConfiguration>();
            var jsonSerializer = new ObcJsonSerializer<PackagingJsonConfiguration>();

            // Act
            var actualBsonString = bsonSerializer.SerializeToString(expected);
            var actualFromBsonString = bsonSerializer.Deserialize<Package>(actualBsonString);

            var actualJsonString = jsonSerializer.SerializeToString(expected);
            var actualFromJsonString = jsonSerializer.Deserialize<Package>(actualJsonString);

            // Assert
            actualFromBsonString.Should().Be(expected);
            actualFromJsonString.Should().Be(expected);
        }

        [Fact]
        public static void Roundtrip_DefaultPropertiesPackage()
        {
            // Arrange
            var expected = new Package();
            var bsonSerializer = new ObcBsonSerializer<PackagingBsonConfiguration>();
            var jsonSerializer = new ObcJsonSerializer<PackagingJsonConfiguration>();

            // Act
            var actualBsonString = bsonSerializer.SerializeToString(expected);
            var actualFromBsonString = bsonSerializer.Deserialize<Package>(actualBsonString);

            var actualJsonString = jsonSerializer.SerializeToString(expected);
            var actualFromJsonString = jsonSerializer.Deserialize<Package>(actualJsonString);

            // Assert
            actualFromBsonString.Should().Be(expected);
            actualFromJsonString.Should().Be(expected);
        }

        [Fact]
        public static void Roundtrip_DummyPackage()
        {
            // Arrange
            var expected = A.Dummy<Package>();
            var bsonSerializer = new ObcBsonSerializer<PackagingBsonConfiguration>();
            var jsonSerializer = new ObcJsonSerializer<PackagingJsonConfiguration>();

            // Act
            var actualBsonString = bsonSerializer.SerializeToString(expected);
            var actualFromBsonString = bsonSerializer.Deserialize<Package>(actualBsonString);

            var actualJsonString = jsonSerializer.SerializeToString(expected);
            var actualFromJsonString = jsonSerializer.Deserialize<Package>(actualJsonString);

            // Assert
            actualFromBsonString.Should().Be(expected);
            actualFromJsonString.Should().Be(expected);
        }

        [Fact]
        public static void Roundtrip_DefaultPackageDescription()
        {
            // Arrange
            var expected = default(PackageDescription);
            var jsonSerializer = new ObcJsonSerializer<PackagingJsonConfiguration>();

            // Act
            var actualJsonString = jsonSerializer.SerializeToString(expected);
            var actualFromJsonString = jsonSerializer.Deserialize<PackageDescription>(actualJsonString);

            // Assert
            actualFromJsonString.Should().Be(expected);
        }

        [Fact]
        public static void Roundtrip_DefaultPropertiesPackageDescription()
        {
            // Arrange
            var expected = new PackageDescription();
            var bsonSerializer = new ObcBsonSerializer<PackagingBsonConfiguration>();
            var jsonSerializer = new ObcJsonSerializer<PackagingJsonConfiguration>();

            // Act
            var actualBsonString = bsonSerializer.SerializeToString(expected);
            var actualFromBsonString = bsonSerializer.Deserialize<PackageDescription>(actualBsonString);

            var actualJsonString = jsonSerializer.SerializeToString(expected);
            var actualFromJsonString = jsonSerializer.Deserialize<PackageDescription>(actualJsonString);

            // Assert
            actualFromBsonString.Should().Be(expected);
            actualFromJsonString.Should().Be(expected);
        }

        [Fact]
        public static void Roundtrip_DummyPackageDescription()
        {
            // Arrange
            var expected = A.Dummy<PackageDescription>();
            var bsonSerializer = new ObcBsonSerializer<PackagingBsonConfiguration>();
            var jsonSerializer = new ObcJsonSerializer<PackagingJsonConfiguration>();

            // Act
            var actualBsonString = bsonSerializer.SerializeToString(expected);
            var actualFromBsonString = bsonSerializer.Deserialize<PackageDescription>(actualBsonString);

            var actualJsonString = jsonSerializer.SerializeToString(expected);
            var actualFromJsonString = jsonSerializer.Deserialize<PackageDescription>(actualJsonString);

            // Assert
            actualFromBsonString.Should().Be(expected);
            actualFromJsonString.Should().Be(expected);
        }

        [Fact]
        public static void Roundtrip_DefaultPackageRepositoryConfiguration()
        {
            // Arrange
            var expected = default(PackageRepositoryConfiguration);
            var bsonSerializer = new ObcBsonSerializer<PackagingBsonConfiguration>();
            var jsonSerializer = new ObcJsonSerializer<PackagingJsonConfiguration>();

            // Act
            var actualBsonString = bsonSerializer.SerializeToString(expected);
            var actualFromBsonString = bsonSerializer.Deserialize<PackageRepositoryConfiguration>(actualBsonString);

            var actualJsonString = jsonSerializer.SerializeToString(expected);
            var actualFromJsonString = jsonSerializer.Deserialize<PackageRepositoryConfiguration>(actualJsonString);

            // Assert
            actualFromBsonString.Should().Be(expected);
            actualFromJsonString.Should().Be(expected);
        }

        [Fact]
        public static void Roundtrip_DefaultPropertiesPackageRepositoryConfiguration()
        {
            // Arrange
            var expected = new PackageRepositoryConfiguration();
            var bsonSerializer = new ObcBsonSerializer<PackagingBsonConfiguration>();
            var jsonSerializer = new ObcJsonSerializer<PackagingJsonConfiguration>();

            // Act
            var actualBsonString = bsonSerializer.SerializeToString(expected);
            var actualFromBsonString = bsonSerializer.Deserialize<PackageRepositoryConfiguration>(actualBsonString);

            var actualJsonString = jsonSerializer.SerializeToString(expected);
            var actualFromJsonString = jsonSerializer.Deserialize<PackageRepositoryConfiguration>(actualJsonString);

            // Assert
            actualFromBsonString.Should().Be(expected);
            actualFromJsonString.Should().Be(expected);
        }

        [Fact]
        public static void Roundtrip_DummyPackageRepositoryConfiguration()
        {
            // Arrange
            var expected = A.Dummy<PackageRepositoryConfiguration>();
            var bsonSerializer = new ObcBsonSerializer<PackagingBsonConfiguration>();
            var jsonSerializer = new ObcJsonSerializer<PackagingJsonConfiguration>();

            // Act
            var actualBsonString = bsonSerializer.SerializeToString(expected);
            var actualFromBsonString = bsonSerializer.Deserialize<PackageRepositoryConfiguration>(actualBsonString);

            var actualJsonString = jsonSerializer.SerializeToString(expected);
            var actualFromJsonString = jsonSerializer.Deserialize<PackageRepositoryConfiguration>(actualJsonString);

            // Assert
            actualFromBsonString.Should().Be(expected);
            actualFromJsonString.Should().Be(expected);
        }
    }
}
