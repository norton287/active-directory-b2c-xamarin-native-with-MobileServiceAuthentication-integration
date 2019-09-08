using System.Threading.Tasks;
using UserDetailsClient.Core.Models;

namespace UserDetailsClient.Core.Interfaces
{
    public interface IAuthenticationService
    {
        void SetParent(object parent);
        Task<UserContext> SignInAsync();
        Task<UserContext> SignOutAsync();
        Task<UserContext> EditProfileAsync();
        Task<UserContext> ResetPasswordAsync();
    }
}