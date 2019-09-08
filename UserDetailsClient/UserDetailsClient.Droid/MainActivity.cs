using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

using Microsoft.Identity.Client;

using UserDetailsClient.Core;
using UserDetailsClient.Core.Interfaces;

using Xamarin.Forms;

namespace UserDetailsClient.Droid
{
    [Activity(Label = "UserDetailsClient", Icon = "@drawable/icon", Theme = "@style/MyTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);

            LoadApplication(new App());

            var authenticationService = DependencyService.Get<IAuthenticationService>();
            // Default system browser
            authenticationService.SetParent(this);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
        }
    }
}

