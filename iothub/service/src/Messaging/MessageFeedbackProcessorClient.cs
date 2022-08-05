﻿using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Subclient of <see cref="IotHubServiceClient"/> for handling cloud-to-device message feedback.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#message-feedback"/>.
    public class MessageFeedbackProcessorClient : IDisposable
    {
        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly IotHubConnection _connection;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected MessageFeedbackProcessorClient()
        {
        }

        internal MessageFeedbackProcessorClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            IotHubServiceClientOptions options)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _connection = new IotHubConnection(credentialProvider, options.UseWebSocketOnly, options.TransportSettings, options);
            FeedbackReceiver = new AmqpFeedbackReceiver(_connection);
        }

        /// <summary>
        /// Gets the AmqpFeedbackReceiver which can deliver acknowledgments for messages sent to a device/module from IoT hub.
        /// </summary>
        internal AmqpFeedbackReceiver FeedbackReceiver { get; }

        /// <summary>
        /// Open the FeedbackReceiver instance.
        /// </summary>
        public virtual async Task OpenAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Opening FeedbackReceiver", nameof(OpenAsync));

            try
            {
                await FeedbackReceiver.OpenAsync().ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(OpenAsync)} threw an exception: {ex}", nameof(OpenAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Opening FeedbackReceiver", nameof(OpenAsync));
            }
        }

        /// <summary>
        /// Close the FeedbackReceiver instance.
        /// </summary>
        public virtual async Task CloseAsync()
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Closing FeedbackReceiver", nameof(CloseAsync));

            try
            {
                await FeedbackReceiver.CloseAsync().ConfigureAwait(false);
                await _connection.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(CloseAsync)} threw an exception: {ex}", nameof(CloseAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Closing FeedbackReceiver", nameof(CloseAsync));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the FeedbackReceiver instance.
        /// </summary>
        public virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, $"Disposing FeedbackReceiver", nameof(Dispose));

                FeedbackReceiver.Dispose();

                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Disposing FeedbackReceiver", nameof(Dispose));
            }
        }
    }
}