using robot.sl.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// <summary>
    /// Distance sensor ultrasonic: Maxbotix MB1013 HRLV-MaxSonar-EZ
    /// </summary>
    public class DistanceSensorUltrasonic
    {
        private SerialDevice _serialPort = null;
        private DataReader _dataReader = null;
        private GpioPin _startStopPin = null;
        public UltrasonicMeasureList UltrasonicMeasurements = new UltrasonicMeasureList();
        private List<int> _readings = new List<int>();

        private volatile bool _isStopped = true;
        private volatile bool _isStopping = false;

        const int READ_SERIAL_READ_COMPLETE_TIMEOUT_MILLISECONDS = 2000;
        const int READ_SERIAL_READ_TIMEOUT_MILLISECONDS = 500;
        const string SERIAL_DEVICE_NAME = "uart2";
        const int SERIAL_DEVICE_GPIO_PIN = 1;
        const int MEASURMENTS_COUNT = 1;
        const int MEASUREMENT_LENGTH = 8;
        const string MEASUREMENT_END_CHARACTER = "\r";
        const string MEASUREMEN_VALUE_REGEX = "R[0-9]{4}\r";
        const int SENSOR_START_UP_TIME_MILLISECONDS = 170;

        public async Task InitializeAsync()
        {
            var gpioController = GpioController.GetDefault();
            _startStopPin = gpioController.OpenPin(SERIAL_DEVICE_GPIO_PIN);
            _startStopPin.SetDriveMode(GpioPinDriveMode.Output);
            _startStopPin.Write(GpioPinValue.Low);

            var serialDeviceSelector = SerialDevice.GetDeviceSelector();
            var serialDevices = (await DeviceInformation.FindAllAsync(serialDeviceSelector)).ToList();

            _serialPort = await SerialDevice.FromIdAsync(serialDevices.First(sd => sd.Id.ToLower().Contains(SERIAL_DEVICE_NAME)).Id);
            //If data is present, wait if more data become available
            _serialPort.ReadTimeout = TimeSpan.FromMilliseconds(READ_SERIAL_READ_TIMEOUT_MILLISECONDS);
            _serialPort.BaudRate = 9600;
            _serialPort.Parity = SerialParity.None;
            _serialPort.StopBits = SerialStopBitCount.One;
            _serialPort.DataBits = 8;

            _dataReader = new DataReader(_serialPort.InputStream);
            _dataReader.InputStreamOptions = InputStreamOptions.Partial;
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

            await Task.Delay(SENSOR_START_UP_TIME_MILLISECONDS);

            while (!_isStopping)
            {
                var ultrasonicMeasure = new Measurement();

                try
                {
                    var readStart = new Stopwatch();
                    readStart.Start();

                    var readString = await ReadStringAsync(MEASUREMENT_LENGTH * MEASURMENTS_COUNT);
                    var measurementMatches = Regex.Matches(readString, MEASUREMEN_VALUE_REGEX);

                    while (measurementMatches.Count < MEASURMENTS_COUNT)
                    {
                        readString += await ReadStringAsync(MEASUREMENT_LENGTH);
                        measurementMatches = Regex.Matches(readString, MEASUREMEN_VALUE_REGEX);
                        
                        if (readStart.ElapsedMilliseconds >= (READ_SERIAL_READ_COMPLETE_TIMEOUT_MILLISECONDS * MEASURMENTS_COUNT))
                        {
                            await Logger.WriteAsync($"{nameof(DistanceSensorUltrasonic)}, {nameof(Measure)}: To many faulty measurements. Maximum read count reached.");
                            SystemController.ShutdownApplicationAsync(true).Wait();
                        }
                    }

                    ultrasonicMeasure.DistanceInMillimeter = ConvertToDistance(measurementMatches);
                }
                catch (TaskCanceledException)
                {
                    await Logger.WriteAsync($"{nameof(DistanceSensorUltrasonic)}, {nameof(Measure)}: Not receiving data from distance measurement sensor.");
                    SystemController.ShutdownApplicationAsync(true).Wait();
                }

                UltrasonicMeasurements.Add(ultrasonicMeasure);
                UltrasonicMeasurements.RemoveFirst();
            }

            _startStopPin.Write(GpioPinValue.Low);

            _isStopped = true;
        }

        public async Task StopAsync()
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

        private async Task<string> ReadStringAsync(uint count)
        {
            var readBytes = await ReadBytesAsync(count);
            var readString = Encoding.ASCII.GetString(readBytes) ?? string.Empty;

            return readString;
        }

        private async Task<byte[]> ReadBytesAsync(uint count)
        {
            var readBytes = new List<byte>().ToArray();

            var source = new CancellationTokenSource();
            var bytesReadTask = _dataReader.LoadAsync(count).AsTask(source.Token);
            source.CancelAfter(READ_SERIAL_READ_COMPLETE_TIMEOUT_MILLISECONDS * MEASURMENTS_COUNT);

            var bytesRead = await bytesReadTask;
            if (bytesRead > 0)
            {
                readBytes = new byte[bytesRead];
                _dataReader.ReadBytes(readBytes);
            }

            return readBytes;
        }

        private int ConvertToDistance(MatchCollection distanceRawMatches)
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

        /// <summary>
        /// Returns distance in millimeters
        /// Do not cancel operation, because after it sensor is in buggy state and does not work until power cycle
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetDistance()
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

        /// <summary>
        /// Returns distance in millimeters filtered
        /// Do not cancel operation, because after it sensor is in buggy state and does not work until power cycle
        /// </summary>
        /// <param name="filteredMeasurement"></param>
        /// <returns></returns>
        public async Task<int> GetDistanceFiltered(bool filteredMeasurement = true)
        {
            var distance = 0;
            if (filteredMeasurement == true)
            {
                if (_readings.Count >= 5)
                {
                    _readings.RemoveAt(0);
                }
                else
                {
                    while (_readings.Count < 5)
                    {
                        distance = await GetDistance();
                        _readings.Add(distance);
                    }
                }

                distance = await GetDistance();
                _readings.Add(distance);

                distance = Convert.ToInt32(_readings.Average());
            }
            else
            {
                distance = await GetDistance();
            }

            return distance;
        }

        public void ClearDistancesFiltered()
        {
            _readings.Clear();
        }
    }

    public class Measurement
    {
        public Guid Id { get; } = Guid.NewGuid();
        public int DistanceInMillimeter { get; set; }
    }
}