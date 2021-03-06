﻿#region Copyright

// <copyright file="CodeCommand.cs">
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

namespace slimCat.Services
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Windows;
    using Utilities;

    #endregion

    public partial class UserCommandService
    {
        private void OnChannelCodeRequested(IDictionary<string, object> command)
        {
            if (cm.CurrentChannel.Id.Equals("Home", StringComparison.OrdinalIgnoreCase))
            {
                events.NewError("Home channel does not have a code.");
                return;
            }

            var toCopy = $"[session={cm.CurrentChannel.Title}]{cm.CurrentChannel.Id}[/session]";

            Clipboard.SetData(DataFormats.Text, toCopy);
            events.NewMessage("Channel's code copied to clipboard.");
        }
    }
}