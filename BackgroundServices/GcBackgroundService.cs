using Serilog;

namespace ThingsBoardPublisher.BackgroundServices;

public class GcBackgroundService : BackgroundService
{
    private static double BytesDivider => 1048576.0;
    private readonly IConfiguration _configuration;

    public GcBackgroundService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Log some memory information.
                LogMemoryInformation();
                await Task.Delay(TimeSpan.FromSeconds(int.Parse(_configuration.GetSectionValue("GC_DelayInSeconds"))), cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Logger.Error("An error occurred: {Exception}", ex);
            }
        }
    }

    private void LogMemoryInformation()
    {
        var totalMemory = GC.GetTotalMemory(false);
        var memoryInfo = GC.GetGCMemoryInfo();
        var divider = BytesDivider;
        Log.Logger.Information(
            "Heartbeat for service OpsLog: Total {Total}, heap size: {HeapSize}, memory load: {MemoryLoad}.",
            $"{totalMemory / divider:N2} MB", $"{memoryInfo.HeapSizeBytes / divider:N2} MB", $"{memoryInfo.MemoryLoadBytes / divider:N2} MB");
        // Clean Memory if using 3GB mem
        if (totalMemory / divider > 3 * 1024) GC.Collect();
    }
}
