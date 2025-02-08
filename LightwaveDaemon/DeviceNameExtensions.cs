using LightwaveRFLinkPlusSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utilities;

namespace LightwaveDaemon
{
    internal static class DeviceNameExtensions
    {
        public static Device ToDevice(this DeviceName deviceName, Device[] devices)
        {
            string realDeviceName = deviceName.DisplayName();
            return devices.Single(x => x.Name == realDeviceName);
        }

    }
}
