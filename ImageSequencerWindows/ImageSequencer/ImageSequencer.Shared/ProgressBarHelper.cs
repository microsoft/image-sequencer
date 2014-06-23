/**
 * Copyright (c) 2013-2014 Microsoft Mobile.
 * See the license file delivered with this project for more information.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace ImageSequencer
{
    class ProgressBarHelper
    {
#pragma warning disable 1998
        public static async void ShowProgressBar(String label)
        {
#if WINDOWS_PHONE_APP
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    StatusBarProgressIndicator progressbar = StatusBar.GetForCurrentView().ProgressIndicator;
                    progressbar.Text = label;
                    await progressbar.ShowAsync();
                }
            );
#endif
        }

#pragma warning disable 1998
        public static async void HideProgressBar()
        {
#if WINDOWS_PHONE_APP
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                async () =>
                {
                    StatusBarProgressIndicator progressbar = StatusBar.GetForCurrentView().ProgressIndicator;
                    await progressbar.HideAsync();
                }
            );
#endif
        }
    }
}
