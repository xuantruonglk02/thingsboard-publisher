namespace ThingsBoardPublisher.Settings;

public class PushSetting
{
    public string PushType { get; set; }
    public List<DeviceClient> Devices { get; set; }
    public string HttpUrl { get; set; }
    public MqttInfo Mqtt { get; set; }
    public int Zone { get; set; }
}
