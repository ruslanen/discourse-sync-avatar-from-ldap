using System;
using System.Linq;
using System.Threading.Tasks;

namespace discourse_sync_avatar_from_ldap
{
    public class Synchronizer
    {
        public async Task SyncDiscourseAvatarFromLdap(ApplicationConfiguration applicationConfiguration)
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
}