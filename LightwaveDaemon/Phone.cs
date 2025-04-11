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

        public Phone(string personName, string phoneName)
        {
            PersonName = personName;
            PhoneName = phoneName;
        }
    }
}
