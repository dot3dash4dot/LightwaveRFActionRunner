using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightwaveDaemon
{
    class Phone
    {
        public string PersonName { get; }
        public string PhoneName { get; }
        public string IP { get; }

        public Phone(string personName, string phoneName, string ip = null)
        {
            PersonName = personName;
            PhoneName = phoneName;
            IP = ip;
        }
    }
}
