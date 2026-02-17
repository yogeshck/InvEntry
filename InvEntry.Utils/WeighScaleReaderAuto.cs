using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace InvEntry.Utils
{
    public class WeighScaleReaderAuto : IDisposable
    {
        private SerialPort _port;
        private CancellationTokenSource _cts;

        // Event for auto mode
        public event Action<decimal> WeightCaptured;

        /// <summary>
        /// Start listening continuously (Auto Mode).
        /// </summary>
        public void StartAuto(string comPort = "COM3")
        {
            _port = new SerialPort(comPort, 4800, Parity.None, 8, StopBits.One);
            _port.DataReceived += Port_DataReceived;
            _port.Open();
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = _port.ReadLine(); // synchronous, but triggered by event
                decimal weight = ParseWeight(data);
                WeightCaptured?.Invoke(weight);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scale read error: {ex.Message}");
            }
        }

        /// <summary>
        /// Await one stable weight (Manual Mode).
        /// </summary>
        public async Task<decimal> StartManualAsync(string comPort = "COM3")
        {
            try
            {
                _port = new SerialPort(comPort, 4800, Parity.None, 8, StopBits.One);
                _port.Open();
            }
            catch
            {
                return -1;
            }

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

        private decimal ParseWeight(string raw)
        {
            string cleaned = raw.Trim();
            if (cleaned.StartsWith("ST")) cleaned = cleaned.Substring(2).Trim();
            if (cleaned.StartsWith("+")) cleaned = cleaned.Substring(1);
            cleaned = cleaned.Replace("g", "").Trim();
            return decimal.Parse(cleaned, System.Globalization.CultureInfo.InvariantCulture);
        }

        public void Stop()
        {
            _cts?.Cancel();
            if (_port != null)
            {
                if (_port.IsOpen) _port.Close();
                _port.Dispose();
                _port = null;
            }
        }

        public void Dispose() => Stop();
    }
}