using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Services.Interfaces
{
    /// <summary>
    /// Firebase Cloud Messaging (FCM) service for sending push notifications
    /// </summary>
    public interface IFirebaseNotificationService
    {
        /// <summary>
        /// Sends a notification to a single device
        /// </summary>
        /// <param name="deviceToken">FCM device registration token</param>
        /// <param name="title">Notification title</param>
        /// <param name="body">Notification body</param>
        /// <param name="data">Optional custom data payload</param>
        /// <returns>Message ID if successful</returns>
        Task<string> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null);

        /// <summary>
        /// Sends a notification to multiple devices
        /// </summary>
        /// <param name="deviceTokens">List of FCM device registration tokens</param>
        /// <param name="title">Notification title</param>
        /// <param name="body">Notification body</param>
        /// <param name="data">Optional custom data payload</param>
        /// <returns>Batch response with success/failure counts</returns>
        Task<BatchNotificationResult> SendToMultipleAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null);

        /// <summary>
        /// Sends a notification to all devices subscribed to a topic
        /// </summary>
        /// <param name="topic">Topic name (e.g., "all-parents", "urgent-announcements")</param>
        /// <param name="title">Notification title</param>
        /// <param name="body">Notification body</param>
        /// <param name="data">Optional custom data payload</param>
        /// <returns>Message ID if successful</returns>
        Task<string> SendToTopicAsync(string topic, string title, string body, Dictionary<string, string>? data = null);

        /// <summary>
        /// Subscribes a device to a topic
        /// </summary>
        /// <param name="deviceToken">FCM device registration token</param>
        /// <param name="topic">Topic name</param>
        /// <returns>True if successful</returns>
        Task<bool> SubscribeToTopicAsync(string deviceToken, string topic);

        /// <summary>
        /// Subscribes multiple devices to a topic
        /// </summary>
        /// <param name="deviceTokens">List of FCM device registration tokens</param>
        /// <param name="topic">Topic name</param>
        /// <returns>True if successful</returns>
        Task<bool> SubscribeToTopicAsync(List<string> deviceTokens, string topic);

        /// <summary>
        /// Unsubscribes a device from a topic
        /// </summary>
        /// <param name="deviceToken">FCM device registration token</param>
        /// <param name="topic">Topic name</param>
        /// <returns>True if successful</returns>
        Task<bool> UnsubscribeFromTopicAsync(string deviceToken, string topic);

        /// <summary>
        /// Unsubscribes multiple devices from a topic
        /// </summary>
        /// <param name="deviceTokens">List of FCM device registration tokens</param>
        /// <param name="topic">Topic name</param>
        /// <returns>True if successful</returns>
        Task<bool> UnsubscribeFromTopicAsync(List<string> deviceTokens, string topic);

        /// <summary>
        /// Sends a notification with high priority (for time-sensitive alerts)
        /// </summary>
        /// <param name="deviceToken">FCM device registration token</param>
        /// <param name="title">Notification title</param>
        /// <param name="body">Notification body</param>
        /// <param name="data">Optional custom data payload</param>
        /// <returns>Message ID if successful</returns>
        Task<string> SendUrgentNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null);

        /// <summary>
        /// Sends a data-only message (no notification, app handles it in background)
        /// </summary>
        /// <param name="deviceToken">FCM device registration token</param>
        /// <param name="data">Custom data payload</param>
        /// <returns>Message ID if successful</returns>
        Task<string> SendDataMessageAsync(string deviceToken, Dictionary<string, string> data);
    }

    /// <summary>
    /// Result of sending notifications to multiple devices
    /// </summary>
    public class BatchNotificationResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> FailedTokens { get; set; } = new();
        public List<string> ErrorMessages { get; set; } = new();
    }
}
