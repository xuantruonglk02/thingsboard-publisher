using ThingsBoardPublisher.Models;
using Newtonsoft.Json;

namespace ThingsBoardPublisher.Services;

public class FileStorageService
{
    private readonly IHostEnvironment _environment;

    public FileStorageService(IHostEnvironment environment)
    {
        _environment = environment;
        var filePath = Path.Combine(_environment.ContentRootPath, "FileStorage", "file-queue.json");
        if (!Directory.Exists(Path.Combine(_environment.ContentRootPath, "FileStorage")))
        {
            Directory.CreateDirectory(Path.Combine(_environment.ContentRootPath, "FileStorage"));
        }
        if (!File.Exists(filePath))
        {
            File.Create(filePath).Dispose();
        }

        var fileUpdatePath = Path.Combine(_environment.ContentRootPath, "FileStorage", "file-updated.json");
        if (!Directory.Exists(Path.Combine(_environment.ContentRootPath, "FileStorage")))
        {
            Directory.CreateDirectory(Path.Combine(_environment.ContentRootPath, "FileStorage"));
        }
        if (!File.Exists(fileUpdatePath))
        {
            File.Create(fileUpdatePath).Dispose();
        }
    }

    public List<FileModel> GetFileQueue()
    {
        var filePath = Path.Combine(_environment.ContentRootPath, "FileStorage", "file-queue.json");
        string jsonString = File.ReadAllText(filePath);
        var fileStorage = JsonConvert.DeserializeObject<List<FileModel>>(jsonString);
        return fileStorage ?? new List<FileModel>();
    }

    public List<FileUpdate> GetFilesUpdate()
    {
        var fileUpdatePath = Path.Combine(_environment.ContentRootPath, "FileStorage", "file-updated.json");
        var jsonString = File.ReadAllText(fileUpdatePath);
        var fileUpdateStorage = JsonConvert.DeserializeObject<List<FileUpdate>>(jsonString);
        return fileUpdateStorage ?? new List<FileUpdate>();
    }

    public void SaveOrUpdateFileModel(FileModel fileModel)
    {
        var filePath = Path.Combine(_environment.ContentRootPath, "FileStorage", "file-queue.json");
        var fileStorage = GetFileQueue();
        if (fileStorage is null) fileStorage = new List<FileModel>();
        var item = fileStorage.FirstOrDefault(x => x.SourceFilePath == fileModel.SourceFilePath);
        if (item != null)
        {
            fileStorage.Remove(item);
        }
        fileStorage.Add(fileModel);
        string jsonString = JsonConvert.SerializeObject(fileStorage);
        File.WriteAllText(filePath, jsonString);
    }

    public void SaveOrUpdateFileUpdate(FileUpdate file)
    {
        var filePath = Path.Combine(_environment.ContentRootPath, "FileStorage", "file-updated.json");
        var fileUpdateStorage = GetFilesUpdate();
        if (fileUpdateStorage is null) fileUpdateStorage = new List<FileUpdate>();
        var item = fileUpdateStorage.FirstOrDefault(x => x.FilePath == file.FilePath);
        if (item != null)
        {
            fileUpdateStorage.Remove(item);
        }
        fileUpdateStorage.Add(file);
        string jsonString = JsonConvert.SerializeObject(fileUpdateStorage);
        File.WriteAllText(filePath, jsonString);
    }
}
