﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpAuthenticationRefresher : IAmqpAuthenticationRefresher, IDisposable
    {
        private static readonly string[] s_accessRightsStringArray = AccessRightsHelper.AccessRightsToStringArray(AccessRights.DeviceConnect);
        private readonly Uri _amqpEndpoint;
        private readonly AmqpIotCbsLink _amqpIotCbsLink;
        private readonly IClientConfiguration _clientConfiguration;
        private readonly AmqpIotCbsTokenProvider _amqpIotCbsTokenProvider;
        private readonly string _audience;
        private Task _refreshLoop;
        private bool _disposed;

        internal AmqpAuthenticationRefresher(IClientConfiguration clientConfiguration, AmqpIotCbsLink amqpCbsLink)
        {
            _amqpIotCbsLink = amqpCbsLink;
            _clientConfiguration = clientConfiguration;
            _audience = clientConfiguration.AmqpCbsAudience;
            _amqpIotCbsTokenProvider = new AmqpIotCbsTokenProvider(_clientConfiguration);
            _amqpEndpoint = new UriBuilder(CommonConstants.AmqpsScheme, clientConfiguration.HostName, CommonConstants.DefaultAmqpSecurePort).Uri;

            if (Logging.IsEnabled)
            {
                Logging.Associate(this, clientConfiguration, nameof(clientConfiguration));
                Logging.Associate(this, amqpCbsLink, nameof(_amqpIotCbsLink));
            }
        }

        public async Task InitLoopAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(InitLoopAsync));

            DateTime refreshOn = await _amqpIotCbsLink
                .SendTokenAsync(
                    _amqpIotCbsTokenProvider,
                    _amqpEndpoint,
                    _audience,
                    _audience,
                    s_accessRightsStringArray,
                    cancellationToken)
                .ConfigureAwait(false);

            if (refreshOn < DateTime.MaxValue)
            {
                StartLoop(refreshOn, cancellationToken);
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, nameof(InitLoopAsync));
        }

        public void StartLoop(DateTime refreshOn, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, refreshOn, nameof(StartLoop));

            _refreshLoop = RefreshLoopAsync(refreshOn, cancellationToken);

            if (Logging.IsEnabled)
                Logging.Exit(this, refreshOn, nameof(StartLoop));
        }

        private async Task RefreshLoopAsync(DateTime refreshesOn, CancellationToken cancellationToken)
        {
            TimeSpan waitTime = refreshesOn - DateTime.UtcNow;
            Debug.Assert(_clientConfiguration.TokenRefresher != null);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (Logging.IsEnabled)
                    Logging.Info(this, refreshesOn, $"Before {nameof(RefreshLoopAsync)}");

                if (waitTime > TimeSpan.Zero)
                {
                    await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        refreshesOn = await _amqpIotCbsLink
                            .SendTokenAsync(
                                _amqpIotCbsTokenProvider,
                                _amqpEndpoint,
                                _audience,
                                _audience,
                                s_accessRightsStringArray,
                                cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (IotHubCommunicationException ex)
                    {
                        if (Logging.IsEnabled)
                        {
                            Logging.Error(this, refreshesOn, $"Refresh token failed {ex}");
                        }
                    }
                    finally
                    {
                        if (Logging.IsEnabled)
                            Logging.Info(this, refreshesOn, $"After {nameof(RefreshLoopAsync)}");
                    }

                    waitTime = refreshesOn - DateTime.UtcNow;
                }
            }
        }

        public void StopLoop()
        {
            if (Logging.IsEnabled)
                Logging.Info(this, nameof(StopLoop));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            try
            {
                if (Logging.IsEnabled)
                {
                    Logging.Enter(this, $"Disposed={_disposed}; disposing={disposing}", $"{nameof(AmqpAuthenticationRefresher)}.{nameof(Dispose)}");
                }

                if (!_disposed)
                {
                    if (disposing)
                    {
                        StopLoop();
                        _amqpIotCbsTokenProvider?.Dispose();
                    }

                    _disposed = true;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, $"Disposed={_disposed}; disposing={disposing}", $"{nameof(AmqpAuthenticationRefresher)}.{nameof(Dispose)}");
                }
            }
        }
    }
}
