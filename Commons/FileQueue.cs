using ThingsBoardPublisher.Models;

namespace ThingsBoardPublisher.Commons;

public class FileQueue
{
    public static Queue<string> FileChanges = new Queue<string>();
    public static Queue<FileModel> FileNeedHandle = new Queue<FileModel>();
}
