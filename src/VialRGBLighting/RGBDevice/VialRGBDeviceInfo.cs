using RGB.NET.Core;
using SharpVialRGB;

namespace Artemis.Plugins.Devices.VialRGB;

public class VialRGBDeviceInfo : IRGBDeviceInfo
{
    private VialDevice _vialRGBDevice;
    
    public VialDevice RawDevice => _vialRGBDevice;
    public RGBDeviceType DeviceType { get; }
    public string DeviceName { get; }
    public string Manufacturer { get; }
    public string Model { get; }
    
    public object? LayoutMetadata { get; set; }

    public VialRGBDeviceInfo(VialDevice vialRGBDevice)
    {
        _vialRGBDevice = vialRGBDevice;
        
        DeviceType = RGBDeviceType.Keyboard;
        DeviceName = _vialRGBDevice.Name;
        Manufacturer = _vialRGBDevice.Manufacturer;
        Model = _vialRGBDevice.Name;
    }
}