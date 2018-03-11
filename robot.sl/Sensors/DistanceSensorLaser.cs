using robot.sl.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace robot.sl.Sensors
{
    /// <summary>
    /// Distance sensor laser: Adafruit VL53L0X
    /// </summary>
    public class DistanceSensorLaser
    {
        private I2cDevice _i2cDevice;
        private List<int> _readings = new List<int>();
        private LightResponse _lightResponse;
        private MultiplexerDevice _multiplexerDevice;
        
        // Dependencies
        private Multiplexer _multiplexer;

        const byte _DEVICE_ADDRESS = 0x29;
        const int _RANGE_MAX_VALUE = 800;
        private int io_timeout_s = 0;
        private double _measurement_timing_budget_us = 0;
        private byte _stop_variable = 0;

        const byte _SYSRANGE_START = 0x00;
        const byte _SYSTEM_THRESH_HIGH = 0x0C;
        const byte _SYSTEM_THRESH_LOW = 0x0E;
        const byte _SYSTEM_SEQUENCE_CONFIG = 0x01;
        const byte _SYSTEM_RANGE_CONFIG = 0x09;
        const byte _SYSTEM_INTERMEASUREMENT_PERIOD = 0x04;
        const byte _SYSTEM_INTERRUPT_CONFIG_GPIO = 0x0A;
        const byte _GPIO_HV_MUX_ACTIVE_HIGH = 0x84;
        const byte _SYSTEM_INTERRUPT_CLEAR = 0x0B;
        const byte _RESULT_INTERRUPT_STATUS = 0x13;
        const byte _RESULT_RANGE_STATUS = 0x14;
        const byte _RESULT_CORE_AMBIENT_WINDOW_EVENTS_RTN = 0xBC;
        const byte _RESULT_CORE_RANGING_TOTAL_EVENTS_RTN = 0xC0;
        const byte _RESULT_CORE_AMBIENT_WINDOW_EVENTS_REF = 0xD0;
        const byte _RESULT_CORE_RANGING_TOTAL_EVENTS_REF = 0xD4;
        const byte _RESULT_PEAK_SIGNAL_RATE_REF = 0xB6;
        const byte _ALGO_PART_TO_PART_RANGE_OFFSET_MM = 0x28;
        const byte _I2C_SLAVE_DEVICE_ADDRESS = 0x8A;
        const byte _MSRC_CONFIG_CONTROL = 0x60;
        const byte _PRE_RANGE_CONFIG_MIN_SNR = 0x27;
        const byte _PRE_RANGE_CONFIG_VALID_PHASE_LOW = 0x56;
        const byte _PRE_RANGE_CONFIG_VALID_PHASE_HIGH = 0x57;
        const byte _PRE_RANGE_MIN_COUNT_RATE_RTN_LIMIT = 0x64;
        const byte _FINAL_RANGE_CONFIG_MIN_SNR = 0x67;
        const byte _FINAL_RANGE_CONFIG_VALID_PHASE_LOW = 0x47;
        const byte _FINAL_RANGE_CONFIG_VALID_PHASE_HIGH = 0x48;
        const byte _FINAL_RANGE_CONFIG_MIN_COUNT_RATE_RTN_LIMIT = 0x44;
        const byte _PRE_RANGE_CONFIG_SIGMA_THRESH_HI = 0x61;
        const byte _PRE_RANGE_CONFIG_SIGMA_THRESH_LO = 0x62;
        const byte _PRE_RANGE_CONFIG_VCSEL_PERIOD = 0x50;
        const byte _PRE_RANGE_CONFIG_TIMEOUT_MACROP_HI = 0x51;
        const byte _PRE_RANGE_CONFIG_TIMEOUT_MACROP_LO = 0x52;
        const byte _SYSTEM_HISTOGRAM_BIN = 0x81;
        const byte _HISTOGRAM_CONFIG_INITIAL_PHASE_SELECT = 0x33;
        const byte _HISTOGRAM_CONFIG_READOUT_CTRL = 0x55;
        const byte _FINAL_RANGE_CONFIG_VCSEL_PERIOD = 0x70;
        const byte _FINAL_RANGE_CONFIG_TIMEOUT_MACROP_HI = 0x71;
        const byte _FINAL_RANGE_CONFIG_TIMEOUT_MACROP_LO = 0x72;
        const byte _CROSSTALK_COMPENSATION_PEAK_RATE_MCPS = 0x20;
        const byte _MSRC_CONFIG_TIMEOUT_MACROP = 0x46;
        const byte _SOFT_RESET_GO2_SOFT_RESET_N = 0xBF;
        const byte _IDENTIFICATION_MODEL_ID = 0xC0;
        const byte _IDENTIFICATION_REVISION_ID = 0xC2;
        const byte _OSC_CALIBRATE_VAL = 0xF8;
        const byte _GLOBAL_CONFIG_VCSEL_WIDTH = 0x32;
        const byte _GLOBAL_CONFIG_SPAD_ENABLES_REF_0 = 0xB0;
        const byte _GLOBAL_CONFIG_SPAD_ENABLES_REF_1 = 0xB1;
        const byte _GLOBAL_CONFIG_SPAD_ENABLES_REF_2 = 0xB2;
        const byte _GLOBAL_CONFIG_SPAD_ENABLES_REF_3 = 0xB3;
        const byte _GLOBAL_CONFIG_SPAD_ENABLES_REF_4 = 0xB4;
        const byte _GLOBAL_CONFIG_SPAD_ENABLES_REF_5 = 0xB5;
        const byte _GLOBAL_CONFIG_REF_EN_START_SELECT = 0xB6;
        const byte _DYNAMIC_SPAD_NUM_REQUESTED_REF_SPAD = 0x4E;
        const byte _DYNAMIC_SPAD_REF_EN_START_OFFSET = 0x4F;
        const byte _POWER_MANAGEMENT_GO1_POWER_FORCE = 0x80;
        const byte _VHV_CONFIG_PAD_SCL_SDA__EXTSUP_HV = 0x89;
        const byte _ALGO_PHASECAL_LIM = 0x30;
        const byte _ALGO_PHASECAL_CONFIG_TIMEOUT = 0x30;
        const byte _VCSEL_PERIOD_PRE_RANGE = 0;
        const byte _VCSEL_PERIOD_FINAL_RANGE = 1;

        public DistanceSensorLaser(Multiplexer multiplexer,
                                   MultiplexerDevice multiplexerDevice,
                                   LightResponse lightResponse)
        {
            _multiplexer = multiplexer;
            _multiplexerDevice = multiplexerDevice;
            _lightResponse = lightResponse;
        }

        public async Task InitializeAsync()
        {
            var settings = new I2cConnectionSettings(_DEVICE_ADDRESS)
            {
                BusSpeed = I2cBusSpeed.StandardMode, // Use this sensor only with I2C Standard Mode
                SharingMode = I2cSharingMode.Shared
            };

            var controller = await I2cController.GetDefaultAsync();
            _i2cDevice = controller.GetDevice(settings);

            Configure();
        }

        private void Configure(int io_timeout_s_p = 0)
        {
            io_timeout_s = io_timeout_s_p;

            var _BUFFER = new byte[3];

            // Check identification registers for expected values.
            // From section 3.2 of the datasheet.
            //if (_read_u8(0xC0) != 0xEE || _read_u8(0xC1) != 0xAA || _read_u8(0xC2) != 0x10)
            //    throw new Exception("Failed to find expected ID register values. Check wiring!");

            // Initialize access to the sensor.  This is based on the logic from:
            //   https://github.com/pololu/vl53l0x-arduino/blob/master/VL53L0X.cpp
            // Set I2C standard mode.
            _write_u8(0x88, 0x00);
            _write_u8(0x80, 0x01);
            _write_u8(0xFF, 0x01);
            _write_u8(0x00, 0x00);
            _stop_variable = _read_u8(0x91);
            _write_u8(0x00, 0x01);
            _write_u8(0xFF, 0x00);
            _write_u8(0x80, 0x00);
            // set final range signal rate limit to 0.15 MCPS (million counts per second)
            // disable SIGNAL_RATE_MSRC (bit 1) and SIGNAL_RATE_PRE_RANGE (bit 4)
            // limit checks
            var config_control = _read_u8(_MSRC_CONFIG_CONTROL) | 0x12;
            _write_u8(_MSRC_CONFIG_CONTROL, (byte)config_control);
            // set final range signal rate limit to 0.25 MCPS (million counts per
            // second)
            var signalRateLimit = LightResponseValues.GetLightResponseValue(_lightResponse);
            signal_rate_limit(signalRateLimit);
            _write_u8(_SYSTEM_SEQUENCE_CONFIG, 0xFF);
            var si = _get_spad_info();
            // The SPAD map (RefGoodSpadMap) is read by
            // VL53L0X_get_info_from_device() in the API, but the same data seems to
            // be more easily readable from GLOBAL_CONFIG_SPAD_ENABLES_REF_0 through
            // _6, so read it from there.
            var ref_spad_map = new byte[7];
            I2CSynchronous.Call(() =>
            {
                _multiplexer.SelectDevice(_multiplexerDevice);
                _i2cDevice.WriteRead(new byte[] { _GLOBAL_CONFIG_SPAD_ENABLES_REF_0 }, ref_spad_map);
            });
            _write_u8(0xFF, 0x01);
            _write_u8(_DYNAMIC_SPAD_REF_EN_START_OFFSET, 0x00);
            _write_u8(_DYNAMIC_SPAD_NUM_REQUESTED_REF_SPAD, 0x2C);
            _write_u8(0xFF, 0x00);
            _write_u8(_GLOBAL_CONFIG_REF_EN_START_SELECT, 0xB4);
            var first_spad_to_enable = si.is_aperture ? 12 : 0;
            var spads_enabled = 0;
            for (var i = 0; i <= 47; i++)
            {
                if (i < first_spad_to_enable || spads_enabled == si.count)
                {
                    // This bit is lower than the first one that should be enabled,
                    // or (reference_spad_count) bits have already been enabled, so
                    // zero this bit.
                    ref_spad_map[1 + (int)Math.Floor(i / 8d)] &= (byte)~(1 << (i % 8));
                }
                else if (((ref_spad_map[1 + (int)Math.Floor(i / 8d)] >> (i % 8)) & 0x1) > 0)
                    spads_enabled += 1;
            }
            I2CSynchronous.Call(() =>
            {
                _multiplexer.SelectDevice(_multiplexerDevice);
                _i2cDevice.Write(ref_spad_map);
            });
            _write_u8(0xFF, 0x01);
            _write_u8(0x00, 0x00);
            _write_u8(0xFF, 0x00);
            _write_u8(0x09, 0x00);
            _write_u8(0x10, 0x00);
            _write_u8(0x11, 0x00);
            _write_u8(0x24, 0x01);
            _write_u8(0x25, 0xFF);
            _write_u8(0x75, 0x00);
            _write_u8(0xFF, 0x01);
            _write_u8(0x4E, 0x2C);
            _write_u8(0x48, 0x00);
            _write_u8(0x30, 0x20);
            _write_u8(0xFF, 0x00);
            _write_u8(0x30, 0x09);
            _write_u8(0x54, 0x00);
            _write_u8(0x31, 0x04);
            _write_u8(0x32, 0x03);
            _write_u8(0x40, 0x83);
            _write_u8(0x46, 0x25);
            _write_u8(0x60, 0x00);
            _write_u8(0x27, 0x00);
            _write_u8(0x50, 0x06);
            _write_u8(0x51, 0x00);
            _write_u8(0x52, 0x96);
            _write_u8(0x56, 0x08);
            _write_u8(0x57, 0x30);
            _write_u8(0x61, 0x00);
            _write_u8(0x62, 0x00);
            _write_u8(0x64, 0x00);
            _write_u8(0x65, 0x00);
            _write_u8(0x66, 0xA0);
            _write_u8(0xFF, 0x01);
            _write_u8(0x22, 0x32);
            _write_u8(0x47, 0x14);
            _write_u8(0x49, 0xFF);
            _write_u8(0x4A, 0x00);
            _write_u8(0xFF, 0x00);
            _write_u8(0x7A, 0x0A);
            _write_u8(0x7B, 0x00);
            _write_u8(0x78, 0x21);
            _write_u8(0xFF, 0x01);
            _write_u8(0x23, 0x34);
            _write_u8(0x42, 0x00);
            _write_u8(0x44, 0xFF);
            _write_u8(0x45, 0x26);
            _write_u8(0x46, 0x05);
            _write_u8(0x40, 0x40);
            _write_u8(0x0E, 0x06);
            _write_u8(0x20, 0x1A);
            _write_u8(0x43, 0x40);
            _write_u8(0xFF, 0x00);
            _write_u8(0x34, 0x03);
            _write_u8(0x35, 0x44);
            _write_u8(0xFF, 0x01);
            _write_u8(0x31, 0x04);
            _write_u8(0x4B, 0x09);
            _write_u8(0x4C, 0x05);
            _write_u8(0x4D, 0x04);
            _write_u8(0xFF, 0x00);
            _write_u8(0x44, 0x00);
            _write_u8(0x45, 0x20);
            _write_u8(0x47, 0x08);
            _write_u8(0x48, 0x28);
            _write_u8(0x67, 0x00);
            _write_u8(0x70, 0x04);
            _write_u8(0x71, 0x01);
            _write_u8(0x72, 0xFE);
            _write_u8(0x76, 0x00);
            _write_u8(0x77, 0x00);
            _write_u8(0xFF, 0x01);
            _write_u8(0x0D, 0x01);
            _write_u8(0xFF, 0x00);
            _write_u8(0x80, 0x01);
            _write_u8(0x01, 0xF8);
            _write_u8(0xFF, 0x01);
            _write_u8(0x8E, 0x01);
            _write_u8(0x00, 0x01);
            _write_u8(0xFF, 0x00);
            _write_u8(0x80, 0x00);
            _write_u8(_SYSTEM_INTERRUPT_CONFIG_GPIO, 0x04);
            var gpio_hv_mux_active_high = _read_u8(_GPIO_HV_MUX_ACTIVE_HIGH);
            _write_u8(_GPIO_HV_MUX_ACTIVE_HIGH, (byte)(gpio_hv_mux_active_high & ~0x10)); // active low
            _write_u8(_SYSTEM_INTERRUPT_CLEAR, 0x01);
            //var _measurement_timing_budget_us = measurement_timing_budget();
            _write_u8(_SYSTEM_SEQUENCE_CONFIG, 0xE8);
            //measurement_timing_budget(_measurement_timing_budget_us);
            measurement_timing_budget((int)Mode.HIGH_SPEED);
            _write_u8(_SYSTEM_SEQUENCE_CONFIG, 0x01);
            _perform_single_ref_calibration(0x40);
            _write_u8(_SYSTEM_SEQUENCE_CONFIG, 0x02);
            _perform_single_ref_calibration(0x00);
            // "restore the previous Sequence Config"
            _write_u8(_SYSTEM_SEQUENCE_CONFIG, 0xE8);
        }

        public void SetDeviceAddress(int newAdress)
        {
            _write_u8(_I2C_SLAVE_DEVICE_ADDRESS, (byte)(newAdress & 0x7F));
        }
        
        private double _decode_timeout(int val)
        {
            //format: "(LSByte * 2^MSByte) + 1"
            var decodedTimeout = (val & 0xFF) * Math.Pow(2, ((val & 0xFF00) >> 8)) + 1;
            return decodedTimeout;
        }

        private int _encode_timeout(double timeout_mclks)
        {
            //format: "(LSByte * 2^MSByte) + 1"
            var iTimeout_mclks = (int)timeout_mclks & 0xFFFF;
            var ls_byte = 0;
            var ms_byte = 0;

            if (timeout_mclks > 0)
            {
                ls_byte = iTimeout_mclks - 1;

                while (ls_byte > 255)
                {
                    ls_byte >>= 1;
                    ms_byte += 1;
                }

                var encoded_timeout = ((ms_byte << 8) | (ls_byte & 0xFF)) & 0xFFFF;
                return encoded_timeout;
            }

            return 0;
        }

        private double _timeout_mclks_to_microseconds(double timeout_period_mclks, int vcsel_period_pclks)
        {
            var macro_period_ns = Math.Floor((double)((2304 * (vcsel_period_pclks) * 1655) + 500) / 1000);
            var timeout_mclks_to_microseconds = Math.Floor(((timeout_period_mclks * macro_period_ns) + Math.Floor((macro_period_ns / 2))) / 1000);
            return timeout_mclks_to_microseconds;
        }

        private byte _read_u8(byte address)
        {
            //Read an 8-bit unsigned value from the specified 8-bit address.
            var data = new byte[1];
            I2CSynchronous.Call(() =>
            {
                _multiplexer.SelectDevice(_multiplexerDevice);
                _i2cDevice.WriteRead(new byte[] { (byte)(address & 0xFF) }, data);
            });

            return data[0];
        }
        private int _read_u16(byte address)
        {
            //Read a 16-bit BE unsigned value from the specified 8-bit address.
            var data = new byte[2];
            I2CSynchronous.Call(() =>
            {
                _multiplexer.SelectDevice(_multiplexerDevice);
                _i2cDevice.WriteRead(new byte[] { (byte)(address & 0xFF) }, data);
            });

            return (data[0] << 8) | data[1];
        }

        private void _write_u8(ushort address, ushort val)
        {
            I2CSynchronous.Call(() =>
            {
                _multiplexer.SelectDevice(_multiplexerDevice);
                //Write an 8-bit unsigned value to the specified 8-bit address.
                _i2cDevice.Write(new byte[] { (byte)(address & 0xFF), (byte)(val & 0xFF) });
            });
        }

        private void _write_u16(byte address, byte val)
        {
            I2CSynchronous.Call(() =>
            {
                _multiplexer.SelectDevice(_multiplexerDevice);
                //Write a 16-bit BE unsigned value to the specified 8-bit address.
                _i2cDevice.Write(new byte[] {
                    (byte)(address & 0xFF),
                    (byte)((val >> 8) & 0xFF),
                    (byte)(val & 0xFF)
                    });
            });
        }

        private SpadInfo _get_spad_info()
        {
            // Get reference SPAD count and type, returned as a 2-tuple of
            // count and boolean is_aperture.  Based on code from:
            // https://github.com/pololu/vl53l0x-arduino/blob/master/VL53L0X.cpp

            _write_u8(0x80, 0x01);
            _write_u8(0xFF, 0x01);
            _write_u8(0x00, 0x00);
            _write_u8(0xFF, 0x06);
            _write_u8(0x83, (byte)(_read_u8(0x83) | 0x04));
            _write_u8(0xFF, 0x07);
            _write_u8(0x81, 0x01);
            _write_u8(0x80, 0x01);
            _write_u8(0x94, 0x6b);
            _write_u8(0x83, 0x00);

            var start = new Stopwatch();
            start.Start();

            while (_read_u8(0x83) == 0x00)
            {
                if (io_timeout_s > 0 && start.ElapsedMilliseconds >= io_timeout_s)
                    throw new Exception("Timeout waiting for VL53L0X!");
            }

            _write_u8(0x83, 0x01);
            var tmp = _read_u8(0x92);
            var count = tmp & 0x7F;
            var is_aperture = ((tmp >> 7) & 0x01) == 1;

            _write_u8(0x81, 0x00);
            _write_u8(0xFF, 0x06);
            _write_u8(0x83, (byte)(_read_u8(0x83) & ~0x04));
            _write_u8(0xFF, 0x01);
            _write_u8(0x00, 0x01);
            _write_u8(0xFF, 0x00);
            _write_u8(0x80, 0x00);

            return new SpadInfo { count = count, is_aperture = is_aperture };
        }

        private void _perform_single_ref_calibration(byte vhv_init_byte)
        {
            // based on VL53L0X_perform_single_ref_calibration() from ST API.
            _write_u8(_SYSRANGE_START, (byte)(0x01 | vhv_init_byte & 0xFF));

            var start = new Stopwatch();
            start.Start();

            while ((_read_u8(_RESULT_INTERRUPT_STATUS) & 0x07) == 0)
            {
                if (io_timeout_s > 0 && start.ElapsedMilliseconds >= io_timeout_s)
                    throw new Exception("Timeout waiting for VL53L0X!");
            }

            _write_u8(_SYSTEM_INTERRUPT_CLEAR, 0x01);
            _write_u8(_SYSRANGE_START, 0x00);
        }

        private int _get_vcsel_pulse_period(int vcsel_period_type)
        {
            if (vcsel_period_type == _VCSEL_PERIOD_PRE_RANGE)
            {
                var val = _read_u8(_PRE_RANGE_CONFIG_VCSEL_PERIOD);
                return (((val) + 1) & 0xFF) << 1;
            }
            else if (vcsel_period_type == _VCSEL_PERIOD_FINAL_RANGE)
            {
                var val = _read_u8(_FINAL_RANGE_CONFIG_VCSEL_PERIOD);
                return (((val) + 1) & 0xFF) << 1;
            }

            return 255;
        }

        private SequenceStepEnables _get_sequence_step_enables()
        {
            // based on VL53L0X_GetSequenceStepEnables() from ST API
            var sequence_config = _read_u8(_SYSTEM_SEQUENCE_CONFIG);
            var tcc = ((sequence_config >> 4) & 0x1) > 0;
            var dss = ((sequence_config >> 3) & 0x1) > 0;
            var msrc = ((sequence_config >> 2) & 0x1) > 0;
            var pre_range = ((sequence_config >> 6) & 0x1) > 0;
            var final_range = ((sequence_config >> 7) & 0x1) > 0;

            return new SequenceStepEnables
            {
                tcc = tcc,
                dss = dss,
                msrc = msrc,
                pre_range = pre_range,
                final_range = final_range
            };
        }

        private SequenceStepTimeouts _get_sequence_step_timeouts(bool pre_range)
        {
            // based on get_sequence_step_timeout() from ST API but modified by
            // pololu here:
            // https://github.com/pololu/vl53l0x-arduino/blob/master/VL53L0X.cpp

            var pre_range_vcsel_period_pclks = _get_vcsel_pulse_period(_VCSEL_PERIOD_PRE_RANGE);
            var msrc_dss_tcc_mclks = (_read_u8(_MSRC_CONFIG_TIMEOUT_MACROP) + 1) & 0xFF;
            var msrc_dss_tcc_us = _timeout_mclks_to_microseconds(msrc_dss_tcc_mclks, pre_range_vcsel_period_pclks);
            var pre_range_mclks = _decode_timeout(_read_u16(_PRE_RANGE_CONFIG_TIMEOUT_MACROP_HI));
            var pre_range_us = _timeout_mclks_to_microseconds(pre_range_mclks, pre_range_vcsel_period_pclks);
            var final_range_vcsel_period_pclks = _get_vcsel_pulse_period(_VCSEL_PERIOD_FINAL_RANGE);
            var final_range_mclks = _decode_timeout(_read_u16(_FINAL_RANGE_CONFIG_TIMEOUT_MACROP_HI));

            if (pre_range)
                final_range_mclks -= pre_range_mclks;

            var final_range_us = _timeout_mclks_to_microseconds(final_range_mclks, final_range_vcsel_period_pclks);

            return new SequenceStepTimeouts
            {
                msrc_dss_tcc_us = msrc_dss_tcc_us,
                pre_range_us = pre_range_us,
                final_range_us = final_range_us,
                final_range_vcsel_period_pclks = final_range_vcsel_period_pclks,
                pre_range_mclks = pre_range_mclks
            };
        }
        
        // Set the return signal rate limit check value in units of MCPS (mega counts
        // per second). "This represents the amplitude of the signal reflected from the
        // target and detected by the device"; setting this limit presumably determines
        // the minimum measurement necessary for the sensor to report a valid reading.
        // Setting a lower limit increases the potential range of the sensor but also
        // seems to increase the likelihood of getting an inaccurate reading because of
        // unwanted reflections from objects other than the intended target.
        // Defaults to 0.25 MCPS as initialized by the ST API and this library.
        private void signal_rate_limit(float limit_Mcps)
        {
            //Min 0.1 Max 511.99
            //Min maybe could be lower

            // Q9.7 fixed point format (9 integer bits, 7 fractional bits)
            _write_u16(_FINAL_RANGE_CONFIG_MIN_COUNT_RATE_RTN_LIMIT, (byte)(limit_Mcps * (1 << 7)));
        }

        // Get the return signal rate limit check value in MCPS
        public float signal_rate_limit()
        {
            return (float)_read_u16(_FINAL_RANGE_CONFIG_MIN_COUNT_RATE_RTN_LIMIT) / (1 << 7);
        }

        private double measurement_timing_budget()
        {
            //"""The measurement timing budget in microseconds."""
            var budget_us = 1910 + 960d; // Start overhead + end overhead.
            var sse = _get_sequence_step_enables();
            var st = _get_sequence_step_timeouts(sse.pre_range);

            if (sse.tcc)
                budget_us += (st.msrc_dss_tcc_us + 590);
            if (sse.dss)
                budget_us += 2 * (st.msrc_dss_tcc_us + 690);
            else if (sse.msrc)
                budget_us += (st.msrc_dss_tcc_us + 660);
            if (sse.pre_range)
                budget_us += (st.pre_range_us + 660);
            if (sse.final_range)
                budget_us += (st.final_range_us + 550);

            _measurement_timing_budget_us = budget_us;
            return budget_us;
        }

        private void measurement_timing_budget(double budget_us)
        {
            var used_budget_us = 1320 + 960d; // Start (diff from get) + end overhead
            var sse = _get_sequence_step_enables();
            var st = _get_sequence_step_timeouts(sse.pre_range);

            if (sse.tcc)
                used_budget_us += (st.msrc_dss_tcc_us + 590);
            if (sse.dss)
                used_budget_us += 2 * (st.msrc_dss_tcc_us + 690);
            else if (sse.msrc)
                used_budget_us += (st.msrc_dss_tcc_us + 660);
            if (sse.pre_range)
                used_budget_us += (st.pre_range_us + 660);
            if (sse.final_range)
            {
                used_budget_us += 550;
                // "Note that the final range timeout is determined by the timing
                // budget and the sum of all other timeouts within the sequence.
                // If there is no room for the final range timeout, then an error
                // will be set. Otherwise the remaining time will be applied to
                // the final range."
                if (used_budget_us > budget_us)
                    throw new Exception("Requested timeout too big.");

                var final_range_timeout_us = budget_us - used_budget_us;
                var final_range_timeout_mclks = _timeout_mclks_to_microseconds(final_range_timeout_us, st.final_range_vcsel_period_pclks);

                if (sse.pre_range)
                    final_range_timeout_mclks += st.pre_range_mclks;

                _write_u16(_FINAL_RANGE_CONFIG_TIMEOUT_MACROP_HI, (byte)_encode_timeout(final_range_timeout_mclks));
                _measurement_timing_budget_us = budget_us;
            }
        }

        /// <summary>
        /// Returns distance in millimeters
        /// </summary>
        /// <returns></returns>
        public int GetDistance()
        {
            //"""Perform a single reading of the range for an object in front of
            //the sensor and return the distance in millimeters.
            //"""
            // Adapted from readRangeSingleMillimeters &
            // readRangeContinuousMillimeters in pololu code at:
            // https://github.com/pololu/vl53l0x-arduino/blob/master/VL53L0X.cpp

            _write_u8(0x80, 0x01);
            _write_u8(0xFF, 0x01);
            _write_u8(0x00, 0x00);
            _write_u8(0x91, _stop_variable);
            _write_u8(0x00, 0x01);
            _write_u8(0xFF, 0x00);
            _write_u8(0x80, 0x00);
            _write_u8(_SYSRANGE_START, 0x01);

            var start = new Stopwatch();
            start.Start();

            while ((_read_u8(_SYSRANGE_START) & 0x01) > 0)
            {
                if (io_timeout_s > 0 && start.ElapsedMilliseconds >= io_timeout_s)
                    throw new Exception("Timeout waiting for VL53L0X!");
            }

            start.Restart();

            while ((_read_u8(_RESULT_INTERRUPT_STATUS) & 0x07) == 0)
            {
                if (io_timeout_s > 0 && start.ElapsedMilliseconds >= io_timeout_s)
                    throw new Exception("Timeout waiting for VL53L0X!");
            }
            
            // assumptions: Linearity Corrective Gain is 1000 (default)
            // fractional ranging is not enabled
            var range_mm = _read_u16(_RESULT_RANGE_STATUS + 10);
            _write_u8(_SYSTEM_INTERRUPT_CLEAR, 0x01);

            // Limit max value for distance laser sensor top.
            // Distance laser sensors looking at floor not always deliver a reading (timeout max value was to high).
            if (_multiplexerDevice == MultiplexerDevice.DistanceLaserSensorTop
                && range_mm > _RANGE_MAX_VALUE)
            {
                range_mm = _RANGE_MAX_VALUE;
            }

            return range_mm;
        }

        public int GetDistanceFiltered()
        {
            var distance = 0;

            if (_readings.Count >= 5)
            {
                _readings.RemoveAt(0);
            }
            else
            {
                while (_readings.Count < 5)
                {
                    distance = GetDistance();
                    _readings.Add(distance);
                }
            }

            distance = GetDistance();
            _readings.Add(distance);

            var distanceFiltered = Convert.ToInt32(_readings.Average());
            return distanceFiltered;
        }

        public void ClearDistancesFiltered()
        {
            _readings.Clear();
        }
    }

    public class SpadInfo
    {
        public int count { get; set; }
        public bool is_aperture { get; set; }
    }

    public class SequenceStepEnables
    {
        public bool tcc { get; set; }
        public bool dss { get; set; }
        public bool msrc { get; set; }
        public bool pre_range { get; set; }
        public bool final_range { get; set; }

    }

    public class SequenceStepTimeouts
    {
        public double msrc_dss_tcc_us { get; set; }
        public double pre_range_us { get; set; }
        public double final_range_us { get; set; }
        public int final_range_vcsel_period_pclks { get; set; }
        public double pre_range_mclks { get; set; }
    }

    public enum Mode
    {
        HIGH_ACCURACY = 200000, //200ms
        HIGH_SPEED = 20000, //20ms
        DEFAULT = 33000 //33ms
    }

    public enum LightResponse
    {
        HIGH = 1,
        LOW = 0
    }

    public static class LightResponseValues
    {
        public const float HIGH = 511.99f;
        public const float LOW = 0.1f;

        public static float GetLightResponseValue(LightResponse lightResponse)
        {
            var lightResponseValue = lightResponse == LightResponse.HIGH ? HIGH : LOW;
            return lightResponseValue;
        }
    }
}