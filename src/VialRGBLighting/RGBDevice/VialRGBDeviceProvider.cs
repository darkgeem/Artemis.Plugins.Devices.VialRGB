using System;
using System.Collections.Generic;
using RGB.NET.Core;
using SharpVialRGB;

namespace Artemis.Plugins.Devices.VialRGB;

public class VialRgbDeviceProvider : AbstractRGBDeviceProvider
{
    #region Properties & Fields
    
    private static VialRgbDeviceProvider? _instance;
    
    /// <summary>
    /// Gets the singleton <see cref="VialRgbDeviceProvider"/> instance.
    /// </summary>
    public static VialRgbDeviceProvider Instance
    {
        get
        {
            return _instance ?? new VialRgbDeviceProvider();
        }
    }
    
    private List<VialRGBDevice> _devices;
	private System.Threading.Timer? _deviceMonitor;
	private readonly object _devicesLock = new object();
    
    #endregion
    
    #region Constructors

    public VialRgbDeviceProvider()
    {
        if(_instance != null) throw new InvalidOperationException($"There can be only one instance of type {nameof(VialRgbDeviceProvider)}");
        _instance = this;

        _devices = new List<VialRGBDevice>();
    }
    
    #endregion
    
    #region Methods
    
    protected override void InitializeSDK()
    {
		// Start monitoring for device changes every 2 seconds
		_deviceMonitor = new System.Threading.Timer(
			CheckForDeviceChanges,
			null,
			TimeSpan.FromSeconds(2),
			TimeSpan.FromSeconds(2)
		);
    }

	private void CheckForDeviceChanges(object? state)
	{
		try
		{
			var currentDevices = SharpVialRGB.VialRGB.GetAllDevices();
			var currentSerials = new HashSet<string>();

			// Build set of currently available device serials
			foreach (var dev in currentDevices)
			{
				try
				{
					dev.Connect();
					currentSerials.Add(dev.Serial);
					dev.Dispose();
				}
				catch { }
			}
			
			lock (_devicesLock)
			{
				// Check for disconnected devices
				for (int i = _devices.Count - 1; i >= 0; i--)
				{
					var device = _devices[i];
					if (!currentSerials.Contains(device.DeviceInfo.RawDevice.Serial))
					{
						RemoveDevice(device);
						device.Dispose();
					}
					catch { }
					_devices.RemoveAt(i);
				}

				// Check for new devices
				var existingSerials = new HashSet<string>();
				foreach (var dev in _devices)
				{
					existingSerials.Add(dev.DeviceInfo.RawDevice.Serial);
				}

				foreach (var vialDevice in currentDevices)
				{
					if (!existingSerials.Contains(vialDevice.Serial))
					{
						try
						{
							VialRGBUpdateQueue updateQueue = new VialRGBUpdateQueue(GetUpdateTrigger(), vialDevice);
							vialDevice.Connect();
							vialDevice.EnableDirectRgb();

							var newDevice = new VialRGBDevice(new VialRGBDeviceInfo(vialDevice), updateQueue);
							_devices.Add(newDevice);
							AddDevice(newDevice);
						}
						catch
						{
							try { vialDevice.Dispose(); } catch { }
						}
					}
				}
			}
		}
		catch
		{
			// Monitoring failed, but don't crash
		}
	}

    protected override IEnumerable<IRGBDevice> LoadDevices()
    {
        VialDevice[]? devices = null;
        
		try
		{
        	devices = SharpVialRGB.VialRGB.GetAllDevices();
		}
		catch
		{
			// Log the exception if you have logging available
			yield break;
		}

        if (devices == null)
            yield break;

		lock (_devicesLock)
		{
			foreach (var device in devices)
			{
				VialRGBDevice? vialDevice = null;
				try
				{
					VialRGBUpdateQueue updateQueue = new VialRGBUpdateQueue(GetUpdateTrigger(), device);

					device.Connect();
					device.EnableDirectRgb();
					
					vialDevice = new VialRGBDevice(new VialRGBDeviceInfo(device), updateQueue);
					_devices.Add(vialDevice);
				}
				catch
				{
					// Log the exception and continue with next device
					// This ensures one failing device doesn't crash the entire provider
					try { device.Dispose(); } catch { }
					continue;
				}
				// Only yield if device was successfully created
				if (vialDevice != null) yield return vialDevice;
			}
		}
    }

    protected override IDeviceUpdateTrigger CreateUpdateTrigger(int id, double updateRateHardLimit)
    {
        return new DeviceUpdateTrigger(1.0/30.0);
    }

    protected override void Dispose(bool disposing)
    {
		_deviceMonitor?.Dispose();
		_deviceMonitor = null;
		lock (_devicesLock)
		{
			foreach (var device in _devices)
			{
				try { device.Dispose(); }
				catch{ /* welp */ }
			}
			
			_devices.Clear();
		}
        _instance = null;
        
        base.Dispose(disposing);
    }

    #endregion
}
