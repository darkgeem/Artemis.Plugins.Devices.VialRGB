using Artemis.Core;
using Artemis.Core.DeviceProviders;
using Artemis.Core.Services;
using RGB.NET.Core;
using Serilog;
using RGBDeviceProvider = Artemis.Plugins.Devices.VialRGB.VialRgbDeviceProvider;

namespace Artemis.Plugins.Devices.VialRGB;

[PluginFeature(Name = "VialRGB Device Provider")]
public class VialDeviceProvider : DeviceProvider
{
    private readonly ILogger _logger;
    private readonly IDeviceService _deviceService;
    
    public override RGBDeviceProvider RgbDeviceProvider => RGBDeviceProvider.Instance;

    public VialDeviceProvider(IDeviceService deviceService, ILogger logger)
    {
        _deviceService = deviceService;
        _logger = logger;
    }
    
    public override void Enable()
    {
        RgbDeviceProvider.Exception += RgbDeviceProviderOnException;
        _deviceService.AddDeviceProvider(this);
    }

    public override void Disable()
    {
        RgbDeviceProvider.Exception -= RgbDeviceProviderOnException;
        _deviceService.RemoveDeviceProvider(this);
        RgbDeviceProvider.Dispose();
    }
    
    private void RgbDeviceProviderOnException(object? sender, ExceptionEventArgs args)
    {
        _logger.Debug(args.Exception, "VialRGB Exception: {message}", args.Exception.Message);
    }
}