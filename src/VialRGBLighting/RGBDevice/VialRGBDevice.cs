using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using RGB.NET.Core;
using SharpVialRGB;

namespace Artemis.Plugins.Devices.VialRGB;

public class VialRGBDevice : AbstractRGBDevice<VialRGBDeviceInfo>
{
    public VialRGBDevice(VialRGBDeviceInfo deviceInfo, IUpdateQueue updateQueue) : base(deviceInfo, updateQueue)
    {
        InitializeLayout();
    }

    private void InitializeLayout()
    {
        Size ledSize = new(18);

        LedId startCustom = LedId.Custom81;

        foreach (var led in DeviceInfo.RawDevice.Leds)
        {
            bool validLed = LedMappings.DEFAULT.TryGetValue(led.Keycode, out LedId id);

            if (!validLed && startCustom != LedId.Custom1024)
            {
                id = startCustom;
                startCustom++;
            }
            
            AddLed(id, new Point(led.X*2, led.Y*2), ledSize, led);
        }
    }

    public override void Dispose()
    {
        DeviceInfo.RawDevice.Dispose();
        
        base.Dispose();
    }
}