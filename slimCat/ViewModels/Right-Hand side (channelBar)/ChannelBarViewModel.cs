﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelBarViewModel.cs" company="Justin Kadrovach">
//   Copyright (c) 2013, Justin Kadrovach
//   All rights reserved.
//   
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are met:
//       * Redistributions of source code must retain the above copyright
//         notice, this list of conditions and the following disclaimer.
//       * Redistributions in binary form must reproduce the above copyright
//         notice, this list of conditions and the following disclaimer in the
//         documentation and/or other materials provided with the distribution.
//   
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//   ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//   WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
//   DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
//   DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
//   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
//   LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
//   ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
// <summary>
//   The ChannebarViewModel is a wrapper to hold the other viewmodels which form the gist of the interaction network.
//   It responds to the ChatOnDisplayEvent to paritally create the chat wrapper.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Slimcat.ViewModels
{
    using System;
    using System.Windows.Input;

    using Microsoft.Practices.Prism.Events;
    using Microsoft.Practices.Prism.Regions;
    using Microsoft.Practices.Unity;

    using Libraries;
    using Models;
    using Utilities;
    using Views;

    /// <summary>
    ///     The ChannebarViewModel is a wrapper to hold the other viewmodels which form the gist of the interaction network.
    ///     It responds to the ChatOnDisplayEvent to paritally create the chat wrapper.
    /// </summary>
    public class ChannelbarViewModel : ChannelbarViewModelCommon
    {
        #region Constants

        /// <summary>
        ///     The channelbar view.
        /// </summary>
        internal const string ChannelbarView = "ChannelbarView";

        private const string TabViewRegion = "TabViewRegion";

        #endregion

        #region Fields

        private string currentSelected;

        private bool hasUpdate;

        private bool isExpanded = true;

        private RelayCommand @select;

        private RelayCommand toggle;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelbarViewModel"/> class.
        /// </summary>
        /// <param name="cm">
        /// The cm.
        /// </param>
        /// <param name="contain">
        /// The contain.
        /// </param>
        /// <param name="regman">
        /// The regman.
        /// </param>
        /// <param name="events">
        /// The events.
        /// </param>
        public ChannelbarViewModel(
            IChatModel cm, IUnityContainer contain, IRegionManager regman, IEventAggregator events)
            : base(contain, regman, events, cm)
        {
            try
            {
                this.Events.GetEvent<ChatOnDisplayEvent>().Subscribe(this.RequestNavigate, ThreadOption.UIThread, true);

                // create the tabs
                this.Container.Resolve<ChannelsTabViewModel>();
                this.Container.Resolve<UsersTabViewModel>();
                this.Container.Resolve<NotificationsTabViewModel>();
                this.Container.Resolve<GlobalTabViewModel>();
                this.Container.Resolve<ManageListsTabView>();

                this.ChatModel.Notifications.CollectionChanged += (s, e) =>
                    {
                        if (!this.IsExpanded)
                        {
                            // removed checking logic, allow the notifications daemon to worry about that
                            this.HasUpdate = true;
                        }
                    };
            }
            catch (Exception ex)
            {
                ex.Source = "Channelbar ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Public Events

        /// <summary>
        ///     The on jump to notifications.
        /// </summary>
        public event EventHandler OnJumpToNotifications;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the change tab command.
        /// </summary>
        public ICommand ChangeTabCommand
        {
            get
            {
                return this.@select ?? (this.@select = new RelayCommand(this.NavigateToTabEvent));
            }
        }

        /// <summary>
        ///     Gets the expand string.
        /// </summary>
        public string ExpandString
        {
            get
            {
                if (this.HasUpdate && !this.IsExpanded)
                {
                    return "!";
                }

                return this.IsExpanded ? ">" : "<";
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether has update.
        /// </summary>
        public bool HasUpdate
        {
            get
            {
                return this.hasUpdate;
            }

            set
            {
                this.hasUpdate = value;
                this.OnPropertyChanged("HasUpdate");
                this.OnPropertyChanged("ExpandString"); // bug fix where the exclaimation point would never show
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                return this.isExpanded;
            }

            set
            {
                this.isExpanded = value;
                this.OnPropertyChanged("IsExpanded");
                this.OnPropertyChanged("ExpandString");
            }
        }

        /// <summary>
        ///     Gets the toggle bar command.
        /// </summary>
        public ICommand ToggleBarCommand
        {
            get
            {
                return this.toggle ?? (this.toggle = new RelayCommand(
                delegate
                    {
                        this.IsExpanded = !this.IsExpanded;

                        if (this.IsExpanded)
                        {
                            // this shoots us to the notifications tab if we have something to see there
                            if (this.hasUpdate)
                            {
                                this.NavigateToTabEvent("Notifications");

                                // used to check if we weren't already here; now that isn't possible
                                if (this.OnJumpToNotifications != null)
                                {
                                    this.OnJumpToNotifications(
                                        this, new EventArgs());

                                    // this lets the view sync our jump
                                }

                                this.HasUpdate = false;
                            }
                            else if (
                                !string.IsNullOrWhiteSpace(
                                    this.currentSelected))
                            {
                                // this fixes a very subtle bug where a list won't load or won't load properly after switching tabs
                                this.NavigateToTabEvent(
                                    this.currentSelected);
                            }
                        }
                        else
                        {
                            // when we close it, unload the tab, but _currentSelected remains what it was so we remember user input
                            this.NavigateToTabEvent("NoTab");
                        }
                    }));
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The initialize.
        /// </summary>
        public override void Initialize()
        {
            try
            {
                this.Container.RegisterType<object, ChannelbarView>(ChannelbarView);
            }
            catch (Exception ex)
            {
                ex.Source = "Channelbar ViewModel, init";
                Exceptions.HandleException(ex);
            }
        }

        #endregion

        #region Methods

        private void NavigateToTabEvent(object args)
        {
            var newSelected = args as string;

            if (newSelected != "NoTab")
            {
                // this isn't really a selected state
                this.currentSelected = newSelected;
            }

            switch (args as string)
            {
                case "Channels":
                    {
                        this.RegionManager.Regions[TabViewRegion].RequestNavigate(ChannelsTabViewModel.ChannelsTabView);
                        break;
                    }

                case "Users":
                    {
                        this.RegionManager.Regions[TabViewRegion].RequestNavigate(UsersTabViewModel.UsersTabView);
                        break;
                    }

                case "Notifications":
                    {
                        this.RegionManager.Regions[TabViewRegion].RequestNavigate(
                            NotificationsTabViewModel.NotificationsTabView);
                        break;
                    }

                case "Global":
                    {
                        this.RegionManager.Regions[TabViewRegion].RequestNavigate(GlobalTabViewModel.GlobalTabView);
                        break;
                    }

                case "ManageLists":
                    {
                        this.RegionManager.Regions[TabViewRegion].RequestNavigate(ManageListsViewModel.ManageListsTabView);
                        break;
                    }

                case "NoTab":
                    {
                        foreach (var view in this.RegionManager.Regions[TabViewRegion].Views)
                        {
                            this.RegionManager.Regions[TabViewRegion].Remove(view);
                        }

                        break;
                    }
            }
        }

        private void RequestNavigate(bool? payload)
        {
            this.Events.GetEvent<ChatOnDisplayEvent>().Unsubscribe(this.RequestNavigate);
            this.RegionManager.Regions[ChatWrapperView.ChannelbarRegion].Add(this.Container.Resolve<ChannelbarView>());
        }

        #endregion
    }
}