// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// A shared access signature, which can be used for authorization to an IoT hub.
    /// </summary>
    internal sealed class SharedAccessSignature
    {
        internal SharedAccessSignature(
            string iotHubName,
            DateTimeOffset expiresOn,
            string keyName,
            string signature,
            string encodedAudience)
        {
            if (string.IsNullOrWhiteSpace(iotHubName))
            {
                throw new ArgumentNullException(nameof(iotHubName));
            }

            ExpiresOnUtc = expiresOn;

            if (IsExpired())
            {
                throw new IotHubServiceException(
                    "The specified SAS token is expired",
                    HttpStatusCode.Unauthorized,
                    IotHubServiceErrorCode.IotHubUnauthorizedAccess);
            }

            IotHubName = iotHubName;
            Signature = signature;
            Audience = WebUtility.UrlDecode(encodedAudience);
            KeyName = keyName ?? string.Empty;
        }

        /// <summary>
        /// The IoT hub name.
        /// </summary>
        internal string IotHubName { get; private set; }

        /// <summary>
        /// The date and time the SAS expires.
        /// </summary>
        internal DateTimeOffset ExpiresOnUtc { get; private set; }

        /// <summary>
        /// Name of the authorization rule.
        /// </summary>
        internal string KeyName { get; private set; }

        /// <summary>
        /// The audience scope to which this signature applies.
        /// </summary>
        internal string Audience { get; private set; }

        /// <summary>
        /// The value of the shared access signature.
        /// </summary>
        internal string Signature { get; private set; }

        /// <summary>
        /// Indicates if the token has expired.
        /// </summary>
        internal bool IsExpired()
        {
            return ExpiresOnUtc + SharedAccessSignatureConstants.MaxClockSkew < DateTime.UtcNow;
        }

        /// <summary>
        /// The date and time of expiration.
        /// </summary>
        internal DateTimeOffset ExpiryTime()
        {
            return ExpiresOnUtc + SharedAccessSignatureConstants.MaxClockSkew;
        }
    }
}