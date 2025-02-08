using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceDetector
{
    //Created using https://json2csharp.com/

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Config
    {
        [JsonProperty("connected")]
        public bool Connected { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("deviceType")]
        public string DeviceType { get; set; }

        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("interface")]
        public string Interface { get; set; }

        [JsonProperty("speed")]
        public double Speed { get; set; }

        [JsonProperty("ethernet")]
        public Ethernet Ethernet { get; set; }

        [JsonProperty("ipv4")]
        public Ipv4 Ipv4 { get; set; }

        [JsonProperty("wifi")]
        public Wifi Wifi { get; set; }

        [JsonProperty("unknown")]
        public Unknown Unknown { get; set; }
    }

    public class Ethernet
    {
        [JsonProperty("port")]
        public int Port { get; set; }
    }

    public class Hosts1
    {
        [JsonProperty("hosts")]
        public List<Hosts2> Hosts2 { get; set; }
    }

    public class Hosts2
    {
        [JsonProperty("macAddress")]
        public string MacAddress { get; set; }

        [JsonProperty("config")]
        public Config Config { get; set; }
    }

    public class Ipv4
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("leaseTimeRemaining")]
        public int LeaseTimeRemaining { get; set; }
    }

    public class GetHostsResult
    {
        [JsonProperty("hosts")]
        public Hosts1 Hosts1 { get; set; }
    }

    public class Unknown
    {
    }

    public class Wifi
    {
        [JsonProperty("band")]
        public string Band { get; set; }

        [JsonProperty("ssid")]
        public string Ssid { get; set; }

        [JsonProperty("rssi")]
        public int Rssi { get; set; }
    }



}
