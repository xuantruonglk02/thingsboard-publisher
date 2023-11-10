using System.Dynamic;

namespace ThingsBoardPublisher.Models;

public class DataTopic
{
    public string index { get; set; }
    public IEnumerable<object> data { get; set; }
}

public class DataTopicPush
{
    public string index { get; set; }
    public IEnumerable<ExpandoObject> data { get; set; }
}
