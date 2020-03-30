using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Devices.Common.Codec;
using PepperDash.Essentials.Devices.Common.DSP;
using System.Text.RegularExpressions;
using Crestron.SimplSharp.Reflection;
using Newtonsoft.Json;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Bridges;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.Diagnostics;

using pdt_shureMXA_epi.Bridge;


namespace pdt_shureMXA_epi
{
    public class ShureMxwDevice :ReconfigurableDevice, IBridge
    {
        DeviceConfig _Dc;

        public Properties _Props { get; set; }

        public Dictionary<int, bool> MicEnable;
        public Dictionary<int, BoolFeedback> MicEnableFeedback;

        public Dictionary<int, IntFeedback> MicStatusFeedback;
        public Dictionary<int, int> MicStatus;

        public Dictionary<int, IntFeedback> MicBatteryLevelFeedback;
        public Dictionary<int, int> MicBatteryLevel;

        public Dictionary<int, BoolFeedback> MicLowBatteryCautionFeedback;
        public Dictionary<int, bool> MicLowBatteryCaution;

        public Dictionary<int, BoolFeedback> MicLowBatteryWarningFeedback;
        public Dictionary<int, bool> MicLowBatteryWarning;

        public Dictionary<int, IntFeedback> MicLowBatteryStatusFeedback;
        public Dictionary<int, int> MicLowBatteryStatus;

        public Dictionary<int, BoolFeedback> MicOnChargerFeedback;
        public Dictionary<int, bool> MicOnCharger;

        public Dictionary<int, StringFeedback> MicNamesFeedback;
        public Dictionary<int, string> MicNames;

        public CTimer Poll;

        public IBasicCommunication Communication { get; private set; }
        public CommunicationGather PortGather { get; private set; }
        public GenericCommunicationMonitor CommunicationMonitor { get; private set; }
        long _CautionThreshold { get; set; }

        int CautionThreshold
        {
            get
            {
                return (int)((_CautionThreshold * 65535) / 100);
            }
            set
            {
                _CautionThreshold = value;
            }
        }

        long _WarningThreshold { get; set; }

        int WarningThreshold
        {
            get
            {
                return (int)((_WarningThreshold * 65535) / 100);
            }
            set
            {
                _WarningThreshold = value;
            }
        }

        public static void LoadPlugin()
        {
            DeviceFactory.AddFactoryForType("shuremxw", ShureMxwDevice.BuildDevice);
        }

        public static ShureMxwDevice BuildDevice(DeviceConfig dc)
        {
            var comm = CommFactory.CreateCommForDevice(dc);
            var newMe = new ShureMxwDevice(dc.Key, dc.Name, comm, dc);

            return newMe;
        }

        public ShureMxwDevice(string key, string name, IBasicCommunication comm, DeviceConfig dc)
            : base(dc)
        {
            _Dc = dc;
            Name = name;
            
            _Props = JsonConvert.DeserializeObject<Properties>(_Dc.Properties.ToString());

            CautionThreshold = _Props.cautionthreshold;
            WarningThreshold = _Props.warningThreshold;

            Debug.Console(1, this, "Made it to consturctor for ShureMxw {0}", Name);
            Debug.Console(2, this, "ShureMxw Properties : {0}", _Dc.Properties.ToString());

            Communication = comm;
            var socket = comm as ISocketStatus;
            if (socket != null)
            {
                socket.ConnectionChange += new EventHandler<GenericSocketStatusChageEventArgs>(socket_ConnectionChange);
            }
            else
            {
                //I'm RS232, Yo!
            }

            PortGather = new CommunicationGather(Communication, ">");
            PortGather.LineReceived += new EventHandler<GenericCommMethodReceiveTextArgs>(PortGather_LineReceived);

            CommunicationMonitor = new GenericCommunicationMonitor(this, Communication, 15000, 180000, 300000, DoPoll);

            Init();
        }

        private void Init()
        {
            MicEnable = new Dictionary<int, bool>();
            MicEnableFeedback = new Dictionary<int, BoolFeedback>();

            MicStatus = new Dictionary<int, int>();
            MicStatusFeedback = new Dictionary<int, IntFeedback>();

            MicBatteryLevel = new Dictionary<int, int>();
            MicBatteryLevelFeedback = new Dictionary<int, IntFeedback>();

            MicLowBatteryCaution = new Dictionary<int, bool>();
            MicLowBatteryCautionFeedback = new Dictionary<int, BoolFeedback>();

            MicLowBatteryWarning = new Dictionary<int, bool>();
            MicLowBatteryWarningFeedback = new Dictionary<int, BoolFeedback>();

            MicLowBatteryStatus = new Dictionary<int,int>();
            MicLowBatteryStatusFeedback = new Dictionary<int, IntFeedback>();

            MicOnCharger = new Dictionary<int, bool>();
            MicOnChargerFeedback = new Dictionary<int, BoolFeedback>();

            MicNames = new Dictionary<int, string>();
            MicNamesFeedback = new Dictionary<int, StringFeedback>();

            foreach (var item in _Props.Mics)
            {
                var i = item;
                Debug.Console(2, this, "This Mic's name is {0}", i.name);

                MicStatus.Add(i.index, 0);
                MicStatusFeedback.Add(i.index, new IntFeedback(() => MicStatus[i.index]));

                MicBatteryLevel.Add(i.index, 0);
                MicBatteryLevelFeedback.Add(i.index, new IntFeedback(() => MicBatteryLevel[i.index]));

                MicLowBatteryCaution.Add(i.index, false);
                MicLowBatteryCautionFeedback.Add(i.index, new BoolFeedback(() => MicLowBatteryCaution[i.index]));

                MicLowBatteryWarning.Add(i.index, false);
                MicLowBatteryWarningFeedback.Add(i.index, new BoolFeedback(() => MicLowBatteryWarning[i.index]));

                MicLowBatteryStatus.Add(i.index, 0);
                MicLowBatteryStatusFeedback.Add(i.index, new IntFeedback(() => MicLowBatteryStatus[i.index]));

                MicOnCharger.Add(i.index, false);
                MicOnChargerFeedback.Add(i.index, new BoolFeedback(() => MicOnCharger[i.index]));

                MicNames.Add(i.index, i.name);
                MicNamesFeedback.Add(i.index, new StringFeedback(() => MicNames[i.index]));

                MicEnable.Add(i.index, false);
                MicEnableFeedback.Add(i.index, new BoolFeedback(() => MicEnable[i.index]));
            }

            Communication.Connect();
            CommunicationMonitor.Start();
        }

