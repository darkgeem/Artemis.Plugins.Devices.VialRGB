using System;
using System.Collections.Generic;
using System.Linq;
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
	
	private readonly Dictionary<string, VialRGBDevice> _devices;
	private System.Threading.Timer? _deviceMonitor;
	private readonly object _devicesLock = new object();
	
	#endregion
	
	#region Constructors

	public VialRgbDeviceProvider()
	{
		if(_instance != null) throw new InvalidOperationException($"There can be only one instance of type {nameof(VialRgbDeviceProvider)}");
		_instance = this;

		_devices = new Dictionary<string, VialRGBDevice>();
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

			// Build set of currently available device serials and test connectivity
			foreach (var dev in currentDevices)
			{
				try
				{
					dev.Connect();
					if (dev.Connected)
					{
						currentSerials.Add(dev.Serial);
					}
					dev.Dispose();
				}
				catch { }
			}
			
			lock (_devicesLock)
			{
				// Check for disconnected devices
				var disconnectedSerials = _devices.Keys.Where(serial => !currentSerials.Contains(serial)).ToList();
				foreach (var serial in disconnectedSerials)
				{
					if (_devices.TryGetValue(serial, out var device))
					{
						try
						{
							RemoveDevice(device);
							device.Dispose();
							_devices.Remove(serial);
						}
						catch { }
					}
				}

				// Check for new devices
				foreach (var vialDevice in currentDevices)
				{
					if (!_devices.ContainsKey(vialDevice.Serial) && currentSerials.Contains(vialDevice.Serial))
					{
						try
						{
							vialDevice.Connect();
							if (!vialDevice.Connected)
							{
								vialDevice.Dispose();
								continue;
							}

							vialDevice.EnableDirectRgb();
							VialRGBUpdateQueue updateQueue = new VialRGBUpdateQueue(GetUpdateTrigger(), vialDevice);
							var newDevice = new VialRGBDevice(new VialRGBDeviceInfo(vialDevice), updateQueue);
							
							_devices.Add(vialDevice.Serial, newDevice);
							AddDevice(newDevice);
						}
						catch
						{
							try { vialDevice.Dispose(); } catch { }
						}
					}
					else if (_devices.ContainsKey(vialDevice.Serial))
					{
						// Device already tracked, dispose the duplicate
						try { vialDevice.Dispose(); } catch { }
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
					device.Connect();
					if (!device.Connected)
					{
						device.Dispose();
						continue;
					}

					device.EnableDirectRgb();
					VialRGBUpdateQueue updateQueue = new VialRGBUpdateQueue(GetUpdateTrigger(), device);
					
					vialDevice = new VialRGBDevice(new VialRGBDeviceInfo(device), updateQueue);
					_devices.Add(device.Serial, vialDevice);
				}
				catch
				{
					try { device.Dispose(); } catch { }
					continue;
				}
				
				if (vialDevice != null) 
					yield return vialDevice;
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
			foreach (var device in _devices.Values)
			{
				try { device.Dispose(); }
				catch { }
			}
			
			_devices.Clear();
		}
		
		_instance = null;
		
		base.Dispose(disposing);
	}

	#endregion
}
