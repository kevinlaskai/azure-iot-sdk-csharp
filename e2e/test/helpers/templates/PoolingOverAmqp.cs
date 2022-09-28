﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Devices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.Helpers.Templates
{
    public static class PoolingOverAmqp
    {
        public const int SingleConnection_DevicesCount = 2;
        public const int SingleConnection_PoolSize = 1;
        public const int MultipleConnections_DevicesCount = 4;
        public const int MultipleConnections_PoolSize = 2;
        public const int MaxTestRunCount = 5;
        public const int TestSuccessRate = 80; // 4 out of 5 (80%) test runs should pass (even after accounting for network instability issues).

        public static async Task TestPoolAmqpAsync(
            string devicePrefix,
            IotHubClientAmqpSettings transportSettings,
            int poolSize,
            int devicesCount,
            Func<IotHubDeviceClient, TestDevice, TestDeviceCallbackHandler, Task> initOperation,
            Func<IotHubDeviceClient, TestDevice, TestDeviceCallbackHandler, Task> testOperation,
            Func<Task> cleanupOperation,
            ConnectionStringAuthScope authScope,
            bool ignoreConnectionStatus,
            MsTestLogger logger)
        {
            transportSettings.ConnectionPoolSettings = new AmqpConnectionPoolSettings
            {
                MaxPoolSize = unchecked((uint)poolSize),
                Pooling = true,
            };

            int totalRuns = 0;
            int successfulRuns = 0;
            int currentSuccessRate = 0;
            bool reRunTest = false;

            var testDevices = new List<TestDevice>(devicesCount);
            var deviceClients = new List<IotHubDeviceClient>(devicesCount);
            var testDeviceCallbackHandlers = new List<TestDeviceCallbackHandler>(devicesCount);
            var amqpConnectionStatuses = new List<AmqpConnectionStatusChange>(devicesCount);
            var operations = new List<Task>(devicesCount);

            do
            {
                totalRuns++;

                // Arrange
                // Initialize the test device client instances
                // Set the device client connection status change handler
                logger.Trace($"{nameof(PoolingOverAmqp)} Initializing Device Clients for multiplexing test - Test run {totalRuns}");
                for (int i = 0; i < devicesCount; i++)
                {
                    // Initialize the test device client instances
                    TestDevice testDevice = await TestDevice.GetTestDeviceAsync(logger, $"{devicePrefix}_{i}_").ConfigureAwait(false);
                    IotHubDeviceClient deviceClient = testDevice.CreateDeviceClient(new IotHubClientOptions(transportSettings), authScope: authScope);

                    // Set the device client connection status change handler
                    var amqpConnectionStatusChange = new AmqpConnectionStatusChange(logger);
                    deviceClient.SetConnectionStatusChangeCallback(amqpConnectionStatusChange.ConnectionStatusChangeHandler);

                    var testDeviceCallbackHandler = new TestDeviceCallbackHandler(deviceClient, testDevice, logger);

                    testDevices.Add(testDevice);
                    deviceClients.Add(deviceClient);
                    testDeviceCallbackHandlers.Add(testDeviceCallbackHandler);
                    amqpConnectionStatuses.Add(amqpConnectionStatusChange);

                    if (initOperation != null)
                    {
                        await initOperation(deviceClient, testDevice, testDeviceCallbackHandler).ConfigureAwait(false);
                    }
                }

                try
                {
                    for (int i = 0; i < devicesCount; i++)
                    {
                        operations.Add(testOperation(deviceClients[i], testDevices[i], testDeviceCallbackHandlers[i]));
                    }
                    await Task.WhenAll(operations).ConfigureAwait(false);
                    operations.Clear();

                    // Close the device client instances and verify the connection status change checks
                    bool deviceConnectionStatusAsExpected = true;
                    for (int i = 0; i < devicesCount; i++)
                    {
                        await deviceClients[i].CloseAsync().ConfigureAwait(false);

                        if (!ignoreConnectionStatus)
                        {
                            // The connection status change count should be 2: connect (open) and disabled (close)
                            if (amqpConnectionStatuses[i].ConnectionStatusChangeHandlerCount != 2)
                            {
                                deviceConnectionStatusAsExpected = false;
                            }

                            // The connection status should be "Disabled", with connection status change reason "ClientClose"
                            deviceClients[i].ConnectionStatusInfo.Status.Should().Be(
                                ConnectionStatus.Closed,
                                $"Reason {deviceClients[i].ConnectionStatusInfo.ChangeReason}");
                            deviceClients[i].ConnectionStatusInfo.ChangeReason.Should().Be(ConnectionStatusChangeReason.ClientClosed);
                        }
                    }
                    if (deviceConnectionStatusAsExpected)
                    {
                        successfulRuns++;
                    }

                    currentSuccessRate = (int)((double)successfulRuns / totalRuns * 100);
                    reRunTest = currentSuccessRate < TestSuccessRate;
                }
                finally
                {
                    // Close the service-side components and dispose the device client instances.
                    if (cleanupOperation != null)
                    {
                        await cleanupOperation().ConfigureAwait(false);
                    }

                    deviceClients.ForEach(deviceClient => deviceClient.Dispose());
                    testDeviceCallbackHandlers.ForEach(testDeviceCallbackHandler => testDeviceCallbackHandler.Dispose());

                    // Clean up the local lists
                    testDevices.Clear();
                    deviceClients.Clear();
                    amqpConnectionStatuses.Clear();
                }
            } while (reRunTest && totalRuns < MaxTestRunCount);

            reRunTest.Should().BeFalse($"Device client instances got disconnected in {totalRuns - successfulRuns} runs out of {totalRuns}; current testSuccessRate = {currentSuccessRate}%.");
        }

        private class AmqpConnectionStatusChange
        {
            private readonly MsTestLogger _logger;

            public AmqpConnectionStatusChange(MsTestLogger logger)
            {
                ConnectionStatusChangeHandlerCount = 0;
                _logger = logger;
            }

            public void ConnectionStatusChangeHandler(ConnectionStatusInfo connectionStatusInfo)
            {
                ConnectionStatusChangeHandlerCount++;
                _logger.Trace($"{nameof(PoolingOverAmqp)}.{nameof(ConnectionStatusChangeHandler)}: status={connectionStatusInfo.Status} statusChangeReason={connectionStatusInfo.ChangeReason} count={ConnectionStatusChangeHandlerCount}");
            }

            public int ConnectionStatusChangeHandlerCount { get; set; }
        }
    }
}
