﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Represents a retry policy that performs a specified number of retries, using a fixed retry delay with jitter.
    /// </summary>
    /// <remarks>
    /// Jitter can change the delay from 95% to 105% of the calculated time.
    /// </remarks>
    public class ProvisioningServiceFixedDelayRetryPolicy : ProvisioningServiceRetryPolicyBase
    {
        private readonly TimeSpan _fixedDelay;
        private readonly bool _useJitter;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retry attempts; use 0 for infinite retries.</param>
        /// <param name="fixedDelay">The fixed delay to wait between retries.</param>
        /// <param name="useJitter">Whether to add a small, random adjustment to the retry delay to avoid synchronicity in retrying clients.</param>
        /// <exception cref="ArgumentOutOfRangeException">Throw if the value of <paramref name="fixedDelay"/> is negative.</exception>
        public ProvisioningServiceFixedDelayRetryPolicy(uint maxRetries, TimeSpan fixedDelay, bool useJitter = true)
            : base(maxRetries)
        {
            Argument.AssertNotNegativeValue(fixedDelay, nameof(fixedDelay));

            _fixedDelay = fixedDelay;
            _useJitter = useJitter;
        }

        /// <inheritdoc/>
        public override bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            if (!base.ShouldRetry(currentRetryCount, lastException, out retryInterval))
            {
                return false;
            }

            retryInterval = _useJitter
                ? UpdateWithJitter(_fixedDelay.TotalMilliseconds)
                : _fixedDelay;

            return true;
        }
    }
}
