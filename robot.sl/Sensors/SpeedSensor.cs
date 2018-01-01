using robot.sl.Helper;
using System;
using System.Diagnostics;
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
        public static double RoundsOneKilometer = 5250;
        public static int RoundsPerMinute { get; private set; }
        public static double KilometerPerHour { get; private set; }

        public static bool IsDriving
        {
            get
            {
                return RoundsPerMinute >= 1;
            }
        }
        private static int _downsUps = 0;
        //private static volatile bool _read = false;
        //private static Timer _timer;
        private static GpioPin _pin;
        private static volatile bool _isStopped;
        private static volatile bool _isStopping;

        public static void Initialize()
        {
            var gpioController = GpioController.GetDefault();

            _pin = gpioController.OpenPin(0);
            _pin.SetDriveMode(GpioPinDriveMode.Input);

            //Pin ValueChanged-Event currently not work with Microsoft.IoT.Lightning
            //_pin.ValueChanged += Pin_ValueChanged;
        }

        public static void Start()
        {
            Task.Factory.StartNew(() =>
            {

                StartInternal().Wait();

            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Current)
             .AsAsyncAction()
             .AsTask()
             .ContinueWith((t) =>
             {

                 Logger.Write(nameof(SpeedSensor), t.Exception).Wait();
                 SystemController.ShutdownApplication(true).Wait();

             }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private static async Task StartInternal()
        {
            //_timer = new Timer(Counter, null, 0, 140);

            _isStopped = false;

            var debounceTimeout = TimeSpan.FromMilliseconds(1);
            var debounce = DateTime.Now;
            var pinValueOld = _pin.Read();
            var stopWatch = new Stopwatch();

            stopWatch.Start();

            while (!_isStopping)
            {
                if (stopWatch.ElapsedMilliseconds >= 70)
                {
                    stopWatch.Reset();
                    Counter(); //Counter(null);
                    await Task.Delay(150);
                    stopWatch.Start();
                }

                if (DateTime.Now <= debounce) //!_read || 
                {
                    continue;
                }

                var pinValueNew = _pin.Read();
                if (pinValueOld == GpioPinValue.High
                    && pinValueNew == GpioPinValue.Low)
                {
                    _downsUps++;
                    debounce = DateTime.Now.Add(debounceTimeout);
                }
                else if (pinValueOld == GpioPinValue.Low
                         && pinValueNew == GpioPinValue.High)
                {
                    _downsUps++;

                    debounce = DateTime.Now.Add(debounceTimeout);
                }

                pinValueOld = pinValueNew;
            }

            _isStopped = true;
        }

        public static async Task Stop()
        {
            _isStopping = true;

            while (!_isStopped)
            {
                await Task.Delay(10);
            }

            //_timer = null;
            _downsUps = 0;
            RoundsPerMinute = 0;
            KilometerPerHour = 0;

            _isStopping = false;
        }

        private static void Counter() //private static void Counter(object state)
        {
            //_read = false;

            var sekundeFaktor = 14.28571428571429;
            var minuteFaktor = 60;
            var stundenFaktor = 60;
            var oneRunde = 40;
            var downsUpsPerMinute = _downsUps * sekundeFaktor * minuteFaktor;

            var roundsPerMinute = (int)Math.Round(downsUpsPerMinute / oneRunde, 0);
            roundsPerMinute = roundsPerMinute < 0 ? 0 : roundsPerMinute;
            RoundsPerMinute = roundsPerMinute;

            var roundsPerHour = roundsPerMinute * stundenFaktor;
            var kilometerPerHour = Math.Round(roundsPerHour / RoundsOneKilometer, 2);
            kilometerPerHour = kilometerPerHour < 0 ? 0 : kilometerPerHour;
            KilometerPerHour = kilometerPerHour;

            _downsUps = 0;
            //_read = true;
        }

        //Pin ValueChanged-Event currently not work with Microsoft.IoT.Lightning
        //private static void Pin_ValueChanged(GpioPin gpioPin, GpioPinValueChangedEventArgs args)
        //{
        //    if (!_read)
        //    {
        //        return;
        //    }

        //    if (args.Edge == GpioPinEdge.FallingEdge)
        //    {
        //        _falls++;
        //    }
        //}
    }
}