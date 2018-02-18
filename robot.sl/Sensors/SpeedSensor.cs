using robot.sl.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace robot.sl.Sensors
{
    /// <summary>
    /// Speed sensor: LM393 FC-03
    /// </summary>
    public static class SpeedSensor
    {
        public static int RoundsPerMinute { get; private set; }
        public static double KilometerPerHour { get; private set; }
        public static bool IsDriving
        {
            get
            {
                return RoundsPerMinute >= 1;
            }
        }

        private const int FALLS_DOWNS_ONE_ROUND = 40;
        private const double ROUNDS_ONE_KILOMETER = 5250;
        private const double FALLS_DOWNS_TO_SECOND_FACTOR = 14.28571428571429;
        private const int SECOND_TO_MINUTE_FACTOR = 60;
        private const int MINUTE_TO_HOUR_FACTOR = 60;
        private const int MEASUREMENT_TIME_MILLISECONDS = 70;
        private const int MEASUREMENT_FILTER_COUNT = 4;        
        private const int GPIO_PIN_DEBOUNCE_TIMEOUT_MILLISECONDS = 25;

        private static GpioChangeCounter _gpioChangeCounter;
        private static List<int> _lastDownsUps;
        private static volatile bool _isStopped;
        private static volatile bool _isStopping;

        public static void Initialize()
        {
            var gpioController = GpioController.GetDefault();

            var pin = gpioController.OpenPin(0);
            pin.SetDriveMode(GpioPinDriveMode.Input);
            pin.DebounceTimeout = TimeSpan.FromMilliseconds(GPIO_PIN_DEBOUNCE_TIMEOUT_MILLISECONDS);

            _gpioChangeCounter = new GpioChangeCounter(pin);
            _gpioChangeCounter.Polarity = GpioChangePolarity.Both;
        }

        public static void Start()
        {
            Task.Factory.StartNew(() =>
            {

                StartInternal();

            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Current)
             .AsAsyncAction()
             .AsTask()
             .ContinueWith((t) =>
             {

                 Logger.WriteAsync(nameof(SpeedSensor), t.Exception).Wait();
                 SystemController.ShutdownApplicationAsync(true).Wait();

             }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private static void StartInternal()
        {
            _isStopped = false;

            _lastDownsUps = new int[MEASUREMENT_FILTER_COUNT].ToList();

            _gpioChangeCounter.Start();

            while (_isStopping == false)
            {
                Task.Delay(MEASUREMENT_TIME_MILLISECONDS).Wait();

                var read = _gpioChangeCounter.Read();
                Counter(read.Count);

                _gpioChangeCounter.Reset();
            }

            _gpioChangeCounter.Stop();

            _isStopped = true;
        }

        public static async Task StopAsync()
        {
            _isStopping = true;

            while (_isStopped == false)
            {
                await Task.Delay(10);
            }

            RoundsPerMinute = 0;
            KilometerPerHour = 0;
            _lastDownsUps = null;

            _isStopping = false;
        }

        private static void Counter(ulong downsUps)
        {
            var downsUpsPerMinute = downsUps * FALLS_DOWNS_TO_SECOND_FACTOR * SECOND_TO_MINUTE_FACTOR;

            var roundsPerMinute = (int)Math.Round(downsUpsPerMinute / FALLS_DOWNS_ONE_ROUND, 0);
            roundsPerMinute = roundsPerMinute < 0 ? 0 : roundsPerMinute;

            _lastDownsUps.RemoveAt(0);
            _lastDownsUps.Add(roundsPerMinute);

            roundsPerMinute = (int)Math.Round(_lastDownsUps.Average());
            
            RoundsPerMinute = roundsPerMinute;

            var roundsPerHour = roundsPerMinute * MINUTE_TO_HOUR_FACTOR;
            var kilometerPerHour = Math.Round(roundsPerHour / ROUNDS_ONE_KILOMETER, 2);
            kilometerPerHour = kilometerPerHour < 0 ? 0 : kilometerPerHour;
            KilometerPerHour = kilometerPerHour;
        }
    }
}