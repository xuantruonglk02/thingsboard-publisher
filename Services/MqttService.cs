using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using ThingsBoardPublisher.Models;
using ThingsBoardPublisher.Settings;
using Newtonsoft.Json;
using Serilog;
using System.Net;
using System.Text;

namespace ThingsBoardPublisher.Services;

public class MqttService
{
    private readonly PushSetting _pushSetting;

    public MqttService(IOptions<PushSetting> pushSetting)
    {
        _pushSetting = pushSetting.Value;
    }

    public async Task<bool> PushToMqtt(Message message)
    {
        bool result = false;
        try
        {
            var token = _pushSetting.Devices.Where(x => message.index.Slugify().Contains(x.FileName.Slugify())).FirstOrDefault();
            if (token != null)
            {
                var mqttFactory = new MqttFactory();
                var _mqttClient = new MqttFactory().CreateMqttClient();
                var mqttClientOptions = new MqttClientOptionsBuilder()
                            .WithTcpServer(_pushSetting.Mqtt.Host, _pushSetting.Mqtt.Port)
                            .WithCredentials(token.AccessToken)
                            .WithClientId(Guid.NewGuid().ToString())
                            .WithCleanSession()
                            .WithTimeout(TimeSpan.FromSeconds(2))
                            .Build();
                await _mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
                if (_mqttClient.IsConnected)
                {
                    var objPush = message.data.ToTimestampObject();
                    var data = new MessageTB
                    {
                        ts = long.Parse(objPush["ts"].ToString()),
                        values = objPush
                    };
                    var _message = new MqttApplicationMessageBuilder()
                        .WithTopic("v1/devices/me/telemetry")
                        .WithPayload(JsonConvert.SerializeObject(data))
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();
                    await _mqttClient.PublishAsync(_message);
                    result = true;
                    await _mqttClient.DisconnectAsync();
                    _mqttClient.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Push data with MQTT client error : Job auto resend {ex.FlattenException()}");
        }
        return result;
    }

    public async Task<bool> PushWithHttp(Message message)
    {
        bool result = false;
        try
        {
            var token = _pushSetting.Devices.Where(x => message.index.Slugify().Contains(x.FileName.Slugify())).FirstOrDefault();
            if (token != null)
            {
                var url = $"{_pushSetting.HttpUrl}/api/v1/{token.AccessToken}/telemetry";
                var objPush = message.data.ToTimestampObject();
                var data = new MessageTB
                {
                    ts = long.Parse(objPush["ts"].ToString()),
                    values = objPush
                };
                using (var httpClient = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(url, content);
                    result = response.StatusCode == HttpStatusCode.OK;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Push data with Http : Job auto resend {ex.FlattenException()}");
        }
        return result;
    }
}
