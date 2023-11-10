using ThingsBoardPublisher.Commons;
using ThingsBoardPublisher.Models;
using ThingsBoardPublisher.Services;
using Serilog;
using ThingsBoardPublisher.Settings;
using Microsoft.Extensions.Options;

namespace ThingsBoardPublisher.BackgroundServices;

public class FileChangeBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly FileStorageService _fileStorage;
    private List<DeviceClient> _devices = new List<DeviceClient>();

    public FileChangeBackgroundService(IConfiguration configuration, FileStorageService fileStorage, IOptions<PushSetting> pushSetting)
    {
        _configuration = configuration;
        _fileStorage = fileStorage;
        _devices = pushSetting.Value.Devices;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var dirPath = _configuration.GetSectionValue("DataPath");
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
            Log.Logger.Information($"Create folder: {dirPath}");
        }
        Log.Logger.Information($"Monitor folder: {dirPath}");
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                string[] files = Directory.GetFiles(dirPath);
                var fileUpdates = _fileStorage.GetFilesUpdate();
                foreach (string file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var isConfig = _devices?.Any(x => x.FileName.Contains(fileName)) ?? false;
                    if (!file.Contains(".bak") && _devices != null && _devices.Count > 0 && isConfig)
                    {
                        var fileItem = fileUpdates.FirstOrDefault(x => x.FilePath == file);
                        if (fileItem != null)
                        {
                            // Check update
                            DateTime lastModified = File.GetLastWriteTime(file);
                            if (lastModified > fileItem.DateUpdate)
                            {
                                FileQueue.FileChanges.Enqueue(file);
                                fileItem.DateUpdate = lastModified;
                                _fileStorage.SaveOrUpdateFileUpdate(fileItem);
                                Log.Logger.Information("File change: " + file);
                            }
                        }
                        else
                        {
                            FileQueue.FileChanges.Enqueue(file);
                            fileItem = new FileUpdate { FilePath = file, DateUpdate = DateTime.Now };
                            _fileStorage.SaveOrUpdateFileUpdate(fileItem);
                            Log.Logger.Information("File monitor: " + file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error("FileChangeService error occurred: {Exception}", ex.FlattenException());
            }

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Logger.Information("FileChangeService start");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Logger.Information("FileChangeService stoped");
        return base.StopAsync(cancellationToken);
    }
}
