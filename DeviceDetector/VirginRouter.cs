using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace DeviceDetector
{
    public static class VirginRouter
    {
        public static async Task<IEnumerable<(string Name, string IP)>> GetWifiConnectedDevices(string routerIP, string routerPassword)
        {
            HttpClient httpClient = new HttpClient();

            //Log in to router
            var loginContent = new StringContent($"{{\"password\":\"{routerPassword}\"}}");
            loginContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            var loginResult = await RunQuery<LoginResult>(httpClient.PostAsync($"{routerIP}/rest/v1/user/login", loginContent));
            string token = loginResult.Created.Token;

            //Use token for future requests
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            //Get Connected Hosts
            var getHostsResult = await RunQuery<GetHostsResult>(httpClient.GetAsync($"{routerIP}/rest/v1/network/hosts?connectedOnly=true"));
            var deviceNames = getHostsResult.Hosts1.Hosts2.Select(x => (x.Config.Hostname, x.Config.Ipv4.Address));

            //Log out of router
            await httpClient.DeleteAsync($"{routerIP}/rest/v1/user/3/token/{token}");

            return deviceNames;
        }

        private static async Task<T> RunQuery<T>(Task<HttpResponseMessage> query)
        {
            var response = await query;
            var result = response.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<T>(result);
        }
    }
}
