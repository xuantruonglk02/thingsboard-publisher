using MassTransit;
using ThingsBoardPublisher.Models;
using Newtonsoft.Json;
using Serilog;

namespace ThingsBoardPublisher.BackgroundServices;

/// <summary>
/// Lấy data và push to MQTT
/// </summary>
public class PushDataBackgroundService : BackgroundService
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IBus _bus;

    public PushDataBackgroundService(IHostEnvironment hostEnvironment, IBus bus)
    {
        _hostEnvironment = hostEnvironment;
        _bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        string dirPath = Path.Combine(_hostEnvironment.ContentRootPath, "Data");
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
            Log.Logger.Information($"Create folder: {dirPath}");
        }
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var files = Directory.GetFiles(dirPath).OrderByDescending(f => File.GetLastWriteTime(f)).Take(10).ToList();
                // Log.Logger.Information("File get: {file}",files.Count);
                if (files.Count > 0) await ProcessData(files);
                else await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            }
            catch (Exception ex)
            {
                Log.Logger.Error("PushDataBackgroundService error occurred: {Exception}", ex.FlattenException());
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    private async Task ProcessData(List<string> files)
    {
        var taskAll = new List<Task>();
        foreach (var file in files)
        {
            var task = Task.Run(async () =>
            {
                var dataPush = ReadDataFromFileTmp(file);
                foreach (var data in dataPush.data)
                {
                    var ms = new Message { index = dataPush.index, data = data };
                    await _bus.Publish(new MessageObject { Text = ms.ToBase64String() });
                }
                Log.Logger.Information($"Push done data in file: {file}");
                File.Delete(file);
            });
            taskAll.Add(task);
        }
        await Task.WhenAll(taskAll);
    }

    public DataTopicPush ReadDataFromFileTmp(string file)
    {
        var jsonString = File.ReadAllText(file);
        return JsonConvert.DeserializeObject<DataTopicPush>(jsonString);
    }
}
