namespace ThingsBoardPublisher.Settings;

public class EmailSetting
{
    /// <example>abc@gmail.com</example>
    public string FromEmail { get; set; }

    /// <example>smtp.gmail.com</example>
    public string Url { get; set; }

    /// <example>587</example>
    public int Port { get; set; }

    /// <example>abc@gmail.com</example>
    public string UserName { get; set; }

    /// <example>xxxxxxxx</example>
    public string Password { get; set; }

    /// <example>abc@gmail.com</example>
    public string ToEmail { get; set; }
}
