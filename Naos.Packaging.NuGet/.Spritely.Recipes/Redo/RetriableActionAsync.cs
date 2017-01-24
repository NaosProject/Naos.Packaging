﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RetriableActionAsync.cs">
//   Copyright (c) 2016. All rights reserved.
//   Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>
// <auto-generated>
//   Sourced from NuGet package. Will be overwritten with package update except in Spritely.Redo source.
// </auto-generated>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Redo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    ///     Represents a retriable operation which can accumulate terminating conditions using .Until(...)
    ///     or perform the execution using .Now().
    /// </summary>
#if !SpritelyRecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [System.CodeDom.Compiler.GeneratedCode("Spritely.Recipes", "See package version number")]
#pragma warning disable 0436
#endif
    internal partial class RetriableActionAsync
    {
        private readonly Func<long, TimeSpan> getDelay;
        private readonly long maxRetries;
        private readonly Action<Exception> report;
        private readonly ICollection<Type> exceptionsToRetryOn;
        private readonly ICollection<Type> exceptionsToThrowOn;
        private readonly Func<Task> operation;
        private readonly ICollection<Func<bool>> untilValids = new List<Func<bool>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RetriableAction"/> class.
        /// </summary>
        /// <param name="getDelay">The function that determines the next delay given the current attempt number.</param>
        /// <param name="maxRetries">The maximum number of retries to perform the operation before giving up and throwing the underlying exception.</param>
        /// <param name="report">The function that reports on any exceptions as they occur (even if they are handled).</param>
        /// <param name="exceptionsToRetryOn">The exceptions to retry on.</param>
        /// <param name="exceptionsToThrowOn">The exceptions to throw on.</param>
        /// <param name="operation">The operation to execute and retry as needed.</param>
        public RetriableActionAsync(Func<long, TimeSpan> getDelay, long maxRetries, Action<Exception> report, ICollection<Type> exceptionsToRetryOn, ICollection<Type> exceptionsToThrowOn, Func<Task> operation)
        {
            if (getDelay == null)
            {
                throw new ArgumentNullException("getDelay");
            }

            if (maxRetries < 1)
            {
                throw new ArgumentOutOfRangeException("maxRetries", maxRetries, "Value must be at least 1.");
            }

            if (report == null)
            {
                throw new ArgumentNullException("report");
            }

            if (exceptionsToRetryOn == null)
            {
                throw new ArgumentNullException("exceptionsToRetryOn");
            }

            if (exceptionsToThrowOn == null)
            {
                throw new ArgumentNullException("exceptionsToThrowOn");
            }

            if (operation == null)
            {
                throw new ArgumentNullException("operation");
            }

            this.getDelay = getDelay;
            this.maxRetries = maxRetries;
            this.report = report;
            this.exceptionsToRetryOn = exceptionsToRetryOn;
            this.exceptionsToThrowOn = exceptionsToThrowOn;
            this.operation = operation;
        }

        /// <summary>
        /// Adds a validity check the value must pass before it is considered valid.
        /// If multiple Until values are provided then if Any pass the value is considered valid.
        /// </summary>
        /// <param name="isValid">The function to call to verify validity.</param>
        /// <returns>This instance for chaining other operations.</returns>
        public RetriableActionAsync Until(Func<bool> isValid)
        {
            if (isValid == null)
            {
                throw new ArgumentNullException("isValid");
            }

            untilValids.Add(isValid);

            return this;
        }

        /// <summary>
        /// Performs the asynchronous operation Now retrying as appropriate.
        /// </summary>
        /// <returns>The task being awaited for managing the asynchronous operation.</returns>
        public async Task Now()
        {
            var attempt = 0L;

            while (true)
            {
                try
                {
                    await operation();

                    if (!untilValids.Any() || untilValids.Any(isValid => isValid()))
                    {
                        return;
                    }

                    if (attempt >= maxRetries)
                    {
                        throw new TimeoutException($"Execution timed out after {maxRetries} attempts.");
                    }
                }
                catch (Exception ex)
                {
                    report(ex);

                    var shouldThrow = exceptionsToThrowOn.Any(t => t.IsInstanceOfType(ex));

                    // If there are no exception handlers then handle all exceptions by default
                    var shouldHandle = !exceptionsToRetryOn.Any() ||
                                       exceptionsToRetryOn.Any(t => t.IsInstanceOfType(ex));

                    if (shouldThrow || !shouldHandle)
                    {
                        throw;
                    }

                    if (attempt >= maxRetries)
                    {
                        throw;
                    }
                }

                var delay = getDelay(attempt++);
                await Task.Delay(delay);
            }
        }
    }
#if !SpritelyRecipesProject
#pragma warning restore 0436
#endif
}
