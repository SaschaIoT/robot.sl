using robot.sl.Helper;
using System;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace robot.sl.Sensors
{
    /// <summary>
    /// Analog to digital sensor: Adafruit ADS1115
    /// </summary>
    public class AnalogToDigitalSensor
    {
        private readonly byte ADC_I2C_ADDR;
        private const byte ADC_REG_POINTER_CONVERSION = 0x00;
        private const byte ADC_REG_POINTER_CONFIG = 0x01;
        public const int ADC_RES = 65536;
        private I2cDevice _device;

        private MultiplexerDevice _multiplexerDevice = MultiplexerDevice.AnalogToDigitalSensor;

        //Dependencies
        private Multiplexer _multiplexer;
        
        public AnalogToDigitalSensor(Multiplexer multiplexer, AdcAddress ads1115Addresses = AdcAddress.GND)
        {
            _multiplexer = multiplexer;
            ADC_I2C_ADDR = (byte)ads1115Addresses;
        }
        
        public async Task InitializeAsync()
        {
            var controller = await I2cController.GetDefaultAsync();

            var settings = new I2cConnectionSettings(ADC_I2C_ADDR);
            settings.BusSpeed = I2cBusSpeed.FastMode;
            _device = controller.GetDevice(settings);
        }
        
        public async Task<double> ReadVoltage()
        {
            var setting = new ADS1115SensorSetting
            {
                Mode = AdcMode.SINGLESHOOT_CONVERSION,
                Input = AdcInput.A0_SE,
                Pga = AdcPga.G1,
                DataRate = AdcDataRate.SPS128,
                ComLatching = AdcComparatorLatching.LATCHING,
                ComMode = AdcComparatorMode.TRADITIONAL,
                ComPolarity = AdcComparatorPolarity.ACTIVE_LOW,
                ComQueue = AdcComparatorQueue.DISABLE_COMPARATOR
            };

            var raw = await ReadSensorAsync(ConfigA(setting), ConfigB(setting));
            var voltage = DecimalToVoltage(setting.Pga, raw, ADC_RES / 2);

            return voltage;
        }

        private async Task<int> ReadSensorAsync(byte configA, byte configB)
        {
            var command = new byte[] { ADC_REG_POINTER_CONFIG, configA, configB };
            var readBuffer = new byte[2];
            var writeBuffer = new byte[] { ADC_REG_POINTER_CONVERSION };

            I2CSynchronous.Call(() =>
            {
                _multiplexer.SelectDevice(_multiplexerDevice);

                _device.Write(command);
            });

            // 7,8 MS for SPS128, change if use other data rate
            await Task.Delay(10);

            I2CSynchronous.Call(() =>
            {
                _multiplexer.SelectDevice(_multiplexerDevice);
                
                _device.WriteRead(writeBuffer, readBuffer);
            });

            if ((byte)(readBuffer[0] & 0x80) != 0x00)
            {
                readBuffer[0] = (byte)~readBuffer[0];
                readBuffer[0] &= 0xEF;
                readBuffer[1] = (byte)~readBuffer[1];
                Array.Reverse(readBuffer);
                return Convert.ToInt16(-1 * (BitConverter.ToInt16(readBuffer, 0) + 1));
            }
            else
            {
                Array.Reverse(readBuffer);
                return BitConverter.ToInt16(readBuffer, 0);
            }
        }
        
        private byte ConfigA(ADS1115SensorSetting setting)
        {
            byte configA = 0;
            return configA = (byte)((byte)setting.Mode << 7 | (byte)setting.Input << 4 | (byte)setting.Pga << 1 | (byte)setting.Mode);
        }
        
        private byte ConfigB(ADS1115SensorSetting setting)
        {
            byte configB;
            return configB = (byte)((byte)setting.DataRate << 5 | (byte)setting.ComMode << 4 | (byte)setting.ComPolarity << 3 | (byte)setting.ComLatching << 2 | (byte)setting.ComQueue);
        }
        
        public double DecimalToVoltage(AdcPga pga, int temp, int resolution)
        {
            double voltage;

            switch (pga)
            {
                case AdcPga.G2P3:
                    voltage = 6.144;
                    break;
                case AdcPga.G1:
                    voltage = 4.096;
                    break;
                case AdcPga.G2:
                    voltage = 2.048;
                    break;
                case AdcPga.G4:
                    voltage = 1.024;
                    break;
                case AdcPga.G8:
                    voltage = 0.512;
                    break;
                case AdcPga.G16:
                default:
                    voltage = 0.256;
                    break;
            }

            return temp * (voltage / resolution);
        }
    }

    public enum AdcAddress : byte { GND = 0x48, VCC = 0x49, SDA = 0x4A, SCL = 0x4B }       // Possible ads1115 addresses:  0x48: ADR -> GND  0x49: ADR -> VCC  0x4A: ADR -> SDA  0x4B: ADR -> SCL
    public enum AdcInput : byte { A0_SE = 0x04, A1_SE = 0x05, A2_SE = 0x06, A3_SE = 0x07, A01_DIFF = 0x00, A03_DIFF = 0x01, A13_DIFF = 0x02, A23_DIFF = 0x03 }
    public enum AdcPga : byte { G2P3 = 0x00, G1 = 0x01, G2 = 0x02, G4 = 0x03, G8 = 0x04, G16 = 0x05 }
    public enum AdcMode : byte { CONTINOUS_CONVERSION = 0x00, SINGLESHOOT_CONVERSION = 0x01 }
    public enum AdcDataRate : byte { SPS8 = 0X00, SPS16 = 0X01, SPS32 = 0X02, SPS64 = 0X03, SPS128 = 0X04, SPS250 = 0X05, SPS475 = 0X06, SPS860 = 0X07 }
    public enum AdcComparatorMode : byte { TRADITIONAL = 0x00, WINDOW = 0x01 }
    public enum AdcComparatorPolarity : byte { ACTIVE_LOW = 0x00, ACTIVE_HIGH = 0x01 }
    public enum AdcComparatorLatching : byte { LATCHING = 0x00, NONLATCHING = 0x01 }
    public enum AdcComparatorQueue : byte { ASSERT_AFTER_ONE = 0x01, ASSERT_AFTER_TWO = 0x02, ASSERT_AFTER_FOUR = 0x04, DISABLE_COMPARATOR = 0x03 }
    
    public class ADS1115SensorSetting
    {
        public AdcInput Input { get; set; }

        public AdcPga Pga { get; set; }

        public AdcMode Mode { get; set; }

        public AdcDataRate DataRate { get; set; }

        public AdcComparatorMode ComMode { get; set; }

        public AdcComparatorPolarity ComPolarity { get; set; }

        public AdcComparatorLatching ComLatching { get; set; }

        public AdcComparatorQueue ComQueue { get; set; }
    }
}
