/**
 * Copyright (c) 2013-2014 Microsoft Mobile.
 * See the license file delivered with this project for more information.
 */


using ImageSequencer.Common;
using Windows.UI.Xaml.Controls;

namespace ImageSequencer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage : Page
    {

        private NavigationHelper _navigationHelper;

        public AboutPage()
        {
            this.InitializeComponent();

            _navigationHelper = new NavigationHelper(this);
        }

    }
}
