using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Reflection;
using PepperDash.Essentials.Core;

namespace pdt_shureMXA_epi.Bridge.JoinMap
{
    public class ShureMxwDeviceJoinMap : JoinMapBase
    {
        #region Digital
        public uint IsOnline { get; set; }
        public uint Enabled { get; set; }
        public uint LowBatteryCaution { get; set; }
        public uint LowBatteryWarning { get; set; }

        #endregion

        #region Analog
        public uint SetGlobalStatus { get; set; }
        public uint SetLocalStatus { get; set; }
        public uint LocalStatus { get; set; }
        public uint BatteryLevel { get; set; }
        public uint BatteryStatus { get; set; }
        #endregion

        #region Serial
        public uint Name { get; set; }
        #endregion

        public ShureMxwDeviceJoinMap()
        {
            IsOnline = 1;
            SetGlobalStatus = 1;
            Enabled = 2;
            LowBatteryCaution = 3;
            LowBatteryWarning = 4;
            Name = 2;
            SetLocalStatus = 2;
            LocalStatus = 2;
            BatteryLevel = 3;
            BatteryStatus = 4;
        }


        public override void OffsetJoinNumbers(uint joinStart)
        {
            var joinOffset = joinStart - 1;
            var properties = this.GetType().GetCType().GetProperties().Where(o => o.PropertyType == typeof(uint)).ToList();
            foreach (var property in properties)
            {
                property.SetValue(this, (uint)property.GetValue(this, null) + joinOffset, null);
            }
        }
    }
}