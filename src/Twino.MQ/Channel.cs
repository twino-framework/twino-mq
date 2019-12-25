using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Twino.MQ.Clients;
using Twino.MQ.Helpers;
using Twino.MQ.Options;
using Twino.MQ.Queues;
using Twino.MQ.Security;

namespace Twino.MQ
{
    /// <summary>
    /// Messaging Queue Channel
    /// </summary>
    public class Channel
    {
        #region Properties

        /// <summary>
        /// Unique channel name (not case-sensetive)
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Server of the channel
        /// </summary>
        public MqServer Server { get; }

        /// <summary>
        /// Channel options
        /// </summary>
        public ChannelOptions Options { get; }

        /// <summary>
        /// Channel authenticator.
        /// If null, server's default channel authenticator will be used.
        /// </summary>
        public IChannelAuthenticator Authenticator { get; }

        private readonly SafeList<QueueId> _allowedQueues = new SafeList<QueueId>(32);

        /// <summary>
        /// Allowed queue id list in this channel
        /// </summary>
        public IEnumerable<QueueId> AllowedQueues => _allowedQueues.GetUnsafeList();

        /// <summary>
        /// Channel event handler
        /// </summary>
        public IChannelEventHandler EventHandler { get; }

        /// <summary>
        /// Channel messaging delivery handler.
        /// If queue does not have it's own delivery handler, this one is used.
        /// </summary>
        public IMessageDeliveryHandler DeliveryHandler { get; }

        /// <summary>
        /// Active channel queues
        /// </summary>
        public IEnumerable<ChannelQueue> Queues => _queues.GetUnsafeList();

        /// <summary>
        /// Clone list of active channel queues
        /// </summary>
        public IEnumerable<ChannelQueue> QueuesClone => _queues.GetAsClone();

        private readonly SafeList<ChannelQueue> _queues;

        /// <summary>
        /// Clients in the channel as thread-unsafe list
        /// </summary>
        public IEnumerable<ChannelClient> ClientsUnsafe => _clients.GetUnsafeList();

        /// <summary>
        /// Clients in the channel as cloned list
        /// </summary>
        public List<ChannelClient> ClientsClone => _clients.GetAsClone();

        private readonly SafeList<ChannelClient> _clients;

        #endregion

        #region Constructors

        internal Channel(MqServer server,
                         ChannelOptions options,
                         string name,
                         IChannelAuthenticator authenticator,
                         IChannelEventHandler eventHandler,
                         IMessageDeliveryHandler deliveryHandler)
        {
            Server = server;
            Options = options;
            Name = name;

            Authenticator = authenticator;
            EventHandler = eventHandler;
            DeliveryHandler = deliveryHandler;

            _queues = new SafeList<ChannelQueue>(8);
            _clients = new SafeList<ChannelClient>(256);
        }

        /// <summary>
        /// Destroys the channel, clears all queues and clients 
        /// </summary>
        public void Destroy()
        {
            _clients.Clear();
            _queues.Clear();
        }

        #endregion

        #region Queue Actions

        /// <summary>
        /// Finds queue by content type
        /// </summary>
        public ChannelQueue FindQueue(ushort queueId)
        {
            return _queues.Find(x => x.Id == queueId);
        }

        /// <summary>
        /// Creates new queue in the channel with default options and default handlers
        /// </summary>
        public async Task<ChannelQueue> CreateQueue(ushort queueId)
        {
            ChannelQueueOptions options = ChannelQueueOptions.CloneFrom(Options);
            return await CreateQueue(queueId,
                                     options,
                                     Server.DefaultDeliveryHandler);
        }

        /// <summary>
        /// Creates new queue in the channel with default handlers
        /// </summary>
        public async Task<ChannelQueue> CreateQueue(ushort queueId, Action<ChannelQueueOptions> optionsAction)
        {
            ChannelQueueOptions options = ChannelQueueOptions.CloneFrom(Options);
            optionsAction(options);
            return await CreateQueue(queueId, options);
        }

