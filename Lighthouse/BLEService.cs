using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Extensions;

namespace Lighthouse;

// ReSharper disable once InconsistentNaming
public class BLEService
{
    public BLEService()
    {
        adapter = CrossBluetoothLE.Current.Adapter;

        adapter.ScanMatchMode = ScanMatchMode.AGRESSIVE;
        adapter.ScanTimeout = (int)TimeSpan.FromSeconds(15).TotalMilliseconds;
        adapter.DeviceDiscovered += OnDeviceDiscovered;
    }
    
    private readonly IAdapter adapter;
    public readonly ObservableCollection<LighthouseDeviceDescriptor> Devices = [];
    private readonly Guid stateCharacteristicGuid = new("00001525-1212-EFDE-1523-785FEABCD124");

    public async Task Discover()
    {
        await adapter.StopScanningForDevicesAsync();
        Console.WriteLine("Scanning...");
        await adapter.StartScanningForDevicesAsync();
        Console.WriteLine("Scan finished.");
    }

    private void OnDeviceDiscovered(object? sender, DeviceEventArgs @event)
    {
        var device = @event.Device;

        Task.Run(() => TryTrack(device));
    }

    private async Task TryTrack(IDevice device)
    {
        if (!device.Name.StartsWith("LHB-"))
            return;
        
        if(Devices.Any(x => x.Name == device.Name))
            return;

        try
        {
            await adapter.ConnectToDeviceAsync(device);

            var services = await device.GetServicesAsync();
            foreach (var service in services)
            {
                try
                {
                    var characteristic = await service.GetCharacteristicAsync(stateCharacteristicGuid);
                    if (characteristic is null)
                        continue;

                    if (!characteristic.CanWrite)
                        continue;

                    await characteristic.ReadAsync();
                    
                    var mac = Utilities.BLEHexToMAC(device.Id.ToHexBleAddress());
                    Devices.Add(new()
                    {
                        Device = device,
                        StateCharacteristic = characteristic
                    });
                    Console.WriteLine($"Device discovered: {device.Name} [{mac}]");
                    
                    break;
                }
                catch (Exception exception)
                {
                    if (exception.Message.Contains("AccessDenied"))
                        continue;
                    Console.WriteLine(exception);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error tracking device {device.Name}: {ex.Message}");
        }
        finally
        {
            await adapter.DisconnectDeviceAsync(device);
        }
    }

    public async Task SetDeviceState(LighthouseDeviceDescriptor deviceDescriptor, LighthouseState state)
    {
        var device = deviceDescriptor.Device;
        try
        {
            await adapter.ConnectToDeviceAsync(device);

            var services = await device.GetServicesAsync();
            foreach (var service in services)
            {
                var characteristic = await service.GetCharacteristicAsync(stateCharacteristicGuid);

                if (characteristic is null) continue;

                if (!characteristic.CanWrite) continue;

                byte[] data = [(byte)state];

                deviceDescriptor.StateCharacteristic = characteristic;
                
                await characteristic.WriteAsync(data);
                
                deviceDescriptor.StateOverride = state;
                
                var idx = Devices.IndexOf(deviceDescriptor);
                Devices.Move(idx, idx); // update and remove selection

                Console.WriteLine($"Data written successfully to device: {device.Name}");
                break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to device {device.Name}: {ex.Message}");
        }
        finally
        {
            await adapter.DisconnectDeviceAsync(device);
        }
    }
}