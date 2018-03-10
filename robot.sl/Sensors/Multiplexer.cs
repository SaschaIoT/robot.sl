using System;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace robot.sl.Sensors
{
    /// <summary>
    /// I2C multiplexer: Adafruit TCA9548A
    /// </summary>
    public class Multiplexer
    {
        private I2cDevice _i2cDevice;
        private const int I2C_DEVICE_ADDRESS = 0x71;

        public async Task InitializeAsync()
        {
            var settings = new I2cConnectionSettings(I2C_DEVICE_ADDRESS)
            {
                BusSpeed = I2cBusSpeed.StandardMode,
                SharingMode = I2cSharingMode.Shared
            };

            var controller = await I2cController.GetDefaultAsync();
            _i2cDevice = controller.GetDevice(settings);
        }

        public void SelectDevice(MultiplexerDevice multiplexerDevice)
        {
            // The switch time of the multiplexer is lower than I2C can transfer a bit,
            // so there is no delay needed (no wait for switch necessary).
            // Switch time: the time the multiplexer need to change the I2C channel
            _i2cDevice.Write(new byte[] { (byte)(1 << (int)multiplexerDevice) });
        }
    }

    public enum MultiplexerDevice
    {
        DistanceLaserSensorTop = 0x7,
        DistanceLaserSensorMiddleTop = 0x1,
        DistanceLaserSensorMiddleBottom = 0x5,
        DistanceLaserSensorBottom = 0x3
    }
}
