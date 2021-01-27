using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;

namespace discourse_sync_avatar_from_ldap
{
    public class DiscourseService
    {
        private readonly string url;
        private readonly string apiKey;
        private readonly string userName;

        public DiscourseService(string url, string apiKey, string userName)
        {
            this.url = url;
            this.apiKey = apiKey;
            this.userName = userName;
        }

        public async Task<IReadOnlyCollection<DiscourseUser>> GetUsersListAsync()
        {
            var client = new RestClient(url);
            var request = BuildRestRequest("admin/users.json");
            var cts = new CancellationTokenSource();
            cts.CancelAfter(10000);
            try
            {
                var result = await client.ExecuteGetAsync<List<DiscourseUser>>(request, cts.Token);
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    return result.Data?.AsReadOnly();
                }
                else
                {
                    throw new Exception($"Non success result: {result.Content}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        public async Task<bool> UploadUserAvatar(UserInfo user)
        {
            var client = new RestClient(url);
            var uploadRequest = BuildRestRequest("uploads.json");
            uploadRequest.AddParameter("synchronous", true);
            uploadRequest.AddParameter("type", "avatar");
            uploadRequest.AddParameter("user_id", user.Id);
            uploadRequest.AddFileBytes("files[]", user.Image, $"{user.Id}.jpg", "image/jpeg");
            var cts = new CancellationTokenSource();
            cts.CancelAfter(5000);
            try
            {
                var uploadResult = await client.ExecutePostAsync<UploadResponse>(uploadRequest, cts.Token);
                if (uploadResult.StatusCode == HttpStatusCode.OK)
                {
                    var id = uploadResult.Data?.id;
                    if (!string.IsNullOrEmpty(id))
                    {
                        var putRequest = BuildRestRequest($"users/{user.UserName}/preferences/avatar/pick");
                        putRequest.AddParameter("upload_id", uploadResult.Data?.id);
                        putRequest.AddParameter("type", "uploaded");
                        var putResult = client.Put(putRequest);
                        if (putResult.StatusCode == HttpStatusCode.OK)
                        {
                            return true;
                        }
                    }
                }

                throw new Exception(uploadResult.Content);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private RestRequest BuildRestRequest(string actionUrl)
        {
            var request = new RestRequest(actionUrl, DataFormat.Json);
            request.AddHeader("Api-Key", apiKey);
            request.AddHeader("Api-Username", userName);
            return request;
        }
    }
    
    public class DiscourseUser
    {
        public int id { get; set; }
        
        public string username { get; set; }
        
        public string avatar_template { get; set; }
    }

    public class UploadBody
    {
        public string type { get; set; }
        
        public long user_id { get; set; }
        
        public bool synchronous { get; set; }
        
        public string files { get; set; }
    }

    public class UploadResponse
    {
        public string id { get; set; }
    }
}