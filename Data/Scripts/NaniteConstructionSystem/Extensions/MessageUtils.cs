using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace Ntech.Extensions
{
    public enum MessageSide
    {
        ServerSide,
        ClientSide
    }

    public static class MessageUtils
    {
        public static List<byte> Client_MessageCache = new List<byte>();
        public static Dictionary<ulong, List<byte>> Server_MessageCache = new Dictionary<ulong, List<byte>>();

        public static readonly ushort MessageId = 8956;

        public static void SendMessageToServer(MessageBase message)
        {
            message.Side = MessageSide.ServerSide;
            if (MyAPIGateway.Session.Player != null)
                message.SenderSteamId = MyAPIGateway.Session.Player.SteamUserId;
            var byteData = MyAPIGateway.Utilities.SerializeToBinary<MessageBase>(message);
            NaniteConstructionSystem.Logging.Instance.WriteLine(string.Format("SendMessageToServer {0} {1} {2}, {3}b", message.SenderSteamId, message.Side, message.GetType().Name, byteData.Length));
            MyAPIGateway.Multiplayer.SendMessageToServer(MessageId, byteData);
        }

        /// <summary>
        /// Creates and sends an entity with the given information for the server and all players.
        /// </summary>
        /// <param name="content"></param>
        public static void SendMessageToAll(MessageBase message, bool syncAll = true)
        {
            if (MyAPIGateway.Session.Player != null)
                message.SenderSteamId = MyAPIGateway.Session.Player.SteamUserId;

            if (syncAll || !MyAPIGateway.Multiplayer.IsServer)
                SendMessageToServer(message);
            SendMessageToAllPlayers(message);
        }

        public static void SendMessageToAllPlayers(MessageBase messageContainer)
        {
            //MyAPIGateway.Multiplayer.SendMessageToOthers(StandardClientId, System.Text.Encoding.Unicode.GetBytes(ConvertData(content))); <- does not work as expected ... so it doesn't work at all?
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p != null && !MyAPIGateway.Multiplayer.IsServerPlayer(p.Client));
            foreach (IMyPlayer player in players)
                SendMessageToPlayer(player.SteamUserId, messageContainer);
        }

        public static void SendMessageToPlayer(ulong steamId, MessageBase message)
        {
            message.Side = MessageSide.ClientSide;
            var byteData = MyAPIGateway.Utilities.SerializeToBinary(message);

            NaniteConstructionSystem.Logging.Instance.WriteLine(string.Format("SendMessageToPlayer {0} {1} {2}, {3}b", steamId, message.Side, message.GetType().Name, byteData.Length));

            MyAPIGateway.Multiplayer.SendMessageTo(MessageId, byteData, steamId);
        }

        public static void HandleMessage(byte[] data)
        {
            try
            {
                var message = MyAPIGateway.Utilities.SerializeFromBinary<MessageBase>(data);

                NaniteConstructionSystem.Logging.Instance.WriteLine("HandleMessage()");
                if (message != null)
                {
                    NaniteConstructionSystem.Logging.Instance.WriteLine(string.Format("HandleMessage() {0} {1} {2}, {3}b", message.SenderSteamId, message.Side, message.GetType().Name, data.Length));
                    message.InvokeProcessing();
                }
                return;
            }
            catch (Exception e)
            {
                // Don't warn the user of an exception, this can happen if two mods with the same message id receive an unknown message
                NaniteConstructionSystem.Logging.Instance.WriteLine(string.Format("Processing message exception. Exception: {0}", e.ToString()));
                //Logger.Instance.LogException(e);
            }

        }
    }

    [ProtoContract, ProtoInclude(5001, typeof(Nanite.MessageClientConnected)), ProtoInclude(5002, typeof(Nanite.MessageConfig))]
    public abstract class MessageBase
    {
        /// <summary>
        /// The SteamId of the message's sender. Note that this will be set when the message is sent, so there is no need for setting it otherwise.
        /// </summary>
        [ProtoMember(1)]
        public ulong SenderSteamId;

        /// <summary>
        /// Defines on which side the message should be processed. Note that this will be set when the message is sent, so there is no need for setting it otherwise.
        /// </summary>
        [ProtoMember(2, IsRequired = true)]
        public MessageSide Side = MessageSide.ClientSide;

        /// <summary>
        /// Name of mod. Used to determine if message belongs to us.
        /// </summary>
        [ProtoMember(3)]
        public string ModName = "Foo";

        public void InvokeProcessing()
        {
            if (ModName != "Foo")
            {
                NaniteConstructionSystem.Logging.Instance.WriteLine("Message came from another mod (" + ModName + "), ignored.");
                return;
            }

            switch (Side)
            {
                case MessageSide.ClientSide:
                    InvokeClientProcessing();
                    break;
                case MessageSide.ServerSide:
                    InvokeServerProcessing();
                    break;
            }
        }

        private void InvokeClientProcessing()
        {
            NaniteConstructionSystem.Logging.Instance.WriteLine(string.Format("START - Processing [Client] {0}", this.GetType().Name));
            try
            {
                ProcessClient();
            }
            catch (Exception ex)
            {
                NaniteConstructionSystem.Logging.Instance.WriteLine(ex.ToString());
            }
            NaniteConstructionSystem.Logging.Instance.WriteLine(string.Format("END - Processing [Client] {0}", this.GetType().Name));
        }

        private void InvokeServerProcessing()
        {
            NaniteConstructionSystem.Logging.Instance.WriteLine(string.Format("START - Processing [Server] {0}", this.GetType().Name));

            try
            {
                ProcessServer();
            }
            catch (Exception ex)
            {
                NaniteConstructionSystem.Logging.Instance.WriteLine(ex.ToString());
            }

            NaniteConstructionSystem.Logging.Instance.WriteLine(string.Format("END - Processing [Server] {0}", this.GetType().Name));
        }

        public abstract void ProcessClient();
        public abstract void ProcessServer();
    }
}
