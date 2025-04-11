using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DeviceDetector
{
    public static class FritzBoxRouter
    {
        public static async Task<IEnumerable<string>> GetWifiConnectedDevices(string routerIP, string routerUsername, string routerPassword)
        {
            IEnumerable<string> deviceNames;
            using (HttpClient client = new HttpClient())
            {
                string sessionId = await GetSessionId(client, routerIP, routerUsername, routerPassword);

                deviceNames = await GetConnectedDevices(client, routerIP, sessionId);

                await Logout(client, routerIP, sessionId);
            };

            return deviceNames;
        }

        public static async Task<string> GetSessionId(HttpClient client, string routerIP, string routerUsername, string routerPassword)
        {
            string loginUrl = $"{routerIP}/login_sid.lua";

            var response = await client.GetStringAsync(loginUrl);

            // Extract challenge and SID from the response XML
            var challenge = GetXmlValue(response, "Challenge");
            var sid = GetXmlValue(response, "SID");

            if (sid != "0000000000000000")
            {
                // Already logged in or no password needed
                return sid;
            }

            // Calculate response hash
            var challengeResponse = $"{challenge}-{GetMd5Hash($"{challenge}-{routerPassword}")}";

            // Send login request
            var loginUri = $"{loginUrl}?username={routerUsername}&response={challengeResponse}";
            var loginResponse = await client.GetStringAsync(loginUri);

            // Extract session ID again
            var newSid = GetXmlValue(loginResponse, "SID");
            return newSid;
        }

        private static string GetXmlValue(string xml, string tag)
        {
            var start = xml.IndexOf($"<{tag}>") + tag.Length + 2;
            var end = xml.IndexOf($"</{tag}>");
            return xml.Substring(start, end - start);
        }

        private static string GetMd5Hash(string input)
        {
            Encoding encoding = Encoding.Unicode; // FritzBox uses UTF-16LE
            using MD5 md5 = MD5.Create();
            byte[] hashBytes = md5.ComputeHash(encoding.GetBytes(input));
            StringBuilder sb = new StringBuilder();
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        private static async Task<IEnumerable<string>> GetConnectedDevices(HttpClient client, string routerIP, string sessionId)
        {
            // Create POST data
            var postData = new StringContent(
                $"sid={sessionId}&page=networkDevices",
                Encoding.UTF8,
                "application/x-www-form-urlencoded"
            );

            // Send POST request
            var response = await client.PostAsync($"{routerIP}/data.lua", postData);
            string json = await response.Content.ReadAsStringAsync();

            var root = JObject.Parse(json);
            var deviceNames = root["data"]["net"]["devices"].Select(x => (string)x["name"]);

            return deviceNames;
        }

        private static async Task Logout(HttpClient client, string routerIP, string sessionId)
        {
            await client.GetStringAsync($"{routerIP}/login_sid.lua?sid={sessionId}&logout=1");
        }
    }
}

