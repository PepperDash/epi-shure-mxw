using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using Newtonsoft.Json;
using PepperDash.Essentials.Core.Config;
using Crestron.SimplSharpPro.DeviceSupport;
using ShureWireless.Bridge.JoinMap;
using ShureWireless.Common;


namespace ShureWireless.Mxw
{
    public class ShureMxwDevice : EssentialsBridgeableDevice
    {
        public ShureMicProperties Props { get; private set; }

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

        private string _error;

        public string Error
        {
            get { return _error; }
            set
            {
                _error = value;
                ErrorFeedback.FireUpdate();
            }
        }

        public StringFeedback ErrorFeedback;

        public IBasicCommunication Communication { get; private set; }
        public CommunicationGather PortGather { get; private set; }
        public GenericCommunicationMonitor CommunicationMonitor { get; private set; }
        private long _cautionThreshold;

        int CautionThreshold
        {
            get
            {
                return (int)((_cautionThreshold * 65535) / 100);
            }
            set
            {
                _cautionThreshold = value;
            }
        }

        private long _warningThreshold;

        int WarningThreshold
        {
            get
            {
                return (int)((_warningThreshold * 65535) / 100);
            }
            set
            {
                _warningThreshold = value;
            }
        }


        public ShureMxwDevice(string key, string name, IBasicCommunication comm, DeviceConfig dc)
            : base(key, name)
        {
            Name = name;
            
            Props = JsonConvert.DeserializeObject<ShureMicProperties>(dc.Properties.ToString());

            CautionThreshold = Props.CautionThreshold;
            WarningThreshold = Props.WarningThreshold;

            Debug.Console(1, this, "Made it to consturctor for ShureMxw {0}", Name);
            Debug.Console(2, this, "ShureMxw Properties : {0}", dc.Properties.ToString());

            Communication = comm;
            PortGather = new CommunicationGather(Communication, ">");
            PortGather.LineReceived += PortGather_LineReceived;

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

            ErrorFeedback = new StringFeedback(() => Error);

            foreach (var item in Props.Mics)
            {
                var i = item;
                Debug.Console(2, this, "This Mic's name is {0}", i.Name);

                MicStatus.Add(i.Index, 0);
                MicStatusFeedback.Add(i.Index, new IntFeedback(() => MicStatus[i.Index]));

                MicBatteryLevel.Add(i.Index, 0);
                MicBatteryLevelFeedback.Add(i.Index, new IntFeedback(() => MicBatteryLevel[i.Index]));

                MicLowBatteryCaution.Add(i.Index, false);
                MicLowBatteryCautionFeedback.Add(i.Index, new BoolFeedback(() => MicLowBatteryCaution[i.Index]));

                MicLowBatteryWarning.Add(i.Index, false);
                MicLowBatteryWarningFeedback.Add(i.Index, new BoolFeedback(() => MicLowBatteryWarning[i.Index]));

                MicLowBatteryStatus.Add(i.Index, 0);
                MicLowBatteryStatusFeedback.Add(i.Index, new IntFeedback(() => MicLowBatteryStatus[i.Index]));

                MicOnCharger.Add(i.Index, false);
                MicOnChargerFeedback.Add(i.Index, new BoolFeedback(() => MicOnCharger[i.Index]));

                MicNames.Add(i.Index, i.Name);
                MicNamesFeedback.Add(i.Index, new StringFeedback(() => MicNames[i.Index]));

                MicEnable.Add(i.Index, i.Enabled);
                MicEnableFeedback.Add(i.Index, new BoolFeedback(() => MicEnable[i.Index]));
            }

            Communication.Connect();
            CommunicationMonitor.Start();
        }

        void PortGather_LineReceived(object sender, GenericCommMethodReceiveTextArgs args)
        {
            Debug.Console(2, this, "RX: '{0}'", args.Text);
            try
            {
                var data = args.Text;

                ParseResponseData(data);
            }
            catch (Exception e)
            {
                Debug.Console(0, this, "Exception in PortGather : {0}", e.Message);
            }
        }

        private void ParseResponseData(string data)
        {
            if (data.Contains("REP"))
            {
                var dataChunks = data.Split(' ');

                var attribute = dataChunks[3];
                var index = int.Parse(dataChunks[2]);

                if (attribute == "TX_STATUS")
                {
                    var status = (int) Enum.Parse(typeof (Tx_Status), dataChunks[4], true);
                    MicStatus[index] = status;
                    MicStatusFeedback[index].FireUpdate();
                    MicOnCharger[index] = (status == (int) Tx_Status.OnCharger);
                    MicOnChargerFeedback[index].FireUpdate();
                    MicNamesFeedback[index].FireUpdate();
                }
                if (attribute == "BATT_CHARGE")
                {
                    if (int.Parse(dataChunks[4]) != 255)
                    {
                        var status = (int) ((long.Parse(dataChunks[4])*65535)/100);
                        MicBatteryLevel[index] = status;
                        MicBatteryLevelFeedback[index].FireUpdate();
                        UpdateAlert(index);
                        MicNamesFeedback[index].FireUpdate();
                    }
                }
            }
            foreach (var item in Props.Mics)
            {
                var i = item;
                MicNamesFeedback[i.Index].FireUpdate();
                MicEnableFeedback[i.Index].FireUpdate();
            }

            CheckStatusConditions();
        }


