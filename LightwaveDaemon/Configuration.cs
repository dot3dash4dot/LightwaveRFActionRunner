using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace LightwaveDaemon
{
    internal enum DeviceName
    {
        [Description("Hallway light")]
        Hallway,
    }

    internal static class Configuration
    {
        public static string EmailFromAddress = "address@domain.com"; 
        public static string EmailToAddress = "address@domain.com";

        public static Phone[] Phones = new Phone[]
        {
            new Phone("PERSON NAME", "PHONE NAME", "OPTIONAL FIXED PHONE IP")
        };

        public static TimeSpan BedTime => TimeSpan.Parse("23:11"); //The application currently relies on this time being before midnight


        private static List<DaemonAutomation> _daemonAutomations = new List<DaemonAutomation>
        {
            //Each automation needs an "example time" which assumes a dusk time of 6pm and a bed time of 11pm. These
            //will then be converted into real times later depending on the real dusk and bed times.
            new DaemonAutomation("Dusk", TimeSpan.Parse("18:00"), new StateChange[]
            {
                new StateChange(DeviceName.Hallway, true, true), //Always turn on hallway light, regardless of whether anyone is home or not
            }),
            new DaemonAutomation("Hallway off", TimeSpan.Parse("22:45"), new StateChange[]
            {
                new StateChange(DeviceName.Hallway, false) //Only turn light off if no-one is home
            })
        };

        public static IEnumerable<DaemonAutomation> DaemonAutomations => _daemonAutomations.OrderBy(x => x.ExampleTime);
    }
}
