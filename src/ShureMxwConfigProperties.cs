using Newtonsoft.Json;
using PepperDash.Core;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugin.Shure.Mxw
{
    public class ShureMxwConfigProperties
    {
        public ControlPropertiesConfig Control { get; set; }

        [JsonProperty("cautionThreshold")]
        public int CautionThreshold { get; set; }
        [JsonProperty("warningThreshold")]
        public int WarningThreshold { get; set; }
        [JsonProperty("mics")]
        public Dictionary<string, Mics> Mics { get; set; }
    }

    public class Mic : IKeyed
    {
        public string Key { get; set; }
        public int Index { get; set; }
        public bool Enabled { get; set; }
        public string Name { get; set; }

        public Mic(string key, Mics mic)
        {
            Key = key;
            Index = mic.Index;
            Enabled = mic.Enabled || mic.Enable;
            Name = mic.Name;
        }
    }

    public class Mics
    {
        [JsonProperty("index")]
        public int Index { get; set; }
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
        [JsonProperty("enable")]
        public bool Enable { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}