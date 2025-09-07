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
        
        devices = SharpVialRGB.VialRGB.GetAllDevices();

        if (devices == null)
            yield break;

        foreach (var device in devices)
        {
            VialRGBUpdateQueue updateQueue = new VialRGBUpdateQueue(GetUpdateTrigger(), device);

            device.Connect();
            device.EnableDirectRgb();
            
            var vialDevice = new VialRGBDevice(new VialRGBDeviceInfo(device), updateQueue);
            _devices.Add(vialDevice);

            yield return vialDevice;
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