using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using Serilog.Events;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugin.Shure.Mxw
{
    public class ShureMxwFactory : EssentialsPluginDeviceFactory<ShureMxwDevice>
    {
        public ShureMxwFactory()
            : base()
        {
            MinimumEssentialsFrameworkVersion = "1.6.6";

            TypeNames = new List<string> { "shuremxw" };
        }

        #region Overrides of EssentialsDeviceFactory<SamsungMdcDisplayController>

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            var comms = CommFactory.CreateCommForDevice(dc);

            if (comms == null)
            {
                Debug.LogMessage(LogEventLevel.Information, "Unable to create comms for device {0}", this);
                return null;
            }

            var config = dc.Properties.ToObject<ShureMxwConfigProperties>();

            if (config != null)
            {
                return new ShureMxwDevice(dc.Key, dc.Name, comms, config);
            }

            Debug.LogMessage(LogEventLevel.Information, "Unable to deserialize config for device {0}", this);
            return null;
        }

        #endregion
    }
}