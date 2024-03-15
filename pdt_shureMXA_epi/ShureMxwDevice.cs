using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Newtonsoft.Json.Linq;
using pdt_shureMXW_epi.Bridge.JoinMap;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using Newtonsoft.Json;
using PepperDash.Essentials.Core.Config;
using Crestron.SimplSharpPro.DeviceSupport;


namespace pdt_shureMXW_epi
{
    public class ShureMxwDevice : EssentialsBridgeableDevice, IPrivacy
    {
        public Properties Props { get; private set; }

        public readonly List<Mic> Mics;
        public Dictionary<int, bool> MicEnable;
        public Dictionary<int, BoolFeedback> MicEnableFeedback;

        public Dictionary<int, IntFeedback> MicStatusFeedback;
        public Dictionary<int, int> MicStatus;

        public Dictionary<int, IntFeedback> MicBatteryLevelPercentageFeedback;
        public Dictionary<int, int> MicBatteryLevelPercentage;

        public Dictionary<int, IntFeedback> MicBatteryLevelRawFeedback;
        public Dictionary<int, int> MicBatteryLevelRaw;

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

        public Dictionary<int, BoolFeedback> MicMuteFeedback;
        public Dictionary<int, bool> MicMute;

        public Dictionary<int, BoolFeedback> MicPressFeedback;
        public Dictionary<int, bool> MicPress;

        public BoolFeedback MicAnyPressFeedback;

        public StringFeedback NameFeedback;

        private bool _lastPrivacySet = false;



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
        public BoolFeedback PrivacyModeIsOnFeedback { get; private set; }


        public IBasicCommunication Communication { get; private set; }
        public CommunicationGather PortGather { get; private set; }
        public GenericCommunicationMonitor CommunicationMonitor { get; private set; }

        int CautionThreshold { get; set; }

        int WarningThreshold { get; set; }


        public ShureMxwDevice(string key, string name, IBasicCommunication comm, Properties dc)
            : base(key, name)
        {
            var dc1 = dc;
            Name = name;

            var mics = dc.Mics.Select(mic => new Mic(mic.Key, mic.Value)).ToList();

            Props = dc;

            Mics = mics;


            CautionThreshold = Props.CautionThreshold;
            WarningThreshold = Props.WarningThreshold;


            Communication = comm;
            var socket = comm as ISocketStatus;
            if (socket != null)
            {
                socket.ConnectionChange += socket_ConnectionChange;
            }

            PortGather = new CommunicationGather(Communication, ">");
            PortGather.LineReceived += LineReceived;

            CommunicationMonitor = new GenericCommunicationMonitor(this, Communication, 15000, 180000, 300000, DoPoll);

            Init();
        }

        private void Init()
        {
            MicEnable = new Dictionary<int, bool>();
            MicEnableFeedback = new Dictionary<int, BoolFeedback>();

            MicStatus = new Dictionary<int, int>();
            MicStatusFeedback = new Dictionary<int, IntFeedback>();

            MicBatteryLevelPercentage = new Dictionary<int, int>();
            MicBatteryLevelPercentageFeedback = new Dictionary<int, IntFeedback>();

            MicBatteryLevelRaw = new Dictionary<int, int>();
            MicBatteryLevelRawFeedback = new Dictionary<int, IntFeedback>();

            MicLowBatteryCaution = new Dictionary<int, bool>();
            MicLowBatteryCautionFeedback = new Dictionary<int, BoolFeedback>();

            MicLowBatteryWarning = new Dictionary<int, bool>();
            MicLowBatteryWarningFeedback = new Dictionary<int, BoolFeedback>();

            MicLowBatteryStatus = new Dictionary<int,int>();
            MicLowBatteryStatusFeedback = new Dictionary<int, IntFeedback>();

            MicOnCharger = new Dictionary<int, bool>();
            MicOnChargerFeedback = new Dictionary<int, BoolFeedback>();

            MicMute = new Dictionary<int, bool>();
            MicMuteFeedback = new Dictionary<int, BoolFeedback>();

            MicNames = new Dictionary<int, string>();
            MicNamesFeedback = new Dictionary<int, StringFeedback>();

            MicPress = new Dictionary<int, bool>();
            MicPressFeedback = new Dictionary<int, BoolFeedback>();

            ErrorFeedback = new StringFeedback(() => Error);
            NameFeedback = new StringFeedback(() => Name);

            foreach (var item in Mics)
            {
                var i = item;
                Debug.Console(2, this, "This Mic's name is {0}", i.Name);

                MicStatus.Add(i.Index, 0);
                MicStatusFeedback.Add(i.Index, new IntFeedback(() => MicStatus[i.Index]));

                MicBatteryLevelPercentage.Add(i.Index, 0);
                MicBatteryLevelPercentageFeedback.Add(i.Index, new IntFeedback(() => MicBatteryLevelPercentage[i.Index]));

                MicBatteryLevelRaw.Add(i.Index, 0);
                MicBatteryLevelRawFeedback.Add(i.Index, new IntFeedback(() => MicBatteryLevelRaw[i.Index]));

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

                MicMute.Add(i.Index, i.Enabled);
                MicMuteFeedback.Add(i.Index, new BoolFeedback(() => MicMute[i.Index]));

                MicPress.Add(i.Index, false);
                MicPressFeedback.Add(i.Index, new BoolFeedback(() => MicPress[i.Index]));
            }

            PrivacyModeIsOnFeedback = new BoolFeedback(() => MicMuteFeedback.Values.All(value => value.BoolValue));
            MicAnyPressFeedback = new BoolFeedback(() => MicPressFeedback.Values.Any(value => value.BoolValue));

            
        }

