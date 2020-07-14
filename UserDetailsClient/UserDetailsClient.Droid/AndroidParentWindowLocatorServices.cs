using UserDetailsClient.Core.Interfaces;
using Plugin.CurrentActivity;

namespace UserDetailsClient.Droid
{
    public class AndroidParentWindowLocatorService : IParentWindowLocatorService
    {
        public object GetCurrentParentWindow()
        {
            return CrossCurrentActivity.Current.Activity;
        }
    }
}
