using MassTransit;
using Microsoft.Extensions.Options;
using ThingsBoardPublisher.Models;
using ThingsBoardPublisher.Services;
using ThingsBoardPublisher.Settings;
using Newtonsoft.Json;

namespace ThingsBoardPublisher.Consumers;

public class MessageConsumer : IConsumer<MessageObject>
{
    private readonly MqttService _mqttService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly PushSetting _pushSetting;
    private const string PushType = "MQTT";

    public MessageConsumer(MqttService mqttService, IHostEnvironment hostEnvironment, IOptionsSnapshot<PushSetting> pushSetting)
    {
        _mqttService = mqttService;
        _hostEnvironment = hostEnvironment;
        _pushSetting = pushSetting.Value;
    }

    public async Task Consume(ConsumeContext<MessageObject> context)
    {
        // Push data
        var ms = context.Message.Text;
        var message = ms.FromBase64String<Message>();
        var result = PushType == _pushSetting.PushType ? await _mqttService.PushToMqtt(message) : await _mqttService.PushWithHttp(message);
        if (!result)
        {
            // Luu file de push lai
            // Log.Logger.Information("Push lai message sau");
            var dataTopic = new DataTopic
            {
                index = message.index,
                data = new List<object> { message.data }
            };
            SaveDataToFile(dataTopic, Guid.NewGuid().ToString());
        }
        /*else
        {
            Log.Logger.Information("Push mqtt success");
        }*/
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
