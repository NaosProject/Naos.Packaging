// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackagingJsonSerializationConfiguration.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Serialization.Json
{
    using System;
    using System.Collections.Generic;
    using Naos.Packaging.Domain;
    using OBeautifulCode.Serialization.Json;

    /// <summary>
    /// Implementation for the <see cref="Naos.Packaging" /> domain.
    /// </summary>
    public class PackagingJsonSerializationConfiguration : JsonSerializationConfigurationBase
    {
        /// <inheritdoc />
        protected override IReadOnlyCollection<string> TypeToRegisterNamespacePrefixFilters => new[]
                                                                                               {
                                                                                                   typeof(Package).Namespace,
                                                                                               };

        /// <inheritdoc />
        protected override IReadOnlyCollection<TypeToRegisterForJson> TypesToRegisterForJson => new[]
                                                                                                {
                                                                                                    typeof(Package).ToTypeToRegisterForJson(),
                                                                                                    typeof(PackageDescription).ToTypeToRegisterForJson(),
                                                                                                    typeof(PackageRepositoryConfiguration).ToTypeToRegisterForJson(),
                                                                                                };
    }
}
