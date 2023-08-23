using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace pdt_shureMXW_epi
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
                Debug.Console(0, Debug.ErrorLogLevel.Error, "Unable to create comms for device {0}", dc.Key);
                return null;
            }

            var config = dc.Properties.ToObject<Properties>();

            if (config != null)
            {
                return new ShureMxwDevice(dc.Key, dc.Name, comms, config);
            }

            Debug.Console(0, Debug.ErrorLogLevel.Error, "Unable to deserialize config for device {0}", dc.Key);
            return null;
        }

        #endregion
    }
}