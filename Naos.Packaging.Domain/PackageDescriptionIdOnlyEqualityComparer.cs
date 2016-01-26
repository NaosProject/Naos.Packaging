// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageDescriptionIdOnlyEqualityComparer.cs" company="Naos">
//   Copyright 2015 Naos
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Domain
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Comparer for use when running LINQ expressions on packages to only use the package ID.
    /// </summary>
    public class PackageDescriptionIdOnlyEqualityComparer : IEqualityComparer<PackageDescription>
    {
        /// <inheritdoc />
        public bool Equals(PackageDescription x, PackageDescription y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            return x.Id == y.Id;
        }

        /// <inheritdoc />
        public int GetHashCode(PackageDescription obj)
        {
            var id = obj == null ? null : obj.Id;

            var hashCode = new Tuple<string>(id).GetHashCode();
            return hashCode;
        }
    }
}