﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    /// <summary>
    /// This class exists to be a serialization type for a test involving raw queries.
    /// </summary>
    public class RawQuerySerializationClass
    {
        [JsonProperty("TotalNumberOfDevices")]
        public int TotalNumberOfDevices { get; set; }
    }
}