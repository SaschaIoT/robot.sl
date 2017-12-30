using robot.sl.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace robot.sl.Sensors
{
    public class DistanceMeasurementSensor
    {
        private SerialDevice _serialPort = null;
        private DataReader _dataReader = null;
        private GpioPin _startStopPin = null;
        public UltrasonicMeasureList UltrasonicMeasurements = new UltrasonicMeasureList();

        private volatile bool _isStopped = true;
        private volatile bool _isStopping = false;

        const int READ_SERIAL_TIMEOUT_MILLISECONDS = 600;
        const int MAX_READ_COUNT = 3;
        const string SERIAL_DEVICE_NAME = "uart2";
        const int SERIAL_DEVICE_GPIO_PIN = 1;
        const int MEASURMENTS_COUNT = 2;
        const int MEASUREMENT_LENGTH = 7;
        const string MEASUREMENT_END_CHARACTER = "\r";

        public async Task Initialize()
        {
            var gpioController = GpioController.GetDefault();
            _startStopPin = gpioController.OpenPin(SERIAL_DEVICE_GPIO_PIN);
            _startStopPin.SetDriveMode(GpioPinDriveMode.Output);
            _startStopPin.Write(GpioPinValue.Low);

            var serialDeviceSelector = SerialDevice.GetDeviceSelector();
            var serialDevices = (await DeviceInformation.FindAllAsync(serialDeviceSelector)).ToList();

            _serialPort = await SerialDevice.FromIdAsync(serialDevices.First(sd => sd.Id.ToLower().Contains(SERIAL_DEVICE_NAME)).Id);
            _serialPort.ReadTimeout = TimeSpan.Zero;
            _serialPort.BaudRate = 9600;
            _serialPort.Parity = SerialParity.None;
            _serialPort.StopBits = SerialStopBitCount.One;
            _serialPort.DataBits = 8;

            _dataReader = new DataReader(_serialPort.InputStream);
            _dataReader.InputStreamOptions = InputStreamOptions.Partial;
        }

        public async Task<int> GetDistanceInMillimeters()
        {
            var last = UltrasonicMeasurements.GetLast();

            while (last == null)
            {
                await Task.Delay(20);
                last = UltrasonicMeasurements.GetLast();
            }

            var oldId = last.Id;

            await Task.Delay(MEASURMENTS_COUNT * 100);

            var current = UltrasonicMeasurements.GetLast();
            while (current.Id == oldId)
            {
                await Task.Delay(20);
                current = UltrasonicMeasurements.GetLast();
            }

            return current.DistanceInMillimeter;
        }

        public void Start()
        {
            if (!_isStopped)
            {
                return;
            }

            _isStopped = false;

            Measure();
        }

        public async void Measure()
        {
            _startStopPin.Write(GpioPinValue.High);

            while (!_isStopping)
            {
                var ultrasonicMeasure = new Measurement();

                try
                {
                    var readCount = 0;
                    var readString = await ReadString(MEASUREMENT_LENGTH * MEASURMENTS_COUNT);
                    var measurementMatches = Regex.Matches(readString, "R[0-9]{4}\r");

                    while (measurementMatches.Count < MEASURMENTS_COUNT)
                    {
                        readString += await ReadString(MEASUREMENT_LENGTH);
                        measurementMatches = Regex.Matches(readString, "R[0-9]{4}\r");

                        readCount++;

                        if (readCount >= (MAX_READ_COUNT * MEASURMENTS_COUNT))
                        {
                            await Logger.Write($"{nameof(DistanceMeasurementSensor)}, {nameof(Measure)}: To many measurements. Maximum read count reached.");
                            SystemController.ShutdownApplication(true).Wait();
                        }
                    }

                    ultrasonicMeasure.DistanceInMillimeter = GetAverageDistance(measurementMatches);
                }
                catch (TaskCanceledException)
                {
                    await Logger.Write($"{nameof(DistanceMeasurementSensor)}, {nameof(Measure)}: Not receiving data from distance measurement sensor.");
                    SystemController.ShutdownApplication(true).Wait();
                }

                UltrasonicMeasurements.Add(ultrasonicMeasure);
                UltrasonicMeasurements.RemoveFirst();
            }

            _startStopPin.Write(GpioPinValue.Low);

            _isStopped = true;
        }

        public async Task Stop()
        {
            if (_isStopped)
            {
                return;
            }

            _isStopping = true;

            while (!_isStopped)
            {
                await Task.Delay(10);
            }

            _isStopping = false;
        }

        private async Task<string> ReadString(uint count)
        {
            var readBytes = await ReadBytes(count);
            var readString = Encoding.ASCII.GetString(readBytes) ?? string.Empty;

            return readString;
        }

        private async Task<byte[]> ReadBytes(uint count)
        {
            var readBytes = new List<byte>().ToArray();

            var source = new CancellationTokenSource();
            var bytesReadTask = _dataReader.LoadAsync(count).AsTask(source.Token);
            source.CancelAfter(READ_SERIAL_TIMEOUT_MILLISECONDS * MEASURMENTS_COUNT);

            var bytesRead = await bytesReadTask;
            if (bytesRead > 0)
            {
                readBytes = new byte[bytesRead];
                _dataReader.ReadBytes(readBytes);
            }

            return readBytes;
        }

        private int GetAverageDistance(MatchCollection distanceRawMatches)
        {
            var distance = 0;
            var distances = new List<int>();

            foreach (var distanceRawMatch in distanceRawMatches.Cast<Match>().Select(match => match.Value).ToList())
            {
                var distanceRaw = distanceRawMatch ?? string.Empty;

                if (int.TryParse(distanceRaw.Substring(1).Replace(MEASUREMENT_END_CHARACTER, string.Empty), out distance))
                {
                    distances.Add(distance);
                }
            }

            return Convert.ToInt32(distances.Average());
        }
    }

    public class Measurement
    {
        public Guid Id { get; } = Guid.NewGuid();
        public int DistanceInMillimeter { get; set; }
    }
}
