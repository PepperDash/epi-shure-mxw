using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace pdt_shureMXW_epi
{
    public class Properties
    {
        public ControlPropertiesConfig Control { get; set; }

        public List<Mics> Mics { get; set; }

        public int cautionthreshold { get; set; }
        public int warningThreshold { get; set; }
    }

    public class Mics
    {
        public int index { get; set; }
        public bool enabled { get; set; }
        public string name { get; set; }
    }
}