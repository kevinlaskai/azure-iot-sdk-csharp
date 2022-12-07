﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The provisioning status.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProvisioningRegistrationStatus
    {
        /// <summary>
        /// Device has not yet come online.
        /// </summary>
        Unassigned,

        /// <summary>
        /// Device has connected to the DRS but IoT hub Id has not yet been returned to the device.
        /// </summary>
        Assigning,

        /// <summary>
        /// DRS successfully returned a device Id and connection string to the device.
        /// </summary>
        Assigned,

        /// <summary>
        /// Device enrollment failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Device is disabled.
        /// </summary>
        Disabled,
    }
}