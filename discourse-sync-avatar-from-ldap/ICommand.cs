using System.Threading.Tasks;

namespace discourse_sync_avatar_from_ldap
{
    public interface ICommand
    {
        Task ExecuteAsync();
    }
}