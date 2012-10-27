﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Practices.Prism.Events;
using Models;
using slimCat;
using WebSocket4Net;

namespace Services
{
    /// <summary>
    /// Maintains the connection to F-Chat's server. Used to send/receive commands.
    /// </summary>
    public class ChatConnection : IChatConnection, IDisposable
    {
        #region Constants
        public const string host = "ws://chat.f-list.net:9722/";
        public const string version = "b3.xx prototype";
        public const string client_n = "SlimCat";
        #endregion

        #region Fields
        private readonly IAccount _account;
        private readonly IEventAggregator _events;
        private string _selectedCharacter;
        private WebSocket _ws;
        private StreamWriter _logger;
        #endregion

        #region Properties
        public IAccount Account { get { return _account; } }
        public string Character { get { return _selectedCharacter; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Chat connection is used to communicate with F-Chat using websockets.
        /// </summary>
        public ChatConnection(IAccount user, IEventAggregator eventagg)
        {
            _account = user;
            _events = eventagg;

            _events.GetEvent<CharacterSelectedLoginEvent>().Subscribe(ConnectToChat, ThreadOption.BackgroundThread, true);
            if (!System.IO.Directory.Exists(@"Debug"))
                System.IO.Directory.CreateDirectory("Debug");

            _logger = new StreamWriter(@"Debug\Rawchat "+System.DateTime.Now.Ticks+".log", true);
        }
        #endregion

        #region Connection Management
        /// <summary>
        /// When the user has picked a character and is ready to connect.
        /// </summary>
        /// <param name="character">Character to connect with</param>
        private void ConnectToChat(string character)
        {
            try
            {
                if (character == null) throw new ArgumentNullException("Provided Character Name");
                if (_account == null) throw new ArgumentNullException("Account Reference");

                _events.GetEvent<CharacterSelectedLoginEvent>().Unsubscribe(ConnectToChat);

                _selectedCharacter = character;
                _ws = new WebSocket(host);

                //define socket behavior
                _ws.Opened += ConnectionOpened;
                _ws.Error += ConnectionError;
                _ws.MessageReceived += ConnectionMessageReceived;
                _ws.Closed += ConnectionClosed;

                // start connection
                _ws.Open();
            }

            catch (Exception ex)
            {
                ex.Source = "F-Chat Connection Service, init";
                Exceptions.HandleException(ex);
            }
        }

        /// <summary>
        /// When our connection was closed.
        /// </summary>
        private void ConnectionClosed(object s, EventArgs e)
        {
            Exceptions.HandleException(new Exception("Connection to the server was closed"),
                "The connection to the server was closed.\n\nApplication will now exit.");

            _logger.Close();
        }

        /// <summary>
        /// When we got something from the server!
        /// </summary>
        void ConnectionMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            string command_type = e.Message.Substring(0, 3); // type of command sent

            string message = e.Message; // actual arguments sent

            if (e.Message.Length > 3) // if it has arguments...
            {
                message = message.Remove(0, 4); // chop off the command type

                var json = (IDictionary<string, object>)SimpleJson.SimpleJson.DeserializeObject(message);
                // de-serialize it to an object model

                json.Add("command", command_type);
                // add back in the command type so our models can listen for them

                // for debug, write the command received to file
                _logger.WriteLine("<<- Command: {0}", json["command"]);

                foreach (KeyValuePair<string, object> pair in json)
                {
                    if (pair.Key != "command")
                        _logger.WriteLine("{0}: {1}", pair.Key, pair.Value);
                }

                _logger.WriteLine();
                _logger.Flush();
                //

                _events.GetEvent<ChatCommandEvent>().Publish(json);
            }

            else if (e.Message == "PIN")
                this.SendMessage("PIN");

            else if (e.Message == "LRP") { }

            else
            {
                // some other, odd, no argument command not specified
                _logger.WriteLine("Server sent unknown command: " + e.Message);
                _logger.WriteLine();
                _logger.Flush();
            }
        }

        /// <summary>
        /// When something done goofed itself
        /// </summary>
        private void ConnectionError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Exceptions.HandleException(e.Exception);
        }

        /// <summary>
        /// When we have connection to F-chat.
        /// </summary>
        private void ConnectionOpened(object sender, EventArgs e)
        {
            // Handshake completed, send login command
            object idn = new { ticket = _account.Ticket, method = "ticket", account = _account.AccountName,
                                    character = _selectedCharacter, cname = client_n, cversion = version };

            this.SendMessage(idn, "IDN");
        }
        #endregion

        #region Methods
        /// <summary>
        /// Sends a message to the server
        /// </summary>
        /// <param name="command">non-serialized data to be sent</param>
        public void SendMessage(object command, string command_type)
        {
            try
            {
                if (command_type.Length > 3 || command_type.Length < 3)
                    throw new ArgumentOutOfRangeException("command_type", "Command type must be 3 characters long");

                string ser = SimpleJson.SimpleJson.SerializeObject(command);

                // debug information
                _logger.WriteLine("->> Command: " + command_type);
                _logger.WriteLine("Data: " + ser);
                _logger.WriteLine();
                _logger.Flush();
                //

                _ws.Send(command_type + " " + ser);
            }

            catch (Exception ex)
            {
                ex.Source = "F-Chat connection, SendMessage method";
                Exceptions.HandleException(ex);
            }
        }

        public void SendMessage(IDictionary<string, object> command)
        {
            try
            {
                string type = command["type"] as string;

                command.Remove("type");

                string ser = SimpleJson.SimpleJson.SerializeObject(command);

                _logger.WriteLine("->> Command: " + type);
                _logger.WriteLine("Data: " + ser);
                _logger.WriteLine();
                _logger.Flush();

                _ws.Send(type + " " + ser);
            }

            catch (Exception ex)
            {
                ex.Source = "F-Chat connection, Send Message Method, IDictionary<string, object> overload";
                Exceptions.HandleException(ex);
            }
        }

        /// <summary>
        /// Sends an argument-less command to the server
        /// </summary>
        /// <param name="commandType">Type of command to send</param>
        public void SendMessage(string commandType)
        {
            try
            {
                if (commandType.Length > 3 || commandType.Length < 3)
                    throw new ArgumentOutOfRangeException("commandType", "Command type must be 3 characters long");

                _logger.WriteLine("->> Command: " + commandType);
                _logger.WriteLine();
                _logger.Flush();

                _ws.Send(commandType);
            }

            catch (Exception ex)
            {
                ex.Source = "F-Chat connection, SendMessage method";
                Exceptions.HandleException(ex);
            }

        }
        #endregion

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool isManagedDispose)
        {
            if (isManagedDispose)
                _logger.Dispose();
            _ws.Close();
        }
    }

    public interface IChatConnection
    {
        IAccount Account { get; }
        string Character { get; }

        void SendMessage(object command, string command_type);
        void SendMessage(string commandType);
        void SendMessage(IDictionary<string, object> command);
    }
}