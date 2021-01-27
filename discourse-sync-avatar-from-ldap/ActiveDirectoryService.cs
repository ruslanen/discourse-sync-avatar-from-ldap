using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Novell.Directory.Ldap;

namespace discourse_sync_avatar_from_ldap
{
    public class ActiveDirectoryService
    {
        private readonly string serverAddress;
        private readonly string login;
        private readonly string password;

        public ActiveDirectoryService(
            string serverAddress,
            string login,
            string password)
        {
            this.serverAddress = serverAddress;
            this.login = login;
            this.password = password;
        }

        public async Task<IReadOnlyDictionary<long, UserInfo>> GetUserProfileImages(IReadOnlyCollection<DiscourseUser> users, string mailDomain)
        {
            var stringBuilder = new StringBuilder("(&(objectclass=user)(objectcategory=person)(|");
            foreach (var user in users)
            {
                stringBuilder.Append("(mail=");
                stringBuilder.Append(user.username);
                stringBuilder.Append($"@{mailDomain})");
            }

            stringBuilder.Append("))");
            var searchFiler = stringBuilder.ToString();

            var attributes = new string[] {"cn", "thumbnailPhoto", "mail"};
            try
            {
                using var ldapConnection = new LdapConnection();
                await ldapConnection.ConnectAsync(serverAddress, LdapConnection.DefaultPort);
                var (domain, domainInfo) = await GetDomainDescription(ldapConnection);
                await ldapConnection.BindAsync($@"{login}@{domain}", password);

                var searchResults = await ldapConnection.SearchAsync(domainInfo, LdapConnection.ScopeSub, searchFiler,
                    attributes, false);

                return GetSearchResult(searchResults, users);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<(string Domain, string DomainInfo)> GetDomainDescription(LdapConnection ldapConnection)
        {
            var schemaDn = await ldapConnection.GetSchemaDnAsync();
            var domainParts = schemaDn.Split(",")
                .Where(x => x.StartsWith("DC"))
                .Select(x => x.Substring(3))
                .ToArray();
            var domain = string.Join('.', domainParts);
            var domainInfoParts = schemaDn.Split(",")
                .Where(x => x.StartsWith("DC"))
                .ToArray();
            var domainInfo = string.Join(',', domainInfoParts);

            return (domain, domainInfo);
        }

        // https://docs.bmc.com/docs/fpsc121/ldap-attributes-and-associated-fields-495323340.html
        private IReadOnlyDictionary<long, UserInfo> GetSearchResult(
            ILdapSearchResults searchResults,
            IReadOnlyCollection<DiscourseUser> users)
        {
            var result = new Dictionary<long, UserInfo>();
            try
            {
                while (searchResults.HasMore())
                {
                    var nextEntry = searchResults.Next();
                    var userName = nextEntry.GetAttribute("mail")?.StringValue?.Split('@')[0];
                    var discourseUser = users.FirstOrDefault(x => x.username == userName);
                    if (discourseUser != null)
                    {
                        result.Add(
                            discourseUser.id,
                            new UserInfo
                            {
                                Id = discourseUser.id,
                                UserName = discourseUser.username,
                                Image = nextEntry.GetAttribute("thumbnailPhoto")?.ByteValue,
                            });
                    }
                }
            }
            catch (LdapException ex)
            {
                if (ex.ResultCode != 10)
                {
                    throw;
                }
            }

            return result;
        }
    }

    public class UserInfo
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public byte[] Image { get; set; }
    }
}