using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common;
using PepperDash.Essentials.Bridges;
using Newtonsoft.Json;
using Crestron.SimplSharp.Reflection;
using pdt_shureMXA_epi.Bridge.JoinMap;


namespace pdt_shureMXA_epi.Bridge
{
    public static class SureMxwDeviceApiExtensions
    {
        public static void LinkToApiExt(this ShureMxwDevice micDevice, BasicTriList trilist, uint joinStart, string joinMapKey) {
            ShureMxwDeviceJoinMap joinMap = new ShureMxwDeviceJoinMap();

            Debug.Console(1, micDevice, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(2, micDevice, "There are {0} Mics", micDevice._Props.Mics.Count());
            joinMap.OffsetJoinNumbers(joinStart);

            micDevice.CommunicationMonitor.IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline]);
            trilist.SetUShortSigAction(joinMap.SetGlobalStatus, u => { if (u > 0) { micDevice.SetStatus(u); } });

            foreach (var item in micDevice._Props.Mics)
            {
                var i = item;
                var offset = (uint)((i.index - 1) * 3);

                Debug.Console(2, micDevice, "Mic Channe; {0} Connect", i.index);

                trilist.BooleanInput[(joinMap.Enabled + offset)].BoolValue = i.enabled;
                trilist.StringInput[(joinMap.Name + offset)].StringValue = i.name;

                micDevice.MicBatteryLevelFeedback[i.index].LinkInputSig(trilist.UShortInput[joinMap.BatteryLevel + offset]);
                micDevice.MicStatusFeedback[i.index].LinkInputSig(trilist.UShortInput[joinMap.LocalStatus + offset]);
                micDevice.MicLowBatteryStatusFeedback[i.index].LinkInputSig(trilist.UShortInput[joinMap.BatteryStatus + offset]);
                micDevice.MicLowBatteryCautionFeedback[i.index].LinkInputSig(trilist.BooleanInput[joinMap.LowBatteryCaution + offset]);
                micDevice.MicLowBatteryWarningFeedback[i.index].LinkInputSig(trilist.BooleanInput[joinMap.LowBatteryWarning + offset]);

                trilist.SetUShortSigAction(joinMap.SetLocalStatus + offset, u => { if (u > 0) { micDevice.SetStatus(i.index, u); } });
            }
        }
    }
}