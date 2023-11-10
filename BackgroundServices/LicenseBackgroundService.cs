using Microsoft.Extensions.Options;
using ThingsBoardPublisher.Settings;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace ThingsBoardPublisher.BackgroundServices;

public class LicenseBackgroundService : BackgroundService
{
    private readonly TenantSetting _tenantSetting;

    public LicenseBackgroundService(IOptionsSnapshot<TenantSetting> tenantSetting)
    {
        _tenantSetting = tenantSetting.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            bool conditionMet = await CheckCondition();
            if (!conditionMet)
            {
                Environment.Exit(0);
            }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task<bool> CheckCondition()
    {
        var licenseValid = false;
        var url = "https://license.viphap.com/license/validate-client";
        var data = new
        {
            Tenant = _tenantSetting.Name,
            Value = _tenantSetting.License
        };
        using (var httpClient = new HttpClient())
        {
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(url, content);
            licenseValid = response.StatusCode == HttpStatusCode.OK;
        }

        return licenseValid;
    }
}

