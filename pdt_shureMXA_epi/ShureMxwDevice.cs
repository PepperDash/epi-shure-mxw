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

        public List<IntFeedback> MicStatusFeedback;
        public List<int> MicStatus;

        public List<IntFeedback> MicBatteryLevelFeedback;
        public List<int> MicBatteryLevel;

        public List<BoolFeedback> MicLowBatteryCautionFeedback;
        public List<bool> MicLowBatteryCaution;

        public List<BoolFeedback> MicLowBatteryWarningFeedback;
        public List<bool> MicLowBatteryWarning;

        public List<IntFeedback> MicLowBatteryStatusFeedback;
        public List<int> MicLowBatteryStatus;

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
        }

        public override bool CustomActivate()
        {
            

            MicStatus = new List<int>();
            MicStatusFeedback = new List<IntFeedback>();

            MicBatteryLevel = new List<int>();
            MicBatteryLevelFeedback = new List<IntFeedback>();

            MicLowBatteryCaution = new List<bool>();
            MicLowBatteryCautionFeedback = new List<BoolFeedback>();

            MicLowBatteryWarning = new List<bool>();
            MicLowBatteryWarningFeedback = new List<BoolFeedback>();

            MicLowBatteryStatus = new List<int>();
            MicLowBatteryStatusFeedback = new List<IntFeedback>();

            MicStatus.Add(0);
            MicStatusFeedback.Add(new IntFeedback(() => MicStatus[0]));

            MicBatteryLevel.Add(0);
            MicBatteryLevelFeedback.Add(new IntFeedback(() => MicBatteryLevel[0]));

            MicLowBatteryCaution.Add(false);
            MicLowBatteryCautionFeedback.Add(new BoolFeedback(() => MicLowBatteryCaution[0]));

            MicLowBatteryWarning.Add(false);
            MicLowBatteryWarningFeedback.Add(new BoolFeedback(() => MicLowBatteryWarning[0]));

            MicLowBatteryStatus.Add(0);
            MicLowBatteryStatusFeedback.Add(new IntFeedback(() => MicLowBatteryStatus[0]));

            foreach (var item in _Props.Mics)
            {
                var i = item;
                Debug.Console(2, this, "This Mic's name is {0}", i.name);

                MicStatus.Insert(i.index, 0);
                MicStatusFeedback.Insert(i.index, new IntFeedback(() => MicStatus[i.index]));

                MicBatteryLevel.Insert(i.index, 0);
                MicBatteryLevelFeedback.Insert(i.index, new IntFeedback(() => MicBatteryLevel[i.index]));

                MicLowBatteryCaution.Insert(i.index, false);
                MicLowBatteryCautionFeedback.Insert(i.index, new BoolFeedback(() => MicLowBatteryCaution[i.index]));

                MicLowBatteryWarning.Insert(i.index, false);
                MicLowBatteryWarningFeedback.Insert(i.index, new BoolFeedback(() => MicLowBatteryWarning[i.index]));

                MicLowBatteryStatus.Insert(i.index, 0);
                MicLowBatteryStatusFeedback.Insert(i.index, new IntFeedback(() => MicLowBatteryStatus[i.index]));
            }

            Communication.Connect();
            CommunicationMonitor.Start();

            return true;
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
                        MicStatus.Insert(index, Status);
                        MicStatusFeedback[index].FireUpdate();
                    }
                    if (attribute == "BATT_CHARGE")
                    {
                        if (int.Parse(DataChunks[4]) != 255)
                        {
                            var Status = (int)((long.Parse(DataChunks[4]) * 65535) / 100);
                            MicBatteryLevel.Insert(index, Status);
                            MicBatteryLevelFeedback[index].FireUpdate();
                            UpdateAlert(index);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //Do Something
            }
        }

        private void UpdateAlert(int data)
        {
            if (MicStatus[data] != 4 && MicStatus[data] != 5)
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
                else {
                    MicLowBatteryCaution[data] = false;
                    MicLowBatteryWarning[data] = false;
                    MicLowBatteryStatus[data] = 0;
                }

                MicLowBatteryCautionFeedback[data].FireUpdate();
                MicLowBatteryWarningFeedback[data].FireUpdate();
                MicLowBatteryStatusFeedback[data].FireUpdate();

            }
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

