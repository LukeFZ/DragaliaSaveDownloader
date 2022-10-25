using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NintendoAuth.Popup
{
    public partial class OAuthPopup : Form
    {
        private string _oauthCallbackUrl = "";

        public OAuthPopup(string url)
        {
            InitializeComponent();
            Resize += (sender, args) =>
            {
                OAuthWebview.Size = ClientSize - new Size(OAuthWebview.Location);
            };

            OAuthWebview.Source = new Uri(url);
            OAuthWebview.NavigationStarting += (sender, args) =>
            {
                var oauth = args.Uri;
                if (oauth.StartsWith("npf") && oauth.Contains("://auth"))
                {
                    _oauthCallbackUrl = oauth;
                    Hide();
                }
            };
        }

        public static string ShowPopup(string url)
        {
            var popup = new OAuthPopup(url);
            popup.ShowDialog();
            return popup._oauthCallbackUrl;
        }
    }
}
