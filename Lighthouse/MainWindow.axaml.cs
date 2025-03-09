using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Lighthouse;

public partial class MainWindow : Window
{
    private readonly BLEService bleService;
    
    public MainWindow()
    {
        bleService = new();
        
        InitializeComponent();

        CanResize = false;
        Devices.ItemsSource = bleService.Devices;
    }
    
    private LighthouseDeviceDescriptor[] SelectedDevices => Devices.SelectedItems!.OfType<LighthouseDeviceDescriptor>().ToArray();
    
    private readonly SemaphoreSlim @lock = new(1, 1);

    private void SwitchSelectedDevices(LighthouseState state)
    {
        Task.Run(async () =>
        {
            await @lock.WaitAsync();
            try
            {
                var tasks = SelectedDevices.Select(x => bleService.SetDeviceState(x, state));
                await Task.WhenAll(tasks);
            }
            finally
            {
                @lock.Release();
            }
        });
    }

    private void Enable_OnClick(object? sender, RoutedEventArgs e)
    {
        SwitchSelectedDevices(LighthouseState.On);
    }
    
    private void Standby_OnClick(object? sender, RoutedEventArgs e)
    {
        SwitchSelectedDevices(LighthouseState.Standby);
    }
    
    private void Disable_OnClick(object? sender, RoutedEventArgs e)
    {
        SwitchSelectedDevices(LighthouseState.Off);
    }

    private void Discover_OnClick(object? sender, RoutedEventArgs e)
    {
        Task.Run(async () =>
        {
            Dispatcher.UIThread.Post(() => { DiscoveryLabel.IsVisible = true; });
            
            await bleService.Discover();
            
            Dispatcher.UIThread.Post(() => { DiscoveryLabel.IsVisible = false; });
        });
    }
}