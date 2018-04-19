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
    public class DistanceSensorUltrasonic
    {
        private I2cDevice _distanceSensorUltrasonic;
        private List<int> _readings = new List<int>();
        private MultiplexerDevice _multiplexerDevice = MultiplexerDevice.UltrasonicDistanceSensor;
        private const int FILTERING_COUNT = 5;

        //Dependencies
        private Multiplexer _multiplexer;

        public async Task InitializeAsync(Multiplexer multiplexer, int i2cAddress = 0x39)
        {
            var settings = new I2cConnectionSettings(i2cAddress);
            settings.BusSpeed = I2cBusSpeed.FastMode;
            settings.SharingMode = I2cSharingMode.Shared;

            _multiplexer = multiplexer;

            var controller = await I2cController.GetDefaultAsync();
            _distanceSensorUltrasonic = controller.GetDevice(settings);
        }

        public void ChangeI2cAddress()
        {
            //Change the I2c Address of "I2CXL-MaxSonar- EZ MB1202" because his adrees is conflicting with the adress of
            //"Adafruit 16-Channel PWM/Servo HAT for Raspberry Pi". The addresses of "I2CXL-MaxSonar- EZ MB1202" and
            //"Adafruit 16-Channel PWM/Servo HAT for Raspberry Pi" are not the same, but it conflicting because the
            //"Adafruit 16-Channel PWM/Servo HAT for Raspberry Pi" has a bug.

            I2CSynchronous.Call(() =>
            {
                _multiplexer.SelectDevice(_multiplexerDevice);

                _distanceSensorUltrasonic.Write(new byte[] { 0xe0, 0xAA, 0xA5, 0x72 }); //0x71 is the 8 bit address of the I2c device
            });
        }

        public async Task<int> GetDistanceFiltered()
        {
            var distance = 0;

            if (_readings.Count >= FILTERING_COUNT)
            {
                _readings.RemoveAt(0);
            }
            else
            {
                while (_readings.Count < FILTERING_COUNT)
                {
                    distance = await GetDistance();
                    _readings.Add(distance);
                }
            }

            distance = await GetDistance();
            _readings.Add(distance);

            var distanceFiltered = Convert.ToInt32(_readings.Average());
            return distanceFiltered;
        }

        public async Task<int> GetDistance()
        {
            var range_highLowByte = new byte[2];

            I2CSynchronous.Call(() =>
            {
                _multiplexer.SelectDevice(_multiplexerDevice);

                //Call device measurement
                _distanceSensorUltrasonic.Write(new byte[] { 0x51 });
            });

            //Wait device measurement has finished and I2C is ready
            await Task.Delay(120);

            I2CSynchronous.Call(() =>
            {
                _multiplexer.SelectDevice(_multiplexerDevice);

                //Read measurement
                _distanceSensorUltrasonic.WriteRead(new byte[] { 0xe1 }, range_highLowByte);
            });

            var distance = (range_highLowByte[0] * 256) + range_highLowByte[1];

            return distance;
        }

        public void ClearDistancesFiltered()
        {
            _readings.Clear();
        }
    }
}