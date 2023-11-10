namespace ThingsBoardPublisher.Models;

public class FileModel
{
    public string SourceFilePath { get; set; }
    public string DestinationFilePath { get; set; }
    public int RowPointer { get; set; }
    public int FieldNumber { get; set; }
}
