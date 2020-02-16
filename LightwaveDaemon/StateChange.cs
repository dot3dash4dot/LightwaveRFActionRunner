using LightwaveRFLinkPlusSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LightwaveDaemon.Configuration;

namespace LightwaveDaemon
{
    class StateChange
    {
        public DeviceName DeviceName { get; set; }
        public bool SwitchOn { get; set; }
        public bool EvenIfHome { get; set; }

        public StateChange(DeviceName deviceName, bool switchOn, bool evenIfHome = false)
        {
            DeviceName = deviceName;
            SwitchOn = switchOn;
            EvenIfHome = evenIfHome;
        }

        public override string ToString()
        {
            string state = SwitchOn ? "On" : "Off";
            return $"{DeviceName} {state}";
        }
    }

    static class StateChangeExtensions
    {
        public static async Task Run(this StateChange stateChange, Device[] devices, LightwaveAPI api)
        {
            Device device = stateChange.DeviceName.ToDevice(devices);
            await api.SetSwitchStateAsync(device, stateChange.SwitchOn);
        }
    }
}