        private void CheckStatusConditions()
        {
            var errorCode = 0;
            var errorStatus = "";

            foreach (var mic in MicEnable)
            {
                var index = mic.Key;
                var caution = MicLowBatteryCaution[index];
                var warning = MicLowBatteryWarning[index];
                var charging = MicOnCharger[index];
                var micName = MicNames[index];
                var cautionThreshold = CautionThreshold;
                var warningThreshold = WarningThreshold;

                if (errorStatus.Length > 0)
                {
                    errorStatus += "| ";
                }

                if (caution && !warning)
                {
                    errorStatus += String.Format("{0} - {1} - Mic Level < {2}% and{3} Charging", Name, micName, cautionThreshold, 
                        charging ? "" : " not");
                    if (errorCode < 1)
                    {
                        errorCode = 1;
                    }
                }

                else if (warning && !caution)
                {
                    errorStatus += String.Format("{0} - {1} - Mic Level < {2}% and{3} Charging", Name, micName, warningThreshold,
                        charging ? "" : " not");
                    if (errorCode < 2)
                    {
                        errorCode = 2;
                    }
                }
            }

            if (errorCode == 0)
            {
                errorStatus = String.Format("{0} : {1} - Mics Okay");
            }
        }

        private void UpdateAlert(int data)
        {
            if (MicStatus[data] == (int)Tx_Status.OnCharger)
            {
                MicLowBatteryCaution[data] = false;
                MicLowBatteryWarning[data] = false;
                MicLowBatteryStatus[data] = 0; 
            }

            else if (MicStatus[data] != (int)Tx_Status.Unknown)
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
            }
            if (string.IsNullOrEmpty(parameter)) return;
            var cmd = string.Format("< SET 0 TX_STATUS {0} >", parameter);
            CommandManager(cmd);
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
            }
            if (string.IsNullOrEmpty(parameter)) return;
            var cmd = string.Format("< SET {1} TX_STATUS {0} >", parameter, index);
            CommandManager(cmd);
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


        enum Tx_Status
        {
            Active = 1,
            Mute = 2,
            Standby = 3,
            OnCharger = 4,
            Unknown = 5
        }



        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, PepperDash.Essentials.Core.Bridges.EiscApiAdvanced bridge)
        {
            var joinMap = new ShureWirlessMicJoinmap(joinStart);

            Debug.Console(1, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(2, this, "There are {0} Mics", Props.Mics.Count());

            CommunicationMonitor.IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            Debug.Console(2, this, "Linked Online at {0}", joinMap.IsOnline);

            ErrorFeedback.LinkInputSig(trilist.StringInput[joinMap.ErrorString.JoinNumber]);


            foreach (var item in Props.Mics)
            {
                var i = item;
                var offset = (uint)((i.Index - 1) * 4);

                Debug.Console(2, this, "Mic Channel {0} Connect", i.Index);

                trilist.BooleanInput[(joinMap.Enabled.JoinNumber + offset)].BoolValue = i.Enabled;
                Debug.Console(2, this, "Linked Mic {0} Enabled at {1}", i.Index, joinMap.Enabled.JoinNumber + offset);
                MicEnableFeedback[i.Index].LinkInputSig(trilist.BooleanInput[joinMap.Enabled.JoinNumber + offset]);

                trilist.StringInput[(joinMap.Name.JoinNumber + offset)].StringValue = MicNames[i.Index];
                Debug.Console(2, this, "Linked Mic {0} Name at {1}", i.Index, joinMap.Name.JoinNumber + offset);
                MicNamesFeedback[i.Index].LinkInputSig(trilist.StringInput[joinMap.Name.JoinNumber + offset]);

                MicBatteryLevelFeedback[i.Index].LinkInputSig(trilist.UShortInput[joinMap.BatteryLevel.JoinNumber + offset]);
                Debug.Console(2, this, "Linked Mic {0} Battery Level Feedback at {1}", i.Index, joinMap.BatteryLevel.JoinNumber + offset);

                MicStatusFeedback[i.Index].LinkInputSig(trilist.UShortInput[joinMap.LocalStatus.JoinNumber + offset]);
                Debug.Console(2, this, "Linked Mic {0} Status Feedback at {1}", i.Index, joinMap.LocalStatus.JoinNumber + offset);

                MicLowBatteryStatusFeedback[i.Index].LinkInputSig(trilist.UShortInput[joinMap.BatteryStatus.JoinNumber + offset]);
                Debug.Console(2, this, "Linked Mic {0} Battery Status Feedback at {1}", i.Index, joinMap.BatteryStatus.JoinNumber + offset);

                MicLowBatteryCautionFeedback[i.Index].LinkInputSig(trilist.BooleanInput[joinMap.LowBatteryCaution.JoinNumber + offset]);
                Debug.Console(2, this, "Linked Mic {0} Battery Caution Feedback at {1}", i.Index, joinMap.LowBatteryCaution.JoinNumber + offset);

                MicLowBatteryWarningFeedback[i.Index].LinkInputSig(trilist.BooleanInput[joinMap.LowBatteryWarning.JoinNumber + offset]);
                Debug.Console(2, this, "Linked Mic {0} Battery Warning Feedback at {1}", i.Index, joinMap.LowBatteryWarning.JoinNumber + offset);

                MicOnChargerFeedback[i.Index].LinkInputSig(trilist.BooleanInput[joinMap.OnCharger.JoinNumber + offset]);
                Debug.Console(2, this, "Linked Mic {0} On Charger Feedback at {1}", i.Index, joinMap.OnCharger.JoinNumber + offset);



            }

            trilist.OnlineStatusChange += (d, args) =>
            {
                if (!args.DeviceOnLine) return;
                foreach (var item in MicNamesFeedback)
                {
                    item.Value.FireUpdate();
                }

                foreach (var item in MicEnableFeedback)
                {
                    item.Value.FireUpdate();
                }
            };
        }
    }
}

