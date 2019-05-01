// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackagingDummyFactory.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Recipes
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    using FakeItEasy;
    using Naos.Packaging.Domain;
    using OBeautifulCode.AutoFakeItEasy;

    /// <summary>
    /// A dummy factory for Accounting Time types.
    /// </summary>
#if !NaosPackagingRecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [System.CodeDom.Compiler.GeneratedCode("Naos.Packaging", "See package version number")]
#endif
    public class PackagingDummyFactory : IDummyFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackagingDummyFactory"/> class.
        /// </summary>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This is not excessively complex.  Dummy factories typically wire-up many types.")]
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "This is not excessively complex.  Dummy factories typically wire-up many types.")]
        public PackagingDummyFactory()
        {
            AutoFixtureBackedDummyFactory.AddDummyCreator(
                () =>
                {
                    var result = new PackageDescription { Id = A.Dummy<string>(), Version = A.Dummy<string>() };

                    return result;
                });

            AutoFixtureBackedDummyFactory.AddDummyCreator(
                () =>
                {
                    var packageDescription = A.Dummy<PackageDescription>();
                    var result = new Package { PackageDescription = packageDescription, PackageFileBytes = Encoding.UTF32.GetBytes(A.Dummy<string>()), PackageFileBytesRetrievalDateTimeUtc = A.Dummy<DateTime>() };

                    return result;
                });

            AutoFixtureBackedDummyFactory.AddDummyCreator(
                () =>
                {
                    var result = new PackageRepositoryConfiguration { Source = A.Dummy<string>(), SourceName = A.Dummy<string>(), UserName = A.Dummy<string>(), ClearTextPassword = A.Dummy<string>(), ProtocolVersion = A.Dummy<int?>() };

                    return result;
                });
        }

        /// <inheritdoc />
        public Priority Priority => new FakeItEasy.Priority(1);

        /// <inheritdoc />
        public bool CanCreate(Type type)
        {
            return false;
        }

        /// <inheritdoc />
        public object Create(Type type)
        {
            return null;
        }
    }
}
