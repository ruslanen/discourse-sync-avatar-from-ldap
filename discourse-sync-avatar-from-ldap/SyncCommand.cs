using System.Threading.Tasks;

namespace discourse_sync_avatar_from_ldap
{
    public class SyncCommand : ICommand
    {
        private readonly Synchronizer _synchronizer;
        private readonly ApplicationConfiguration _applicationConfiguration;

        public SyncCommand(Synchronizer synchronizer, ApplicationConfiguration applicationConfiguration)
        {
            _synchronizer = synchronizer;
            _applicationConfiguration = applicationConfiguration;
        }
        
        public async Task ExecuteAsync()
        {
            await _synchronizer.SyncDiscourseAvatarFromLdap(_applicationConfiguration);
        }
    }
}