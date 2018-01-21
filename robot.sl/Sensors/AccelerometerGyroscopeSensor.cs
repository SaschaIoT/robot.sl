using robot.sl.Helper;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace robot.sl.Sensors
{
    /// <summary>
    /// Accelerometer: Adafruit Accelerometer LIS3DH
    /// </summary>
    public class AccelerometerGyroscopeSensor
    {
        private const byte I2C_ADDRESS = 0x68; //I2C address of accelorometer

        private I2cDevice _accelerometer;

        private AccelerationGyroleration _currentAcceleration = new AccelerationGyroleration { AccelerationX = 0, AccelerationY = 0, AccelerationZ = 0 };
        private AccelerationGyroleration _currentLinearAcceleration = new AccelerationGyroleration { AccelerationX = 0, AccelerationY = 0, AccelerationZ = 0 };

        private double _gravityX = 0d;
        private double _gravityY = 0d;
        private double _gravityZ = 0d;

        private volatile bool _isStopping = false;
        private volatile bool _isStopped = true;

        private const byte REGISTER_POWER_MANAGEMENT_1 = 0x6B;
        private const byte REGISTER_SAMPLE_RATE_DIVIDER = 0x19;
        private const byte REGISTER_CONFIG = 0x1A;
        private const byte REGISTER_GYROSCOPE_CONFIG = 0x1B;
        private const byte REGISTER_ACCELEROMETER_CONFIG = 0x1C;
        private const byte REGISTER_ACCELEROMETER_X = 0x3B;

        public async Task StopAsync()
        {
            _isStopping = true;

            while (!_isStopped)
            {
                await Task.Delay(10);
            }

            _isStopping = false;
        }

        public async Task InitializeAsync()
        {
            var settings = new I2cConnectionSettings(I2C_ADDRESS)
            {
                BusSpeed = I2cBusSpeed.StandardMode,
                SharingMode = I2cSharingMode.Shared
            };

            var controller = await I2cController.GetDefaultAsync();
            _accelerometer = controller.GetDevice(settings);

            I2CSynchronous.Call(() =>
            {
                //Enable all axes with normal mode
                _accelerometer.Write(new byte[] { REGISTER_POWER_MANAGEMENT_1, 0 }); //Wake up device
                _accelerometer.Write(new byte[] { REGISTER_POWER_MANAGEMENT_1, 0x80 }); //Reset the device
            });

            await Task.Delay(20);

            I2CSynchronous.Call(() =>
            {
                _accelerometer.Write(new byte[] { REGISTER_POWER_MANAGEMENT_1, 1 }); //Set clock source to gyro x
                _accelerometer.Write(new byte[] { REGISTER_GYROSCOPE_CONFIG, 0 }); //+/- 250 degrees sec
                _accelerometer.Write(new byte[] { REGISTER_ACCELEROMETER_CONFIG, 0 }); //+/- 2g

                _accelerometer.Write(new byte[] { REGISTER_CONFIG, 1 }); //184 Hz, 2ms delay
                _accelerometer.Write(new byte[] { REGISTER_SAMPLE_RATE_DIVIDER, 19 }); //Set rate 50Hz
                _accelerometer.Write(new byte[] { REGISTER_POWER_MANAGEMENT_1, 0 }); //Wake up device
            });
        }

        public void Start()
        {
            Task.Factory.StartNew(() =>
            {

                StartInternal();

            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
             .AsAsyncAction()
             .AsTask()
             .ContinueWith((t) =>
             {

                 Logger.WriteAsync(nameof(AccelerometerGyroscopeSensor), t.Exception).Wait();
                 SystemController.ShutdownApplicationAsync(true).Wait();

             }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private void StartInternal()
        {
            var warmUp = 0;

            _isStopped = false;

            while (!_isStopping)
            {
                AccelerationGyroleration acceleration;

                try
                {
                    acceleration = ReadInternal();
                }
                catch (Exception exception)
                {
                    Logger.WriteAsync($"{nameof(AccelerometerGyroscopeSensor)}, {nameof(StartInternal)}", exception).Wait();

                    Task.Delay(10).Wait();
                    continue;
                }

                //Remove gravity
                var alpha = 0.8d;
                _gravityX = alpha * _gravityX + (1 - alpha) * acceleration.AccelerationX;
                _gravityY = alpha * _gravityY + (1 - alpha) * acceleration.AccelerationY;
                _gravityZ = alpha * _gravityZ + (1 - alpha) * acceleration.AccelerationZ;
                var accelerationX = acceleration.AccelerationX - _gravityX;
                var accelerationY = acceleration.AccelerationY - _gravityY;
                var accelerationZ = acceleration.AccelerationZ - _gravityZ;

                if (warmUp <= 60)
                {
                    warmUp++;
                }
                else
                {
                    _currentAcceleration = acceleration;
                    _currentLinearAcceleration = new AccelerationGyroleration
                    {
                        AccelerationX = accelerationX,
                        AccelerationY = accelerationY,
                        AccelerationZ = accelerationZ,
                        GyroX = acceleration.GyroX,
                        GyroY = acceleration.GyroY,
                        GyroZ = acceleration.GyroZ
                    };
                }

                Task.Delay(10).Wait();
            }

            _isStopped = true;
        }

        public AccelerationGyroleration ReadLinearAcceleration()
        {
            return _currentLinearAcceleration;
        }

        public AccelerationGyroleration ReadRawAcceleration()
        {
            return _currentAcceleration;
        }

        private AccelerationGyroleration ReadInternal()
        {
            var data = new byte[14]; //6 bytes equals 2 bytes * 3 axes
            var readAddress = new byte[] { REGISTER_ACCELEROMETER_X }; //0x80 for autoincrement, read from register x all three axis

            I2CSynchronous.Call(() =>
            {
                _accelerometer.WriteRead(readAddress, data);
            });

            var xa = (short)(data[0] << 8 | data[1]);
            var ya = (short)(data[2] << 8 | data[3]);
            var za = (short)(data[4] << 8 | data[5]);

            var temperature = (short)(data[6] << 8 | data[7]);

            var xg = (short)(data[8] << 8 | data[9]);
            var yg = (short)(data[10] << 8 | data[11]);
            var zg = (short)(data[12] << 8 | data[13]);

            var acceleration = new AccelerationGyroleration
            {
                AccelerationX = xa / (float)16384,
                AccelerationY = ya / (float)16384,
                AccelerationZ = za / (float)16384,
                TemperatureInC = temperature / 340.00 + 36.53,
                GyroX = xg / (float)131,
                GyroY = yg / (float)131,
                GyroZ = zg / (float)131
            };

            return acceleration;
        }
    }

    public struct AccelerationGyroleration
    {
        public double AccelerationX;
        public double AccelerationY;
        public double AccelerationZ;
        public double TemperatureInC;
        public double GyroX;
        public double GyroY;
        public double GyroZ;
    }
}
