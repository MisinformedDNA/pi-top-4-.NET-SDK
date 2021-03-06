﻿using System;

using PiTop.Abstractions;

namespace PiTop.MakerArchitecture.Foundation.Sensors
{
    public class Potentiometer : AnaloguePortDeviceBase
    {
        private readonly bool _normalizeValue;
        private readonly AnalogueDigitalConverter _adc;

        public Potentiometer(AnaloguePort port, int deviceAddress, II2CDeviceFactory i2CDeviceFactory) : this(port, deviceAddress, i2CDeviceFactory, true)
        {
        }

        public Potentiometer(AnaloguePort port, int deviceAddress, II2CDeviceFactory i2CDeviceFactory, bool normalizeValue = true) : base(port, deviceAddress, i2CDeviceFactory)
        {
            _normalizeValue = normalizeValue;
            var (pin1, _) = Port.ToPinPair();
            var bus = I2CDeviceFactory.GetOrCreateI2CDevice(deviceAddress);
            _adc = new AnalogueDigitalConverter(bus, pin1);

            AddToDisposables(_adc);
        }

        public double Position => ReadValue();

        private double ReadValue()
        {
            var value = _adc.ReadSample();
            return Math.Round(_normalizeValue ? value / 999.0 : value, 2);
        }
    }
}
