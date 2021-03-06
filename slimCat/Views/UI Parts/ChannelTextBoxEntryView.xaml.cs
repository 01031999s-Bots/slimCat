﻿#region Copyright

// <copyright file="ChannelTextBoxEntryView.xaml.cs">
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

namespace slimCat.Views
{
    #region Usings

    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Markup;
    using Models;
    using ViewModels;

    #endregion

    /// <summary>
    ///     Interaction logic for ChannelTextBoxEntryView.xaml
    /// </summary>
    public partial class ChannelTextBoxEntryView
    {
        #region Fields

        private ChannelViewModelBase vm;
        private ShortcutManager shortcuts;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChannelTextBoxEntryView" /> class.
        /// </summary>
        public ChannelTextBoxEntryView()
        {
            InitializeComponent();

            DataObject.AddPastingHandler(this, OnPaste);

            Entry.Language = XmlLanguage.GetLanguage(ApplicationSettings.Langauge);

            OnContextChanged();

            DataContextChanged += (s, e) => OnContextChanged();
        }

        private void PropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName != "Language")
                return;

            Dispatcher.BeginInvoke(
                (Action) delegate { Entry.Language = XmlLanguage.GetLanguage(ApplicationSettings.Langauge); });
        }

        #endregion

        #region Methods

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!ApplicationSettings.AllowMarkupPastedLinks) return;

            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text, true)) return;

            var pasteText = e.SourceDataObject.GetData(DataFormats.Text) as string;
            if (string.IsNullOrWhiteSpace(pasteText)) return;

            // only auto-markup links
            if (!(pasteText.StartsWith("http://") || pasteText.StartsWith("https://")) || pasteText.Contains(" "))
                return;


            e.CancelCommand();

            if (string.IsNullOrWhiteSpace(Entry.SelectedText))
            {
                var formattedPaste = $"[url={pasteText}][/url]";
                var caretPasteIndex = Entry.CaretIndex;
                Entry.Text = Entry.Text.Insert(Entry.CaretIndex, formattedPaste);
                Entry.CaretIndex = caretPasteIndex + formattedPaste.IndexOf("[/url]", StringComparison.Ordinal);
            }
            else
            {
                var oldText = Entry.SelectedText;
                var newPasteText = $"[url={pasteText}]{oldText}[/url]";
                Entry.SelectedText = newPasteText;

                // why invoke? Entry.Text doesn't update immediately, it's updated on a scheduler or something
                // this just schedules it after that
                Dispatcher.BeginInvoke((Action) (() =>
                {
                    var startIndex = Entry.Text.IndexOf($"{oldText}[/url]", StringComparison.Ordinal);
                    if (startIndex == -1) return; // don't want to crash if something weird happens
                    Entry.Select(startIndex, oldText.Length);
                }));
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers != ModifierKeys.Control) return;

            if (e.Key == Key.Up || e.Key == Key.Down) e.Handled = true;
        }

        private void OnContextChanged()
        {
            if (vm != null)
                vm.PropertyChanged -= PropertyChanged;

            vm = DataContext as ChannelViewModelBase;
            if (vm == null) return;

            shortcuts = new ShortcutManager(Entry, vm);
            vm.PropertyChanged += PropertyChanged;
        }

        #endregion
    }
}