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
                            SyncDiscourseAvatarFromLdap(applicationConfiguration).Wait();
                            break;
                        default:
                            throw new Exception("Unknown argument");
                    }
                    
                    argument = i < arguments.Length ? arguments[i++] : null;
                }
            }
        }

        private async Task SyncDiscourseAvatarFromLdap(ApplicationConfiguration applicationConfiguration)
        {
            var discourseSettings = applicationConfiguration.Discourse;
            var adSettings = applicationConfiguration.ActiveDirectory;
            var discourseService = new DiscourseService(discourseSettings.Url, discourseSettings.ApiKey, discourseSettings.UserName);
            // Получить всех пользователей из Discource
            var users = await discourseService.GetUsersListAsync();
            // Отфильтровать тех, у которых используется аватар по умолчанию
            var usersWithDefaultImage =
                users.Where(x => x.avatar_template.StartsWith("/letter_avatar_proxy")).ToList().AsReadOnly();
            var activeDirectoryService =
                new ActiveDirectoryService(adSettings.Host, adSettings.Login, adSettings.Password);
            // Получить пользоваталей из ActiveDirectory (на этом этапе может быть несоответствие числа пользователей Discourse <-> AD) 
            var userImages = await activeDirectoryService.GetUserProfileImages(usersWithDefaultImage, adSettings.MailDomain);
            // Загрузить фотографии пользователям

            var tasks = userImages.Values.Select(async user =>
            {
                var isSuccess = await discourseService.UploadUserAvatar(user);
                Console.WriteLine(isSuccess ? $"Success applied image for: {user.UserName}" : $"Can't apply image for: {user.UserName}");
            });
            await Task.WhenAll(tasks);
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