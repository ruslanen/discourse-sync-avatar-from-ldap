using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace discourse_sync_avatar_from_ldap
{
    public class Application
    {
        public void Run(string[] arguments)
        {
            var i = 0;
            if (arguments.Length > 0)
            {
                var argument = arguments[i++];
                while (argument != null)
                {
                    switch (argument)
                    {
                        case "--help":
                            Console.WriteLine("Usage: discourse-sync-avatar-from-ldap.exe [--config] <path>");
                            break;
                        case "--config":
                            var configPath = i < argument.Length ? arguments[i++] : throw new Exception("Expected argument <path>");
                            var applicationConfiguration = JsonConvert.DeserializeObject<ApplicationConfiguration>(File.ReadAllText(configPath));
                            ICommand command = new SyncCommand(new Synchronizer(), applicationConfiguration);
                            command.ExecuteAsync().Wait();
                            break;
                        default:
                            throw new Exception("Unknown argument");
                    }
                    
                    argument = i < arguments.Length ? arguments[i++] : null;
                }
            }
        }
    }
    
    public class ApplicationConfiguration
    {
        public DiscourseSettings Discourse { get; set; }
        public ActiveDirectorySettings ActiveDirectory { get; set; }
    }

    public class DiscourseSettings
    {
        public string Url { get; set; }
        public string ApiKey { get; set; }
        public string UserName { get; set; }
    }

    public class ActiveDirectorySettings
    {
        public string Host { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string MailDomain { get; set; }
    }
}