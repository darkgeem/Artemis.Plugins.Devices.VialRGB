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

    protected override IDeviceUpdateTrigger CreateUpdateTrigger(int id, double updateRateHardLimit)
    {
        return new DeviceUpdateTrigger(1.0/30.0);
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var device in _devices)
        {
            try { device.Dispose(); }
            catch{ /* welp */ }
        }
        
        _devices.Clear();
        _instance = null;
        
        base.Dispose(disposing);
    }

    #endregion
}
