using LightwaveRFLinkPlusSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightwaveDaemon
{
    internal static class DeviceNameExtensions
    {
        public static string ToRealName(this DeviceName deviceName)
        {
            return Configuration.DeviceRealNameLookup[deviceName];
        }

        public static Device ToDevice(this DeviceName deviceName, Device[] devices)
        {
            string realDeviceName = deviceName.ToRealName();
            return devices.Single(x => x.Name == realDeviceName);
        }

    }
}
