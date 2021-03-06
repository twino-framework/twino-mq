using System;
using System.Threading.Tasks;
using Horse.Mq.Clients;
using Horse.Mq.Delivery;
using Horse.Mq.Queues;
using Horse.Protocols.Hmq;

namespace Horse.Mq.Handlers
{
    /// <summary>
    /// Quick IMessageDeliveryHandler implementation.
    /// Allows all operations, does not keep and does not send acknowledge message to producer
    /// </summary>
    public class JustAllowDeliveryHandler : IMessageDeliveryHandler
    {
        /// <summary>
        /// Decision: Allow
        /// </summary>
        public async Task<Decision> ReceivedFromProducer(HorseQueue queue, QueueMessage message, MqClient sender)
        {
            return await Task.FromResult(new Decision(true, false));
        }

        /// <summary>
        /// Decision: Allow
        /// </summary>
        public async Task<Decision> BeginSend(HorseQueue queue, QueueMessage message)
        {
            return await Task.FromResult(new Decision(true, false));
        }

        /// <summary>
        /// Decision: Allow
        /// </summary>
        public async Task<Decision> CanConsumerReceive(HorseQueue queue, QueueMessage message, MqClient receiver)
        {
            return await Task.FromResult(new Decision(true, false));
        }

        /// <summary>
        /// Decision: Allow
        /// </summary>
        public async Task<Decision> ConsumerReceived(HorseQueue queue, MessageDelivery delivery, MqClient receiver)
        {
            return await Task.FromResult(new Decision(true, false));
        }

        /// <summary>
        /// Decision: Allow
        /// </summary>
        public async Task<Decision> ConsumerReceiveFailed(HorseQueue queue, MessageDelivery delivery, MqClient receiver)
        {
            return await Task.FromResult(new Decision(true, false));
        }

        /// <summary>
        /// Decision: Allow
        /// </summary>
        public async Task<Decision> EndSend(HorseQueue queue, QueueMessage message)
        {
            return await Task.FromResult(new Decision(true, false));
        }

        /// <summary>
        /// Decision: Allow
        /// </summary>
        public async Task<Decision> AcknowledgeReceived(HorseQueue queue, HorseMessage acknowledgeMessage, MessageDelivery delivery, bool success)
        {
            return await Task.FromResult(new Decision(true, false));
        }

        /// <summary>
        /// Decision: Allow
        /// </summary>
        public async Task<Decision> MessageTimedOut(HorseQueue queue, QueueMessage message)
        {
            return await Task.FromResult(new Decision(true, false));
        }

        /// <summary>
        /// Decision: Allow
        /// </summary>
        public async Task<Decision> AcknowledgeTimedOut(HorseQueue queue, MessageDelivery delivery)
        {
            return await Task.FromResult(new Decision(true, false));
        }

        /// <summary>
        /// Does nothing in this implementation
        /// </summary>
        public async Task MessageDequeued(HorseQueue queue, QueueMessage message)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Decision: Allow
        /// </summary>
        public async Task<Decision> ExceptionThrown(HorseQueue queue, QueueMessage message, Exception exception)
        {
            return await Task.FromResult(new Decision(true, false));
        }

        /// <summary>
        /// Does nothing for this implementation and returns false
        /// </summary>
        public async Task<bool> SaveMessage(HorseQueue queue, QueueMessage message)
        {
            return await Task.FromResult(false);
        }
    }
}