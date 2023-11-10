namespace ThingsBoardPublisher.Models;

public class Message
{
    public string index { get; set; }
    public object data { get; set; }
}

public class MessageObject
{
    public string Text { get; set; }
}

public class MessageTB
{
    public long ts { get; set; }
    public object values { get; set; }
}