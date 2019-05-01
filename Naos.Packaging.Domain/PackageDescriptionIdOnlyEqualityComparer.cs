// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PackageDescriptionIdOnlyEqualityComparer.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Packaging.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using OBeautifulCode.Validation.Recipes;

    /// <summary>
    /// Comparer for use when running LINQ expressions on packages to only use the package ID.
    /// </summary>
    public class PackageDescriptionIdOnlyEqualityComparer : IEqualityComparer<PackageDescription>
    {
        /// <summary>
        /// Compares the distinct package IDs of two sets for equality.
        /// </summary>
        /// <param name="firstSet">First set of packages to compare.</param>
        /// <param name="secondSet">Second set of packages to compare.</param>
        /// <returns>Whether or not the distinct package IDs match exactly.</returns>
        public static bool DistinctPackageIdsMatchExactly(
            ICollection<PackageDescription> firstSet,
            ICollection<PackageDescription> secondSet)
        {
            new { firstSet }.Must().NotBeNull();
            new { secondSet }.Must().NotBeNull();

            if (firstSet.Count == 0 && secondSet.Count == 0)
            {
                return true;
            }

            var firstSetDistinctOrderedIds = firstSet.Select(_ => _.Id).ToList().Distinct().OrderBy(_ => _);
            var secondSetDistinctOrderedIds = secondSet.Select(_ => _.Id).ToList().Distinct().OrderBy(_ => _);

            var ret = firstSetDistinctOrderedIds.SequenceEqual(secondSetDistinctOrderedIds, StringComparer.CurrentCultureIgnoreCase);
            return ret;
        }

        /// <inheritdoc />
        public bool Equals(PackageDescription x, PackageDescription y)
        {
            if (x == null || y == null)
            {
                return false;
            }

            return x.Id.Equals(y.Id, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <inheritdoc />
        public int GetHashCode(PackageDescription obj)
        {
            new { obj }.Must().NotBeNull();

            var id = obj.Id;
            var hashCode = new Tuple<string>(id.ToUpperInvariant()).GetHashCode();
            return hashCode;
        }
    }
}