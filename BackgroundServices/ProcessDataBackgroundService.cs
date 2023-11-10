using CsvHelper;
using CsvHelper.Configuration;
using ThingsBoardPublisher.Commons;
using ThingsBoardPublisher.Models;
using ThingsBoardPublisher.Services;
using Newtonsoft.Json;
using Serilog;
using System.Globalization;

namespace ThingsBoardPublisher.BackgroundServices;
/// <summary>
/// Chia nhỏ file trước khi push vào MQTT
/// </summary>
public class ProcessDataBackgroundService : BackgroundService
{
    private CsvConfiguration _csvConfiguration;
    private readonly IConfiguration _configuration;
    private readonly FileStorageService _fileStorage;
    private int pageSize = 100;
    private readonly IHostEnvironment _hostEnvironment;

    public ProcessDataBackgroundService(IConfiguration configuration, FileStorageService fileStorage, IHostEnvironment hostEnvironment)
    {
        _csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
        };
        _configuration = configuration;
        // pageSize = int.Parse(_configuration.GetSectionValue("PageSize"));
        pageSize = 100;
        _fileStorage = fileStorage;
        _hostEnvironment = hostEnvironment;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (FileQueue.FileNeedHandle.Count > 0)
                {
                    var file = FileQueue.FileNeedHandle.Dequeue();
                    Log.Logger.Information("Begin Process file des: " + file.DestinationFilePath);
                    await ProcessData(file);
                    Log.Logger.Information("Done Process file des: " + file.DestinationFilePath);
                }
                else await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Logger.Error("ProcessDataService An error occurred: {Exception}", ex);
            }
        }
    }

    private Task ProcessData(FileModel fileModel)
    {
        var index = $"{Path.GetFileNameWithoutExtension(fileModel.SourceFilePath).Slugify()}";
        if (fileModel.RowPointer == -1)
        {
            fileModel.RowPointer = 0;
        }
        using (var reader = new StreamReader(fileModel.DestinationFilePath))
        {
            using (var csv = new CsvReader(reader, _csvConfiguration))
            {
                var records = csv.GetRecords<object>().Skip(fileModel.RowPointer).ToList();
                if (records.Count > 0)
                {
                    var pageTotal = Math.Ceiling((decimal)records.Count / pageSize);
                    for (var i = 0; i < pageTotal; i++)
                    {
                        var pageNumber = i + 1;
                        var data = records.Skip((pageNumber - 1) * pageSize).Take(pageSize);
                        SaveDataToFile(new DataTopic { index = index, data = data }, Guid.NewGuid().ToString());
                    }
                }
                fileModel.RowPointer += records.Count;
                _fileStorage.SaveOrUpdateFileModel(fileModel);
            }
        }
        // Delete process file
        File.Delete(fileModel.DestinationFilePath);
        return Task.CompletedTask;
    }

    private void SaveDataToFile(DataTopic data, string guid = "")
    {
        string directoryPath = Path.Combine(_hostEnvironment.ContentRootPath, "Data");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        var fileName = Path.Combine(directoryPath, $"{data.index}-{DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmsss")}-{guid}.json");
        if (!File.Exists(fileName))
        {
            File.Create(fileName).Dispose();
        }
        File.WriteAllText(fileName, JsonConvert.SerializeObject(data));
    }
}
