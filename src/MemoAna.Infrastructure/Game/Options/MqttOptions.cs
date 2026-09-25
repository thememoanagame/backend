namespace MemoAna.Infrastructure.Game.Options;

public sealed class MqttOptions
{
    public const string SectionName = "Mqtt";

    public string Endpoint { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
}
