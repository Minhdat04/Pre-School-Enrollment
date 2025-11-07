using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using PreschoolEnrollmentSystem.Services.Interfaces;

namespace PreschoolEnrollmentSystem.Services.Implementation
{
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        private readonly ILogger<FirebaseNotificationService> _logger;
        private readonly FirebaseMessaging _messaging;

        public FirebaseNotificationService(ILogger<FirebaseNotificationService> logger)
        {
            _logger = logger;
            _messaging = FirebaseMessaging.DefaultInstance;
        }

        public async Task<string> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceToken))
                {
                    throw new ArgumentException("Device token cannot be null or empty", nameof(deviceToken));
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    throw new ArgumentException("Title cannot be null or empty", nameof(title));
                }

                _logger.LogInformation("Sending notification to device: {DeviceToken}", MaskToken(deviceToken));

                var message = new Message
                {
                    Token = deviceToken,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data,
                    Android = new AndroidConfig
                    {
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ChannelId = "default"
                        }
                    },
                    Apns = new ApnsConfig
                    {
                        Aps = new Aps
                        {
                            Sound = "default"
                        }
                    }
                };

                var messageId = await _messaging.SendAsync(message);
                _logger.LogInformation("Notification sent successfully. Message ID: {MessageId}", messageId);

                return messageId;
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, "Firebase Messaging error sending notification to {DeviceToken}: {ErrorCode}",
                    MaskToken(deviceToken), ex.MessagingErrorCode);
                throw new InvalidOperationException($"Failed to send notification: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to device {DeviceToken}", MaskToken(deviceToken));
                throw;
            }
        }

        public async Task<BatchNotificationResult> SendToMultipleAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null)
        {
            try
            {
                if (deviceTokens == null || !deviceTokens.Any())
                {
                    throw new ArgumentException("Device tokens list cannot be null or empty", nameof(deviceTokens));
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    throw new ArgumentException("Title cannot be null or empty", nameof(title));
                }

                _logger.LogInformation("Sending notification to {Count} devices", deviceTokens.Count);

                var message = new MulticastMessage
                {
                    Tokens = deviceTokens,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data,
                    Android = new AndroidConfig
                    {
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ChannelId = "default"
                        }
                    },
                    Apns = new ApnsConfig
                    {
                        Aps = new Aps
                        {
                            Sound = "default"
                        }
                    }
                };

                var response = await _messaging.SendEachForMulticastAsync(message);

                var result = new BatchNotificationResult
                {
                    SuccessCount = response.SuccessCount,
                    FailureCount = response.FailureCount
                };

                // Collect failed tokens and error messages
                for (int i = 0; i < response.Responses.Count; i++)
                {
                    if (!response.Responses[i].IsSuccess)
                    {
                        result.FailedTokens.Add(deviceTokens[i]);
                        result.ErrorMessages.Add(response.Responses[i].Exception?.Message ?? "Unknown error");
                    }
                }

                _logger.LogInformation(
                    "Batch notification completed. Success: {SuccessCount}, Failures: {FailureCount}",
                    result.SuccessCount, result.FailureCount);

                if (result.FailureCount > 0)
                {
                    _logger.LogWarning("Failed to send to {Count} devices", result.FailureCount);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending batch notification to {Count} devices", deviceTokens?.Count ?? 0);
                throw;
            }
        }

        public async Task<string> SendToTopicAsync(string topic, string title, string body, Dictionary<string, string>? data = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(topic))
                {
                    throw new ArgumentException("Topic cannot be null or empty", nameof(topic));
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    throw new ArgumentException("Title cannot be null or empty", nameof(title));
                }

                _logger.LogInformation("Sending notification to topic: {Topic}", topic);

                var message = new Message
                {
                    Topic = topic,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data,
                    Android = new AndroidConfig
                    {
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ChannelId = "default"
                        }
                    },
                    Apns = new ApnsConfig
                    {
                        Aps = new Aps
                        {
                            Sound = "default"
                        }
                    }
                };

                var messageId = await _messaging.SendAsync(message);
                _logger.LogInformation("Notification sent to topic {Topic}. Message ID: {MessageId}", topic, messageId);

                return messageId;
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, "Firebase Messaging error sending to topic {Topic}: {ErrorCode}",
                    topic, ex.MessagingErrorCode);
                throw new InvalidOperationException($"Failed to send notification to topic: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to topic {Topic}", topic);
                throw;
            }
        }

        public async Task<bool> SubscribeToTopicAsync(string deviceToken, string topic)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceToken))
                {
                    throw new ArgumentException("Device token cannot be null or empty", nameof(deviceToken));
                }

                if (string.IsNullOrWhiteSpace(topic))
                {
                    throw new ArgumentException("Topic cannot be null or empty", nameof(topic));
                }

                _logger.LogInformation("Subscribing device to topic: {Topic}", topic);

                var response = await _messaging.SubscribeToTopicAsync(new[] { deviceToken }, topic);

                if (response.FailureCount > 0)
                {
                    _logger.LogWarning("Failed to subscribe device to topic {Topic}", topic);
                    return false;
                }

                _logger.LogInformation("Device successfully subscribed to topic: {Topic}", topic);
                return true;
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, "Firebase Messaging error subscribing to topic {Topic}: {ErrorCode}",
                    topic, ex.MessagingErrorCode);
                throw new InvalidOperationException($"Failed to subscribe to topic: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing device to topic {Topic}", topic);
                throw;
            }
        }

        public async Task<bool> SubscribeToTopicAsync(List<string> deviceTokens, string topic)
        {
            try
            {
                if (deviceTokens == null || !deviceTokens.Any())
                {
                    throw new ArgumentException("Device tokens list cannot be null or empty", nameof(deviceTokens));
                }

                if (string.IsNullOrWhiteSpace(topic))
                {
                    throw new ArgumentException("Topic cannot be null or empty", nameof(topic));
                }

                _logger.LogInformation("Subscribing {Count} devices to topic: {Topic}", deviceTokens.Count, topic);

                var response = await _messaging.SubscribeToTopicAsync(deviceTokens, topic);

                _logger.LogInformation(
                    "Topic subscription completed. Success: {SuccessCount}, Failures: {FailureCount}",
                    response.SuccessCount, response.FailureCount);

                return response.FailureCount == 0;
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, "Firebase Messaging error subscribing devices to topic {Topic}: {ErrorCode}",
                    topic, ex.MessagingErrorCode);
                throw new InvalidOperationException($"Failed to subscribe devices to topic: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing devices to topic {Topic}", topic);
                throw;
            }
        }

        public async Task<bool> UnsubscribeFromTopicAsync(string deviceToken, string topic)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceToken))
                {
                    throw new ArgumentException("Device token cannot be null or empty", nameof(deviceToken));
                }

                if (string.IsNullOrWhiteSpace(topic))
                {
                    throw new ArgumentException("Topic cannot be null or empty", nameof(topic));
                }

                _logger.LogInformation("Unsubscribing device from topic: {Topic}", topic);

                var response = await _messaging.UnsubscribeFromTopicAsync(new[] { deviceToken }, topic);

                if (response.FailureCount > 0)
                {
                    _logger.LogWarning("Failed to unsubscribe device from topic {Topic}", topic);
                    return false;
                }

                _logger.LogInformation("Device successfully unsubscribed from topic: {Topic}", topic);
                return true;
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, "Firebase Messaging error unsubscribing from topic {Topic}: {ErrorCode}",
                    topic, ex.MessagingErrorCode);
                throw new InvalidOperationException($"Failed to unsubscribe from topic: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing device from topic {Topic}", topic);
                throw;
            }
        }

        public async Task<bool> UnsubscribeFromTopicAsync(List<string> deviceTokens, string topic)
        {
            try
            {
                if (deviceTokens == null || !deviceTokens.Any())
                {
                    throw new ArgumentException("Device tokens list cannot be null or empty", nameof(deviceTokens));
                }

                if (string.IsNullOrWhiteSpace(topic))
                {
                    throw new ArgumentException("Topic cannot be null or empty", nameof(topic));
                }

                _logger.LogInformation("Unsubscribing {Count} devices from topic: {Topic}", deviceTokens.Count, topic);

                var response = await _messaging.UnsubscribeFromTopicAsync(deviceTokens, topic);

                _logger.LogInformation(
                    "Topic unsubscription completed. Success: {SuccessCount}, Failures: {FailureCount}",
                    response.SuccessCount, response.FailureCount);

                return response.FailureCount == 0;
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, "Firebase Messaging error unsubscribing devices from topic {Topic}: {ErrorCode}",
                    topic, ex.MessagingErrorCode);
                throw new InvalidOperationException($"Failed to unsubscribe devices from topic: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing devices from topic {Topic}", topic);
                throw;
            }
        }

        public async Task<string> SendUrgentNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceToken))
                {
                    throw new ArgumentException("Device token cannot be null or empty", nameof(deviceToken));
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    throw new ArgumentException("Title cannot be null or empty", nameof(title));
                }

                _logger.LogInformation("Sending urgent notification to device: {DeviceToken}", MaskToken(deviceToken));

                var message = new Message
                {
                    Token = deviceToken,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data,
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ChannelId = "urgent",
                            Priority = NotificationPriority.MAX
                        }
                    },
                    Apns = new ApnsConfig
                    {
                        Headers = new Dictionary<string, string>
                        {
                            { "apns-priority", "10" }
                        },
                        Aps = new Aps
                        {
                            Sound = "default",
                            ContentAvailable = true
                        }
                    }
                };

                var messageId = await _messaging.SendAsync(message);
                _logger.LogInformation("Urgent notification sent successfully. Message ID: {MessageId}", messageId);

                return messageId;
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, "Firebase Messaging error sending urgent notification to {DeviceToken}: {ErrorCode}",
                    MaskToken(deviceToken), ex.MessagingErrorCode);
                throw new InvalidOperationException($"Failed to send urgent notification: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending urgent notification to device {DeviceToken}", MaskToken(deviceToken));
                throw;
            }
        }

        public async Task<string> SendDataMessageAsync(string deviceToken, Dictionary<string, string> data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceToken))
                {
                    throw new ArgumentException("Device token cannot be null or empty", nameof(deviceToken));
                }

                if (data == null || !data.Any())
                {
                    throw new ArgumentException("Data cannot be null or empty", nameof(data));
                }

                _logger.LogInformation("Sending data message to device: {DeviceToken}", MaskToken(deviceToken));

                var message = new Message
                {
                    Token = deviceToken,
                    Data = data,
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High
                    },
                    Apns = new ApnsConfig
                    {
                        Headers = new Dictionary<string, string>
                        {
                            { "apns-priority", "5" }
                        },
                        Aps = new Aps
                        {
                            ContentAvailable = true
                        }
                    }
                };

                var messageId = await _messaging.SendAsync(message);
                _logger.LogInformation("Data message sent successfully. Message ID: {MessageId}", messageId);

                return messageId;
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, "Firebase Messaging error sending data message to {DeviceToken}: {ErrorCode}",
                    MaskToken(deviceToken), ex.MessagingErrorCode);
                throw new InvalidOperationException($"Failed to send data message: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending data message to device {DeviceToken}", MaskToken(deviceToken));
                throw;
            }
        }

        /// <summary>
        /// Masks the device token for logging purposes (shows first 8 and last 4 characters)
        /// </summary>
        private string MaskToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length < 12)
                return "***";

            return $"{token[..8]}...{token[^4..]}";
        }
    }
}
