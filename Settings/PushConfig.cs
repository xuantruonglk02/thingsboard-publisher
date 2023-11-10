namespace ThingsBoardPublisher.Settings;

public class MqttInfo
{
    public string Host { get; set; }
    public int Port { get; set; }
}

public class DeviceClient
{
    public string FileName { get; set; }
    public string AccessToken { get; set; }
}