        /// <summary>
        /// Creates new queue in the channel with default handlers
        /// </summary>
        public async Task<ChannelQueue> CreateQueue(ushort queueId, ChannelQueueOptions options)
        {
            if (DeliveryHandler == null)
                throw new NoNullAllowedException("There is no default delivery handler defined for the channel. Queue must have it's own delivery handler.");

            return await CreateQueue(queueId,
                                     options,
                                     Server.DefaultDeliveryHandler);
        }

        /// <summary>
        /// Creates new queue in the channel
        /// </summary>
        public async Task<ChannelQueue> CreateQueue(ushort queueId,
                                                    ChannelQueueOptions options,
                                                    IMessageDeliveryHandler deliveryHandler)
        {
            if (deliveryHandler == null)
                throw new NoNullAllowedException("Delivery handler cannot be null.");

            //multiple queues are not allowed
            if (!Options.AllowMultipleQueues && _queues.Count > 0)
                return null;

            //if content type is not allowed for this channel, return null
            if (Options.AllowedQueues != null && Options.AllowedQueues.Length > 0)
                if (!Options.AllowedQueues.Contains(queueId))
                    return null;

            ChannelQueue queue = _queues.Find(x => x.Id == queueId);

            if (queue != null)
                throw new DuplicateNameException($"The channel has already a queue with same content type: {queueId}");

            queue = new ChannelQueue(this, queueId, options, deliveryHandler);
            _queues.Add(queue);

            if (EventHandler != null)
                await EventHandler.OnQueueCreated(queue, this);

            return queue;
        }

        /// <summary>
        /// Removes a queue from the channel
        /// </summary>
        public async Task RemoveQueue(ChannelQueue queue)
        {
            _queues.Remove(queue);
            await queue.SetStatus(QueueStatus.Stopped);

            if (EventHandler != null)
                await EventHandler.OnQueueRemoved(queue, this);

            await queue.Destroy();
        }

        #endregion

        #region Client Actions

        /// <summary>
        /// Adds the client to the channel
        /// </summary>
        public async Task<bool> AddClient(MqClient client)
        {
            if (Authenticator != null)
            {
                bool allowed = await Authenticator.Authenticate(this, client);
                if (!allowed)
                    return false;
            }

            ChannelClient cc = new ChannelClient(this, client);
            _clients.Add(cc);
            client.Join(cc);

            if (EventHandler != null)
                await EventHandler.OnClientJoined(cc);

            IEnumerable<ChannelQueue> list = _queues.GetAsClone();
            foreach (ChannelQueue queue in list)
                await queue.Trigger();

            return true;
        }

        /// <summary>
        /// Removes client from the channel
        /// </summary>
        public async Task RemoveClient(ChannelClient client)
        {
            _clients.Remove(client);
            client.Client.Leave(client);

            if (EventHandler != null)
                await EventHandler.OnClientLeft(client);
        }

        /// <summary>
        /// Removes client from the channel
        /// </summary>
        public async Task<bool> RemoveClient(MqClient client)
        {
            ChannelClient cc = _clients.FindAndRemove(x => x.Client == client);

            if (cc == null)
                return false;

            client.Leave(cc);

            if (EventHandler != null)
                await EventHandler.OnClientLeft(cc);

            return true;
        }

        /// <summary>
        /// Finds client in the channel
        /// </summary>
        public ChannelClient FindClient(MqClient client)
        {
            return _clients.Find(x => x.Client == client);
        }

        /// <summary>
        /// Gets next client with round robin algorithm and updates index
        /// </summary>
        internal ChannelClient GetNextRRClient(ref int index)
        {
            List<ChannelClient> clients = _clients.GetAsClone();
            if (index < 0 || index + 1 >= clients.Count)
            {
                if (clients.Count == 0)
                    return null;

                index = 0;
                return clients[0];
            }

            index++;
            return clients[index];
        }

        #endregion
    }
}