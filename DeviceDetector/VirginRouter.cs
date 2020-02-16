using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DeviceDetector
{
    public static class VirginRouter
    {
        public static IEnumerable<(string Name, string IP)> GetWifiConnectedDevices()
        {
            string result = RunPython("VirginRouter-GetWifiConnectedDevices.py");

            List<(string, string)> devices = new List<(string, string)>();

            foreach (Match deviceRaw in Regex.Matches(result, @"[\d\.']*: Row\(.*?\), '"))
            {
                Match ipMatch = Regex.Match(deviceRaw.Value, @"IPAddress\('(.*?)'\)");
                Match nameMatch = Regex.Match(deviceRaw.Value, @"hostname='(.*?)'");
                devices.Add((nameMatch.Groups[1].Value, ipMatch.Groups[1].Value));
            }

            return devices;
        }

        private static string RunPython(string pythonFileName)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = @"python";
            start.Arguments = pythonFileName;
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            //start.RedirectStandardError = true;
            start.CreateNoWindow = true;
            using (Process process = Process.Start(start))
            {
                //using (StreamReader reader = process.StandardError)
                //{
                //    return reader.ReadToEnd();
                //}
                using (StreamReader reader = process.StandardOutput)
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
