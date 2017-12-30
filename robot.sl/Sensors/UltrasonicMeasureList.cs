using System.Collections.Generic;
using System.Linq;

namespace robot.sl.Sensors
{
    public class UltrasonicMeasureList
    {
        private List<Measurement> _ultrasonicMeasurements = new List<Measurement>();
        private static volatile object _lock = new object();

        public void Add(Measurement ultrasonicMeasure)
        {
            lock (_lock)
            {
                _ultrasonicMeasurements.Add(ultrasonicMeasure);
            }
        }

        public Measurement GetLast()
        {
            lock (_lock)
            {
                var last = _ultrasonicMeasurements.LastOrDefault();
                return last;
            }
        }
        
        public void RemoveFirst()
        {
            lock (_lock)
            {
                if (_ultrasonicMeasurements.Count >= 2)
                {
                    _ultrasonicMeasurements.RemoveAt(0);
                }
            }
        }
    }
}