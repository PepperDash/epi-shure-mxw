using Newtonsoft.Json;
using PepperDash.Core;

namespace pdt_shureMXW_epi
{
    public class Properties
    {
        public ControlPropertiesConfig Control { get; set; }

        [JsonProperty("cautionThreshold")]
        public int CautionThreshold { get; set; }
        [JsonProperty("WarningThreshold")]
        public int WarningThreshold { get; set; }
    }

    public class Mic : IKeyed
    {
        public string Key { get; set; }
        public int Index { get; set; }
        public bool Enabled { get; set; }
        public string Name { get; set; }

        public Mic(string key, MicDict mic)
        {
            Key = key;
            Index = mic.Index;
            Enabled = mic.Enabled || mic.Enable;
            Name = mic.Name;
        }
    }

    public class MicDict
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