        #region Overrides of Device

        public override void Initialize()
        {
            Communication.Connect();
            CommunicationMonitor.Start();
        }

        #endregion

        void LineReceived(object sender, GenericCommMethodReceiveTextArgs args)
        {
            //Debug.Console(2, this, "RX: '{0}'", args.Text);
            try
            {
                var data = args.Text;

                //Is a Status Response
                if (data.Contains("REP") && (!data.Contains(" SEC ")))
                {
                    var dataChunks = data.Split(' ');


                    var attribute = dataChunks[3];
                    var index = int.Parse(dataChunks[2]);

                    if (Props.Mics.All(kvp => kvp.Value.Index != index))
                    {
                        Debug.Console(1, this, "Not configure for mic at index {0}", index);
                        return;
                    }

                    if (attribute == "TX_STATUS")
                    {
                        var status = (int)Enum.Parse(typeof(Tx_Status), dataChunks[4].TrimEnd().TrimStart(), true);
                        MicStatus[index] = status;
                        MicStatusFeedback[index].FireUpdate();
                        MicOnCharger[index] = (status == (int)Tx_Status.ON_CHARGER);
                        MicOnChargerFeedback[index].FireUpdate();
                        MicMute[index] = _lastPrivacySet ? status != (int)Tx_Status.ACTIVE : status == (int)Tx_Status.MUTE;
                        MicMuteFeedback[index].FireUpdate();
                        MicNamesFeedback[index].FireUpdate();
                        PrivacyModeIsOnFeedback.FireUpdate();
                    }
                    if (attribute == "BATT_CHARGE")
                    {
                        if (int.Parse(dataChunks[4]) != 255)
                        {
                            var status = (int)((long.Parse(dataChunks[4]) * 65535) / 100);
                            MicBatteryLevelRaw[index] = status;
                            MicBatteryLevelRawFeedback[index].FireUpdate();

                            MicBatteryLevelPercentage[index] = int.Parse(dataChunks[4]);
                            MicBatteryLevelPercentageFeedback[index].FireUpdate();
                            UpdateAlert(index);
                            MicNamesFeedback[index].FireUpdate();

                        }
                    }

                    if (attribute == "BUTTON_STS")
                    {
                        var buttonStatus = dataChunks[4] == "ON";
                        MicPress[index] = buttonStatus;
                        MicPressFeedback[index].FireUpdate();
                        MicAnyPressFeedback.FireUpdate();
                    }
                }
                foreach (var item in Mics)
                {
                    var i = item;
                    MicNamesFeedback[i.Index].FireUpdate();
                    MicEnableFeedback[i.Index].FireUpdate();
                }

                CheckStatusConditions();
            }
            catch (Exception e)
            {
                Debug.Console(1, this,Debug.ErrorLogLevel.Error, "Exception in LineReceived : {0}", e.Message);
            }
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
                errorStatus = String.Format("{0}  - Mics Okay", Name);
            }
        }

