using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace robot.sl.Sensors
{
    /// <summary>
    /// Distance sensor laser analog: Pololu Carrier with Sharp GP2Y0A60SZLF IR distance sensor
    /// </summary>
    public class DistanceSensorLaserAnalog
    {
        private List<double> _readings = new List<double>();
        private GpioPin _enablePin;

        private const int SENSOR_MIN_DISTANCE = 10;
        private const int SENSOR_MAX_DISTANCE = 90;

        private const int FILTERING_COUNT = 5;

        //Dependencies
        private AnalogToDigitalSensor _analogToDigitalSensor;

        public DistanceSensorLaserAnalog(AnalogToDigitalSensor analogToDigitalSensor)
        {
            _analogToDigitalSensor = analogToDigitalSensor;

            var gpioController = GpioController.GetDefault();

            _enablePin = gpioController.OpenPin(2);
            _enablePin.SetDriveMode(GpioPinDriveMode.Output);
            _enablePin.Write(GpioPinValue.Low);
        }

        public async Task<double> GetDistanceFiltered()
        {
            var distance = 0d;

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
            
            var distanceFiltered = _readings.Average();
            return distanceFiltered;
        }

        public void ClearDistancesFiltered()
        {
            _readings.Clear();
        }

        public async Task<double> GetDistance()
        {
            //Start measurement
            _enablePin.Write(GpioPinValue.High);

            //Wait for measurement
            await Task.Delay(25);

            var voltage = await _analogToDigitalSensor.ReadVoltage();
            var centimeter = 18.778 * Math.Pow(voltage, -1.396);

            //Stop measurement
            _enablePin.Write(GpioPinValue.Low);

            if (centimeter < SENSOR_MIN_DISTANCE)
                centimeter = SENSOR_MIN_DISTANCE;
            else if (centimeter > SENSOR_MAX_DISTANCE)
                centimeter = SENSOR_MAX_DISTANCE;

            return centimeter;
        }
    }
}
