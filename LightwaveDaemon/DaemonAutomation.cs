using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightwaveDaemon
{
    class DaemonAutomation
    {
        private TimeSpan _exampleDuskTime = TimeSpan.FromHours(18);
        private TimeSpan _exampleBedTime = TimeSpan.FromHours(23);

        public string Name { get; set; }

        //Each automation needs an "example time" which assumes a dusk time of 6pm and a bed time of 11pm. These
        //will then be converted into real times later depending on the real dusk and bed times.
        public TimeSpan ExampleTime { get; set; }

        public StateChange[] StateChanges { get; set; }

        public DaemonAutomation(string name, TimeSpan exampleTime, StateChange[] stateChanges)
        {
            Name = name;
            ExampleTime = exampleTime;
            StateChanges = stateChanges;
        }

        public TimeSpan RealTime(TimeSpan todaysDuskTime)
        {
            double proportionOfExampleTime = (ExampleTime - _exampleDuskTime).TotalHours / (_exampleBedTime - _exampleDuskTime).TotalHours;
            double totalRealHours = (Configuration.BedTime - todaysDuskTime).TotalHours;

            return todaysDuskTime + TimeSpan.FromHours(proportionOfExampleTime * totalRealHours);
        }
    }
}
