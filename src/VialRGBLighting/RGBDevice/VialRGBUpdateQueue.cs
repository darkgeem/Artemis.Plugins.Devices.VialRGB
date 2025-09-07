using System;
using RGB.NET.Core;
using SharpVialRGB;

namespace Artemis.Plugins.Devices.VialRGB;

public class VialRGBUpdateQueue : UpdateQueue
{
    private readonly VialDevice _vialDevice;
    
    public VialRGBUpdateQueue(IDeviceUpdateTrigger updateTrigger, VialDevice device) : base(updateTrigger)
    {
        _vialDevice = device;
    }


    protected override bool Update(ReadOnlySpan<(object key, Color color)> dataSet)
    {
        if (!_vialDevice.Connected)
            return false;

        try
        {
            foreach ((object key, Color color) in dataSet)
            {
                if (key is VialRgbLed led)
                {
                    var (hue,saturation,value) = color.GetHSV();
                    led.Hue =  (byte)(hue / 360.0f * 255.0f);
                    led.Saturation = (byte)(saturation * 255.0f);
                    led.Value = (byte)(value * 255.0f);
                }
            }
        
            _vialDevice.Update();
        }
        catch (Exception ex)
        {
            VialRgbDeviceProvider.Instance.Throw(ex);
        }
        
        
        return true;
    }
}