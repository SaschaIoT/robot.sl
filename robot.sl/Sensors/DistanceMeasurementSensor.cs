using robot.sl.Exceptions;
using robot.sl.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;

namespace robot.sl.Sensors
{
    /// <summary>
    /// Ultrasonic distance sensor: I2CXL-MaxSonar- EZ MB1202
    /// </summary>
    public class DistanceMeasurementSensor
    {
        public class Measurement
        {
            public int DistanceInCm { get; set; }
            public bool Error { get; set; }
        }

        private I2cDevice _distanceMeasurementSensor;
        private GpioPin _pin;

        public async Task Initialize(int i2cAddress)
        {
            var settings = new I2cConnectionSettings(i2cAddress);
            settings.BusSpeed = I2cBusSpeed.FastMode;
            settings.SharingMode = I2cSharingMode.Shared;
            
            var controller = await I2cController.GetDefaultAsync();
            _distanceMeasurementSensor = controller.GetDevice(settings);

            var gpioController = GpioController.GetDefault();
            _pin = gpioController.OpenPin(1);
            _pin.SetDriveMode(GpioPinDriveMode.Input);
        }

        public void ChangeI2cAddress()
        {
            //Change the I2c Address of "I2CXL-MaxSonar- EZ MB1202" because his adrees is conflicting with the adress of
            //"Adafruit 16-Channel PWM/Servo HAT for Raspberry Pi". The addresses of "I2CXL-MaxSonar- EZ MB1202" and
            //"Adafruit 16-Channel PWM/Servo HAT for Raspberry Pi" are not the same, but it conflicting because the
            //"Adafruit 16-Channel PWM/Servo HAT for Raspberry Pi" has a bug.

            Synchronous.Call(() =>
            {
                _distanceMeasurementSensor.Write(new byte[] { 0xe0, 0xAA, 0xA5, 0x71 }); //0x71 is the 8 bit address of the I2c device
            });
        }

        public async Task<int> ReadDistanceInCm(int countMesaurements)
        {
            var measurements = new List<int>();
            var errorCount = 0;
            for (int measurementNumber = 0; measurementNumber < countMesaurements; measurementNumber++)
            {
                var measurement = await ReadDistanceInCm();

                if (measurement.Error == false)
                {
                    measurements.Add(measurement.DistanceInCm);
                }
                else
                {
                    errorCount++;
                    measurementNumber--;

                    if (errorCount == 5)
                    {
                        throw new RobotSlException($"{nameof(DistanceMeasurementSensor)}, {nameof(ReadDistanceInCm)}: Exception: Distance measurement fails for than 5 times.");
                    }
                }
            }

            return measurements.Min();
        }

        public async Task<Measurement> ReadDistanceInCm()
        {
            var measurement = new Measurement();

            try
            {
                byte[] range_highLowByte = new byte[2];

                Synchronous.Call(() =>
                {
                    //Call device measurement
                    _distanceMeasurementSensor.Write(new byte[] { 0x51 });
                });

                //Wait device measured started
                await Task.Delay(40);

                //Wait device measurement has finished and I2C is ready
                await WaitDistanceMeasurementSensorI2CIsReady();

                Synchronous.Call(() =>
                {
                    //Read measurement
                    _distanceMeasurementSensor.WriteRead(new byte[] { 0xe1 }, range_highLowByte);
                });

                measurement.DistanceInCm = (range_highLowByte[0] * 256) + range_highLowByte[1];
            }
            catch (Exception exception)
            {
                measurement.Error = true;

                await Logger.Write($"{nameof(DistanceMeasurementSensor)}, {nameof(ReadDistanceInCm)}: ", exception);
            }

            return measurement;
        }

        private async Task WaitDistanceMeasurementSensorI2CIsReady()
        {
            while(_pin.Read() != GpioPinValue.Low)
            {
                await Task.Delay(5);
            }
        }
    }
}
