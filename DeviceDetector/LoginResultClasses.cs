using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceDetector
{
    //Created using https://json2csharp.com/

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    internal class Created
    {
        public string Token { get; set; }
        public string UserLevel { get; set; }
        public int UserId { get; set; }
    }

    internal class LoginResult
    {
        public Created Created { get; set; }
    }
}
