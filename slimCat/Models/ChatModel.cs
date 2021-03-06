﻿#region Copyright

// <copyright file="ChatModel.cs">
//     Copyright (c) 2013-2015, Justin Kadrovach, All rights reserved.
// 
//     This source is subject to the Simplified BSD License.
//     Please see the License.txt file for more information.
//     All other rights reserved.
// 
//     THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//     KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//     IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//     PARTICULAR PURPOSE.
// </copyright>

#endregion

namespace slimCat.Models
{
    #region Usings

    using System;
    using System.Collections.ObjectModel;
    using Utilities;
    using ViewModels;

    #endregion

    /// <summary>
    ///     Contains most chat data which spans channels. Channel-wide UI binds to this.
    /// </summary>
    public class ChatModel : SysProp, IChatModel
    {
        #region Public Events

        /// <summary>
        ///     The selected channel changed.
        /// </summary>
        public event EventHandler SelectedChannelChanged;

        #endregion

        #region Public Methods and Operators

        public ChannelModel FindChannel(string id, string title = null)
        {
            var channel = AllChannels.FirstByIdOrNull(id);

            return channel ?? new GeneralChannelModel(id, ChannelType.InviteOnly) {Title = title};
        }

        #endregion

        #region Fields

        private IAccount account;
        private ChannelModel currentChannel;

        private ICharacter currentCharacter;

        private ProfileData currentCharacterData;
        private bool isAuthenticated;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the all channels.
        /// </summary>
        public ObservableCollection<GeneralChannelModel> AllChannels { get; } =
            new ObservableCollection<GeneralChannelModel>();

        /// <summary>
        ///     Gets or sets the client uptime.
        /// </summary>
        public DateTimeOffset ClientUptime { get; set; }

        /// <summary>
        ///     Gets the current channels.
        /// </summary>
        public ObservableCollection<GeneralChannelModel> CurrentChannels { get; } =
            new ObservableCollection<GeneralChannelModel>();

        /// <summary>
        ///     Gets the current private messages.
        /// </summary>
        public ObservableCollection<PmChannelModel> CurrentPms { get; } = new ObservableCollection<PmChannelModel>();

        /// <summary>
        ///     Gets or sets a value indicating whether is authenticated.
        /// </summary>
        public bool IsAuthenticated
        {
            get { return isAuthenticated; }

            set
            {
                isAuthenticated = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is global moderator.
        /// </summary>
        public bool IsGlobalModerator { get; set; }

        /// <summary>
        ///     Gets or sets the last message received.
        /// </summary>
        public DateTimeOffset LastMessageReceived { get; set; }

        /// <summary>
        ///     Gets the notifications.
        /// </summary>
        public ObservableCollection<NotificationModel> Notifications { get; } =
            new ObservableCollection<NotificationModel>();

        /// <summary>
        ///     Gets or sets the our account.
        /// </summary>
        public IAccount CurrentAccount
        {
            get { return account; }

            set
            {
                account = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the current channel.
        /// </summary>
        public ChannelModel CurrentChannel
        {
            get { return currentChannel; }

            set
            {
                if (currentChannel == value || value == null)
                    return;

                currentChannel = value;

                SelectedChannelChanged?.Invoke(this, new EventArgs());

                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the current character.
        /// </summary>
        public ICharacter CurrentCharacter
        {
            get { return currentCharacter; }

            set
            {
                currentCharacter = value;
                OnPropertyChanged();
            }
        }

        public ProfileData CurrentCharacterData
        {
            get { return currentCharacterData; }
            set
            {
                currentCharacterData = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the server up time.
        /// </summary>
        public DateTimeOffset ServerUpTime { get; set; }

        public bool AutoReplyEnabled { get; set; }
        public string AutoReplyMessage { get; set; }

        #endregion
    }
}