        private void UpdateAlert(int data)
        {
            if (MicStatus[data] == (int)Tx_Status.ON_CHARGER)
            {
                MicLowBatteryCaution[data] = false;
                MicLowBatteryWarning[data] = false;
                MicLowBatteryStatus[data] = 0;
                return;
            }

            if (MicStatus[data] == (int) Tx_Status.UNKNOWN)
            {
                return;
            }
            
            Debug.Console(1, this, "Battery Percentage: {0} | Warning Threshold: {1} | Caution Threshold: {2}", MicBatteryLevelPercentage[data], WarningThreshold, CautionThreshold);

            if (MicBatteryLevelPercentage[data] <= WarningThreshold)
            {
                MicLowBatteryWarning[data] = true;
                MicLowBatteryCaution[data] = false;
                MicLowBatteryStatus[data] = 2;

            }
            else if (MicBatteryLevelPercentage[data] <= CautionThreshold)
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

        private enum Tx_Status
        {
            UNKNOWN = 0,
            ACTIVE = 1,
            MUTE = 2,
            STANDBY = 3,
            ON_CHARGER = 4,
        }

        private List<Mic> ListConvert(Dictionary<string, MicDict> dict)
        {
            return (from outletDict in dict let key = outletDict.Key let value = outletDict.Value select new Mic(key, value)).ToList();
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, PepperDash.Essentials.Core.Bridges.EiscApiAdvanced bridge)
        {
            var joinMap = new ShureMxwDeviceJoinMap(joinStart);

            Debug.Console(0, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, this, "There are {0} Mic", Mics.Count());

            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }

            CommunicationMonitor.IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            Debug.Console(2, this, "Linked Online at {0}", joinMap.IsOnline);

            ErrorFeedback.LinkInputSig(trilist.StringInput[joinMap.ErrorString.JoinNumber]);

            NameFeedback.LinkInputSig(trilist.StringInput[joinMap.DeviceName.JoinNumber]);

            MicAnyPressFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AnyPress.JoinNumber]);


            foreach (var item in Mics)
            {
                var i = item;
                var offset = (uint)((i.Index - 1) * 5);

                Debug.Console(2, this, "Mic Channel {0} Connect", i.Index);

                trilist.BooleanInput[(joinMap.Enabled.JoinNumber + offset)].BoolValue = i.Enabled;
                Debug.Console(2, this, "Linked Mic {0} Enabled at {1}", i.Index, joinMap.Enabled.JoinNumber + offset);
                MicEnableFeedback[i.Index].LinkInputSig(trilist.BooleanInput[joinMap.Enabled.JoinNumber + offset]);

                trilist.StringInput[(joinMap.Name.JoinNumber + offset)].StringValue = this.MicNames[i.Index];
                Debug.Console(2, this, "Linked Mic {0} Name at {1}", i.Index, joinMap.Name.JoinNumber + offset);
                MicNamesFeedback[i.Index].LinkInputSig(trilist.StringInput[joinMap.Name.JoinNumber + offset]);

                MicBatteryLevelPercentageFeedback[i.Index].LinkInputSig(trilist.UShortInput[joinMap.BatteryLevelPercentage.JoinNumber + offset]);
                Debug.Console(2, this, "Linked Mic {0} Battery Level Feedback at {1}", i.Index, joinMap.BatteryLevelPercentage.JoinNumber + offset);

                MicBatteryLevelRawFeedback[i.Index].LinkInputSig(trilist.UShortInput[joinMap.BatteryLevelRaw.JoinNumber + offset]);
                Debug.Console(2, this, "Linked Mic {0} Battery Level Feedback at {1}", i.Index, joinMap.BatteryLevelPercentage.JoinNumber + offset);

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

                trilist.SetSigTrueAction((uint)joinMap.SetAllMicsToMute.JoinNumber + offset, () => SetStatus(2));
                trilist.SetSigTrueAction((uint)joinMap.SetAllMicsToActive.JoinNumber + offset, () => SetStatus(1));

                MicOnChargerFeedback[i.Index].LinkInputSig(trilist.BooleanInput[joinMap.OnCharger.JoinNumber + offset]);
                Debug.Console(2, this, "Linked Mic {0} On Charger Feedback at {1}", i.Index, joinMap.OnCharger.JoinNumber + offset);

                MicEnableFeedback[i.Index].LinkInputSig(trilist.BooleanInput[joinMap.OnChargerFbEnable.JoinNumber + offset]);

            }

            trilist.OnlineStatusChange += (d, args) =>
            {
                if (!args.DeviceOnLine) return;
                ErrorFeedback.FireUpdate();
                NameFeedback.FireUpdate();
                MicAnyPressFeedback.FireUpdate();
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

        #region IPrivacy Members


        public void PrivacyModeOff()
        {
            for (var i = 0; i < MicMute.Count; i++)
            {
                var cmd = string.Format("< SET {0} TX_STATUS ACTIVE>", i + 1);
                CommandManager(cmd);
            }
            _lastPrivacySet = false;
        }

        public void PrivacyModeOn()
        {
            for (var i = 0; i < MicMute.Count; i++)
            {
                var cmd = string.Format("< SET {0} TX_STATUS MUTE>", i + 1);
                CommandManager(cmd);
            }
            _lastPrivacySet = true;
        }

        public void PrivacyModeToggle()
        {
            if (PrivacyModeIsOnFeedback.BoolValue)
            {
                PrivacyModeOff();
                return;
            }
            PrivacyModeOn();
        }

        #endregion
    }
}

