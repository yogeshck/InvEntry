using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace InvEntry.Utils
{
    public class WeighScaleReader
    {
        private SerialPort _port;
        private CancellationTokenSource _cts;

        public event Action<decimal> WeightCaptured;

        public async Task<decimal> StartScaleAsync(string comPort = "COM3")
        {
            _port = new SerialPort(comPort, 4800, Parity.None, 8, StopBits.One);
            _port.Open();

            // Run the read loop in background
            return await Task.Run(() =>
            {
                while (true)
                {
                    string data = _port.ReadLine(); // e.g. "+000222.75  g\r"

                    if (!string.IsNullOrWhiteSpace(data))
                    {
                        // Some scales prefix with ST, others just send formatted data
                        if (data.StartsWith("ST") || data.Contains("g"))
                        {
                            decimal weight = ParseWeight(data);

                            // Close immediately after first stable capture
                            Stop();
                            return weight;
                        }
                    }
                }
            });
        }

        public void Stop()
        {
            if (_port != null)
            {
                if (_port.IsOpen) _port.Close();
                _port.Dispose();
                _port = null;
            }
        }

        private decimal ParseWeight(string raw)
        {
            string cleaned = raw.Trim();
            if (cleaned.StartsWith("ST")) cleaned = cleaned.Substring(2).Trim();
            if (cleaned.StartsWith("+")) cleaned = cleaned.Substring(1);
            cleaned = cleaned.Replace("g", "").Trim();
            return decimal.Parse(cleaned, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}

