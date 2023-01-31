using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Core;

namespace ShureWireless.Common
{
    public class ShureMicProperties
    {
        [JsonProperty("control")]
        public ControlPropertiesConfig Control { get; set; }

        [JsonProperty("controlChargerBase")]
        public ControlPropertiesConfig ControlChargerBase { get; set; }

        [JsonProperty("controlChargerBase2")]
        public ControlPropertiesConfig ControlChargerBase2 { get; set; }

        [JsonProperty("mics")]
        public List<Mics> Mics { get; set; }

        [JsonProperty("chargerBases")]
        public List<ChargerBaseControlProperties> ChargerBases { get; set; }
        
        [JsonProperty("cautionThreshold")]
        public int CautionThreshold { get; set; }
        [JsonProperty("warningThreshold")]
        public int WarningThreshold { get; set; }
    }

    public class Mics
    {
        [JsonProperty("index")]
        public int Index { get; set; }
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("onChargerFbEnable")]
        public bool OnChargerFbEnable { get; set; }

    }

    public class ChargerBaseControlProperties : ControlPropertiesConfig
    {
        [JsonProperty("index")]
        public int Index { get; set; }
    }

}