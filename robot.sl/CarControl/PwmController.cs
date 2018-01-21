using robot.sl.Helper;
using System;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace robot.sl.CarControl
{
    /// <summary>
    /// PWM motor board: Adafruit DC and Stepper Motor HAT for Raspberry Pi
    /// PWM servo board: Adafruit 16-Channel PWM/Servo HAT for Raspberry Pi
    /// </summary>
    public class PwmController
    {
        private I2cDevice _pwmDevice;
        private readonly int _baseAddress;

        public PwmController(int baseAddress)
        {
            //For servo max 1000 frequency. For motor max 1600 frequency
            MaxFrequency = 1600;
            MinFrequency = 40;
            PinCount = 16;
            _baseAddress = baseAddress;
        }

        public async Task InitializeAsync()
        {
            var settings = new I2cConnectionSettings(_baseAddress) { BusSpeed = I2cBusSpeed.FastMode, SharingMode = I2cSharingMode.Shared };

            var controller = await I2cController.GetDefaultAsync();
            _pwmDevice = controller.GetDevice(settings);

            Reset();
        }

        public void Reset()
        {
            I2CSynchronous.Call(() =>
            {
                _pwmDevice.Write(new byte[] { (byte)Registers.MODE1, 0x0 }); // reset the device
            });

            SetAllPwm(4096, 0);
        }

        /// <summary>
        /// Set pwm values
        /// </summary>
        /// <param name="channel">The pin that should updated</param>
        /// <param name="on">The tick (between 0..4095) when the signal should change from low to high</param>
        /// <param name="off">the tick (between 0..4095) when the signal should change from high to low</param>
        public void SetPwm(byte channel, ushort on, ushort off)
        {
            I2CSynchronous.Call(() =>
            {
                _pwmDevice.Write(new byte[] { (byte)(Registers.LED0_ON_L + 4 * channel), (byte)(on & 0xFF) });
                _pwmDevice.Write(new byte[] { (byte)(Registers.LED0_ON_H + 4 * channel), (byte)(on >> 8) });
                _pwmDevice.Write(new byte[] { (byte)(Registers.LED0_OFF_L + 4 * channel), (byte)(off & 0xFF) });
                _pwmDevice.Write(new byte[] { (byte)(Registers.LED0_OFF_H + 4 * channel), (byte)(off >> 8) });
            });
        }

        public void SetAllPwm(ushort on, ushort off)
        {
            I2CSynchronous.Call(() =>
            {
                _pwmDevice.Write(new byte[] { (byte)Registers.ALL_LED_ON_L, (byte)(on & 0xFF) });
                _pwmDevice.Write(new byte[] { (byte)Registers.ALL_LED_ON_H, (byte)(on >> 8) });
                _pwmDevice.Write(new byte[] { (byte)Registers.ALL_LED_OFF_L, (byte)(off & 0xFF) });
                _pwmDevice.Write(new byte[] { (byte)Registers.ALL_LED_OFF_H, (byte)(off >> 8) });
            });
        }

        /// <summary>
        /// Set the frequency (defaults to 60Hz if not set). 1 Hz equals 1 full pwm cycle per second.
        /// </summary>
        /// <param name="frequency">Frequency in Hz</param>
        public double SetDesiredFrequency(double frequency)
        {
            if (frequency > MaxFrequency || frequency < MinFrequency)
            {
                throw new ArgumentOutOfRangeException(nameof(frequency), "Frequency must be between 40 and 1000hz");
            }

            frequency *= 0.9f; //Correct for overshoot in the frequency setting (see issue #11).
            double prescaleval = 25000000f;
            prescaleval /= 4096;
            prescaleval /= frequency;
            prescaleval -= 1;

            byte prescale = (byte)Math.Floor(prescaleval + 0.5f);

            I2CSynchronous.Call(() =>
            {
                var readBuffer = new byte[1];
                _pwmDevice.WriteRead(new byte[] { (byte)Registers.MODE1 }, readBuffer);

                byte oldmode = readBuffer[0];
                byte newmode = (byte)((oldmode & 0x7F) | 0x10); //sleep
                _pwmDevice.Write(new byte[] { (byte)Registers.MODE1, newmode });
                _pwmDevice.Write(new byte[] { (byte)Registers.PRESCALE, prescale });
                _pwmDevice.Write(new byte[] { (byte)Registers.MODE1, oldmode });
                Task.Delay(TimeSpan.FromMilliseconds(5)).Wait();
                _pwmDevice.Write(new byte[] { (byte)Registers.MODE1, (byte)(oldmode | 0xa1) });
            });

            ActualFrequency = frequency;

            return ActualFrequency;

        }

        public double ActualFrequency { get; private set; }
        public double MaxFrequency { get; }
        public double MinFrequency { get; }
        public int PinCount { get; }
    }

    public enum Registers
    {
        MODE1 = 0x00,
        MODE2 = 0x01,
        SUBADR1 = 0x02,
        SUBADR2 = 0x03,
        SUBADR3 = 0x04,
        PRESCALE = 0xFE,
        LED0_ON_L = 0x06,
        LED0_ON_H = 0x07,
        LED0_OFF_L = 0x08,
        LED0_OFF_H = 0x09,
        ALL_LED_ON_L = 0xFA,
        ALL_LED_ON_H = 0xFB,
        ALL_LED_OFF_L = 0xFC,
        ALL_LED_OFF_H = 0xFD
    }

    public enum Bits
    {
        RESTART = 0x80,
        SLEEP = 0x10,
        ALLCALL = 0x01,
        INVRT = 0x10,
        OUTDRV = 0x04
    }
}
