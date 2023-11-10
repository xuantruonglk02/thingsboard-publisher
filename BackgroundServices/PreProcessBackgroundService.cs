using ThingsBoardPublisher.Commons;
using ThingsBoardPublisher.Models;
using ThingsBoardPublisher.Services;
using Serilog;

namespace ThingsBoardPublisher.BackgroundServices;

/// <summary>
/// Job kiểm tra và quản lý file dưới client
/// </summary>
public class PreProcessBackgroundService : BackgroundService
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly FileStorageService _fileStorage;

    public PreProcessBackgroundService(FileStorageService fileStorage, IHostEnvironment hostEnvironment)
    {
        _fileStorage = fileStorage;
        _hostEnvironment = hostEnvironment;
        var dirPath = Path.Combine(_hostEnvironment.ContentRootPath, "Temp");
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
            Log.Logger.Information($"Create folder: {dirPath}");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (FileQueue.FileChanges.Count > 0)
                {
                    var filePath = FileQueue.FileChanges.Dequeue();
                    PreProcessFile(filePath);
                }
                else await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Logger.Error("An error occurred: {Exception}", ex);
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Logger.Information("PreProcessService stoped");
        return base.StopAsync(cancellationToken);
    }

    // Pro process file --> validate lại file xem đúng DL cvs.
    private void PreProcessFile(string filePath)
    {
        Log.Logger.Information("PreProcessFile: " + filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        string destinationFilePath = Path.Combine(_hostEnvironment.ContentRootPath, "Temp", $"{fileName.Slugify()}-chunk-{DateTime.UtcNow.AddHours(7).ToString("yyyyMMdd-HHmm")}.csv");
        // File .dat
        var firstLine = string.Empty;
        var isDatFile = false;
        using (StreamReader reader = new StreamReader(filePath))
        {
            firstLine = reader.ReadLine();
            if (firstLine.Contains("TOA5"))
            {
                isDatFile = true;
                var numberLine = 1;
                using (StreamWriter writer = new StreamWriter(destinationFilePath))
                {
                    while (!reader.EndOfStream)
                    {
                        numberLine++;
                        string line = reader.ReadLine();
                        if (numberLine == 2 || numberLine >= 5)
                        {
                            writer.WriteLine(line);
                        }
                    }
                }
            }
        }
        
        // File CSV
        if (!isDatFile)
        {
            File.Copy(filePath, destinationFilePath, true);
        }

        var fileChecked = _fileStorage.GetFileQueue().FirstOrDefault(x => x.SourceFilePath == filePath);
        if (fileChecked != null)
        {
            var currentHeaderRow = firstLine.Split(",").Length;
            if (fileChecked.FieldNumber != currentHeaderRow)
            {
                // Thay doi so luong cot
                fileChecked.RowPointer = -1;
                Log.Logger.Information("Thay doi so luong cot file: " + filePath);
            }
            fileChecked.FieldNumber = currentHeaderRow;
            fileChecked.SourceFilePath = filePath;
            fileChecked.DestinationFilePath = destinationFilePath;
            _fileStorage.SaveOrUpdateFileModel(fileChecked);
            FileQueue.FileNeedHandle.Enqueue(fileChecked);
        }
        else
        {
            var file = new FileModel
            {
                SourceFilePath = filePath,
                DestinationFilePath = destinationFilePath,
                RowPointer = 0,
                FieldNumber = firstLine.Split(",").Length
            };
            _fileStorage.SaveOrUpdateFileModel(file);
            FileQueue.FileNeedHandle.Enqueue(file);
        }
    }
}
