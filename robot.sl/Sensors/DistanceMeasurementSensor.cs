using robot.sl.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace robot.sl.Sensors
{
    /// <summary>
    /// Ultrasonic distance sensor: I2CXL-MaxSonar- EZ MB1202
    /// </summary>
    public class DistanceMeasurementSensor
    {
        private I2cDevice _distanceMeasurementSensor;

        public async Task Initialize(int i2cAddress)
        {
            var settings = new I2cConnectionSettings(i2cAddress);
            settings.BusSpeed = I2cBusSpeed.FastMode;
            settings.SharingMode = I2cSharingMode.Shared;

            var controller = await I2cController.GetDefaultAsync();
            _distanceMeasurementSensor = controller.GetDevice(settings);
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
            for (int measurementNumber = 0; measurementNumber < countMesaurements; measurementNumber++)
            {
                measurements.Add(await ReadDistanceInCm());
            }

            return measurements.Min();
        }

        public async Task<int> ReadDistanceInCm()
        {
            int range = 0;
            byte[] range_highLowByte = new byte[2];

            Synchronous.Call(() =>
            {
                //Call device measurement
                _distanceMeasurementSensor.Write(new byte[] { 0x51 });
            });

            //Wait device measured
            await Task.Delay(100);

            Synchronous.Call(() =>
            {
                //Read measurement
                _distanceMeasurementSensor.WriteRead(new byte[] { 0xe1 }, range_highLowByte);
            });

            range = (range_highLowByte[0] * 256) + range_highLowByte[1];

            return range;
        }
    }
}