        void PortGather_LineReceived(object sender, GenericCommMethodReceiveTextArgs args)
        {
            if (Debug.Level == 2)
                Debug.Console(2, this, "RX: '{0}'", args.Text);
            try
            {
                var data = args.Text;

                //Is a Status Response
                if (data.Contains("REP"))
                {
                    var DataChunks = data.Split(' ');

                    var attribute = DataChunks[3];
                    var index = int.Parse(DataChunks[2]);

                    if (attribute == "TX_STATUS")
                    {
                        int Status = (int)Enum.Parse(typeof(Tx_Status), DataChunks[4], true);
                        MicStatus[index] = Status;
                        MicStatusFeedback[index].FireUpdate();
                        MicOnCharger[index] = (Status == (int)Tx_Status.ON_CHARGER ? true : false);
                        MicOnChargerFeedback[index].FireUpdate();
                        MicNamesFeedback[index].FireUpdate();


                    }
                    if (attribute == "BATT_CHARGE")
                    {
                        if (int.Parse(DataChunks[4]) != 255)
                        {
                            var Status = (int)((long.Parse(DataChunks[4]) * 65535) / 100);
                            MicBatteryLevel[index] = Status;
                            MicBatteryLevelFeedback[index].FireUpdate();
                            UpdateAlert(index);
                            MicNamesFeedback[index].FireUpdate();

                        }
                    }
                }
                foreach (var item in _Props.Mics)
                {
                    var i = item;
                    MicNamesFeedback[i.index].FireUpdate();
                    MicEnableFeedback[i.index].FireUpdate();
                }
            }
            catch (Exception e)
            {
                //Do Something
            }
        }

        private void UpdateAlert(int data)
        {
            if (MicStatus[data] == (int)Tx_Status.ON_CHARGER)
            {
                MicLowBatteryCaution[data] = false;
                MicLowBatteryWarning[data] = false;
                MicLowBatteryStatus[data] = 0; 
            }

            else if (MicStatus[data] != (int)Tx_Status.UNKNOWN)
            {
                if (MicBatteryLevel[data] <= WarningThreshold)
                {
                    MicLowBatteryWarning[data] = true;
                    MicLowBatteryCaution[data] = false;
                    MicLowBatteryStatus[data] = 2;

                }
                else if (MicBatteryLevel[data] <= CautionThreshold)
                {
                    MicLowBatteryWarning[data] = false;
                    MicLowBatteryCaution[data] = true;
                    MicLowBatteryStatus[data] = 1;
                }
                else
                {
                    MicLowBatteryCaution[data] = false;
                    MicLowBatteryWarning[data] = false;
                    MicLowBatteryStatus[data] = 0;
                }
            }

            MicLowBatteryCautionFeedback[data].FireUpdate();
            MicLowBatteryWarningFeedback[data].FireUpdate();
            MicLowBatteryStatusFeedback[data].FireUpdate();
            MicNamesFeedback[data].FireUpdate();
        }

        public void SetStatus(int data)
        {
            var parameter = "";

            switch (data)
            {
                case 1 :
                    parameter = "ACTIVE";
                    break;
                case 2:
                    parameter = "MUTE";
                    break;
                case 3:
                    parameter = "STANDBY";
                    break;
                default:
                    break;
            }
            if (!string.IsNullOrEmpty(parameter))
            {
                var cmd = string.Format("< SET 0 TX_STATUS {0} >", parameter);
                CommandManager(cmd);
            }
        }

        public void SetStatus(int index, int data)
        {
            var parameter = "";

            switch (data)
            {
                case 1:
                    parameter = "ACTIVE";
                    break;
                case 2:
                    parameter = "MUTE";
                    break;
                case 3:
                    parameter = "STANDBY";
                    break;
                default:
                    break;
            }
            if (!string.IsNullOrEmpty(parameter))
            {
                var cmd = string.Format("< SET {1} TX_STATUS {0} >", parameter, index);
                CommandManager(cmd);
            }
        }

        private void CommandManager(string text)
        {
            Debug.Console(2, this, "TX : '{0}'", text);
            Communication.SendText(text);
        }

        private void DoPoll()
        {
            PollStatus();
        }

        private void PollStatus()
        {
            var cmd = string.Format("< GET 0 TX_STATUS >");
            CommandManager(cmd);
            Poll = new CTimer(o => PollBatteryLevel(), null, 500);
        }

        private void PollBatteryLevel()
        {
            var cmd = string.Format("< GET 0 BATT_CHARGE >");
            CommandManager(cmd);
            
        }

        void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs ars)
        {
            
        }

        enum Tx_Status
        {
            ACTIVE = 1,
            MUTE = 2,
            STANDBY = 3,
            ON_CHARGER = 4,
            UNKNOWN = 5
        }


        #region IBridge Members

        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey)
        {
            this.LinkToApiExt(trilist, joinStart, joinMapKey);
        }

        #endregion
    }
}

