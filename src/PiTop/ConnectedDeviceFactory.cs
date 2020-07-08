﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace PiTop
{
    public class ConnectedDeviceFactory<TConnectionConfiguration, TDevice> : IConnectedDeviceFactory<TConnectionConfiguration, TDevice>
        where TConnectionConfiguration: notnull
        where TDevice : IConnectedDevice
    {
        private readonly Dictionary<TConnectionConfiguration, TDevice> _devices = new Dictionary<TConnectionConfiguration, TDevice>();
        private readonly Func<Type, Func<TConnectionConfiguration, TDevice>> _defaultDeviceFactoryGenerator;
        private readonly Dictionary<Type, Func<TConnectionConfiguration, TDevice>> _factories = new Dictionary<Type, Func<TConnectionConfiguration, TDevice>>();
        public ConnectedDeviceFactory(Func<Type, Func<TConnectionConfiguration, TDevice>>? defaultDeviceFactoryGenerator = null)
        {

            defaultDeviceFactoryGenerator ??= deviceType =>
            {
                var ctorSignature = new[] { typeof(TConnectionConfiguration) };
                var ctor = deviceType.GetConstructor(ctorSignature);
                if (ctor != null)
                {
                    return devicePort => (TDevice)Activator.CreateInstance(deviceType, devicePort)!;
                }

                throw new InvalidOperationException(
                    $"Cannot find suitable constructor for type {deviceType}, looking for signature {ctorSignature}");
            };

            _defaultDeviceFactoryGenerator =
                deviceType =>
                {
                    if (!typeof(TDevice).IsAssignableFrom(deviceType))
                    {
                        throw new InvalidOperationException(
                            $"Device of type ${deviceType} do not derive from {typeof(TDevice)}");
                    }

                    return defaultDeviceFactoryGenerator(deviceType);

                };
        }

        public T GetOrCreateDevice<T>(TConnectionConfiguration connectionConfiguration) where T : TDevice
        {
            if (!_factories.TryGetValue(typeof(T), out var factory))
            {
                factory = _defaultDeviceFactoryGenerator(typeof(T));
                _factories[typeof(T)] = factory;
            }

            return GetOrCreateDevice<T>(connectionConfiguration, factory);
        }

        public T GetOrCreateDevice<T>(TConnectionConfiguration connectionConfiguration, Func<TConnectionConfiguration, TDevice> deviceFactory) where T : TDevice
        {
            if (deviceFactory == null)
            {
                throw new ArgumentNullException(nameof(deviceFactory));
            }

            if (!_devices.TryGetValue(connectionConfiguration, out var device))
            {
                device = deviceFactory(connectionConfiguration);
                _devices[connectionConfiguration] = device;
                device.Initialize();
            }

            return (T)device;

        }

        public IEnumerable<TDevice> Devices => _devices.Select(e => e.Value);
        public void DisposeDevice<T>(T device) where T : TDevice
        {
            var key = _devices.FirstOrDefault(e => ReferenceEquals(e.Value,device)).Key;
            _devices.Remove(key);
            device.Dispose();
        }

        public void Dispose()
        {
            var devices = _devices.Select(e => e.Value).ToList();

            foreach (var disposable in devices)
            {
                disposable.Dispose();
            }
        }
    }
}