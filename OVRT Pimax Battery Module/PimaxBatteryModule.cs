using System;
using System.IO;
using OVRToolkit.Modules;

namespace OVRT_PimaxBatteryModule
{
    public class PimaxBatteryModule : Module
    {
        const int readChunk = 8192; // number of bytes to read at the end of the file
        private long lastUpdate = -1;
        private string logFile;

        private bool isAdded = false;

        public override void Start()
        {
            logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PimaxClient\\logs\\main.log");
        }

        public override void Update()
        {
            long time = DateTimeOffset.Now.ToUnixTimeSeconds();
            if (time > lastUpdate + 10)//update every 10 seconds; that's how often pimax prints to its logs
            {
                lastUpdate = time;
                if (!File.Exists(logFile)) return;
                if (!isAdded)
                {
                    isAdded = true;
                    AddCustomBattery("pimax_hmd", 0, DeviceBatteryStates.Unknown, DeviceBatteryIcons.HMD, new byte[0]);
                }
                float battery = 0;
                DeviceBatteryStates state = DeviceBatteryStates.Unknown;
                using (var reader = new StreamReader(logFile))
                {
                    if(reader.BaseStream.Length > readChunk)
                    {
                        reader.BaseStream.Seek(-readChunk, SeekOrigin.End);
                    }
                    string line;
                    while((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if(line.StartsWith("hmd: "))
                        {
                            battery = float.Parse(line.Substring(4, line.Length - 5).Trim())/100;
                        }
                        if (line.Equals("hmdchargeing4p3b: false"))
                        {
                            state = DeviceBatteryStates.Connected;
                        }
                        if (line.Equals("hmdchargeing4p3b: true"))
                        {
                            state = DeviceBatteryStates.Charging;
                        }
                    }
                }
                if (battery == 0 && state != DeviceBatteryStates.Unknown) state = DeviceBatteryStates.Disconnected;
                UpdateCustomBattery("pimax_hmd", battery, state);
            }
        }
    }
}
