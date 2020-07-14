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
            Debug.Console(2, micDevice, "Linked Online at {0}", joinMap.IsOnline);
            trilist.SetUShortSigAction(joinMap.SetGlobalStatus, u => { if (u > 0) { micDevice.SetStatus(u); } });
            Debug.Console(2, micDevice, "Linked SetGlobalStatus at {0}", joinMap.SetGlobalStatus);


            foreach (var item in micDevice._Props.Mics)
            {
                var i = item;
                var offset = (uint)((i.index - 1) * 4);

                Debug.Console(2, micDevice, "Mic Channel {0} Connect", i.index);

                trilist.BooleanInput[(joinMap.Enabled + offset)].BoolValue = i.enabled;
                Debug.Console(2, micDevice, "Linked Mic {0} Enabled at {1}", i.index, joinMap.Enabled + offset);
                micDevice.MicEnableFeedback[i.index].LinkInputSig(trilist.BooleanInput[joinMap.Enabled + offset]);

                trilist.StringInput[(joinMap.Name + offset)].StringValue = micDevice.MicNames[i.index];
                Debug.Console(2, micDevice, "Linked Mic {0} Name at {1}", i.index, joinMap.Name + offset);
                micDevice.MicNamesFeedback[i.index].LinkInputSig(trilist.StringInput[joinMap.Name + offset]);

                micDevice.MicBatteryLevelFeedback[i.index].LinkInputSig(trilist.UShortInput[joinMap.BatteryLevel + offset]);
                Debug.Console(2, micDevice, "Linked Mic {0} Battery Level Feedback at {1}", i.index, joinMap.BatteryLevel + offset);

                micDevice.MicStatusFeedback[i.index].LinkInputSig(trilist.UShortInput[joinMap.LocalStatus + offset]);
                Debug.Console(2, micDevice, "Linked Mic {0} Status Feedback at {1}", i.index, joinMap.LocalStatus+ offset);

                micDevice.MicLowBatteryStatusFeedback[i.index].LinkInputSig(trilist.UShortInput[joinMap.BatteryStatus + offset]);
                Debug.Console(2, micDevice, "Linked Mic {0} Battery Status Feedback at {1}", i.index, joinMap.BatteryStatus + offset);

                micDevice.MicLowBatteryCautionFeedback[i.index].LinkInputSig(trilist.BooleanInput[joinMap.LowBatteryCaution + offset]);
                Debug.Console(2, micDevice, "Linked Mic {0} Battery Caution Feedback at {1}", i.index, joinMap.LowBatteryCaution + offset);

                micDevice.MicLowBatteryWarningFeedback[i.index].LinkInputSig(trilist.BooleanInput[joinMap.LowBatteryWarning + offset]);
                Debug.Console(2, micDevice, "Linked Mic {0} Battery Warning Feedback at {1}", i.index, joinMap.LowBatteryWarning + offset);

                micDevice.MicOnChargerFeedback[i.index].LinkInputSig(trilist.BooleanInput[joinMap.OnCharger + offset]);
                Debug.Console(2, micDevice, "Linked Mic {0} On Charger Feedback at {1}", i.index, joinMap.OnCharger + offset);


                trilist.SetUShortSigAction(joinMap.SetLocalStatus + offset, u => { if (u > 0) { micDevice.SetStatus(i.index, u); } });
                Debug.Console(2, micDevice, "Linked Mic {0} Set Local Status at {1}", i.index, joinMap.SetLocalStatus + offset);

            }

            trilist.OnlineStatusChange += (d, args) =>
            {
                if (!args.DeviceOnLine) return;
                foreach (var item in micDevice.MicNamesFeedback)
                {                       
                    item.Value.FireUpdate();
                }

                foreach (var item in micDevice.MicEnableFeedback)
                {
                    item.Value.FireUpdate();
                }
            };
        }
    }
}