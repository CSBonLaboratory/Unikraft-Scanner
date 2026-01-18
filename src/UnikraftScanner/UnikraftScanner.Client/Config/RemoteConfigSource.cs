namespace UnikraftScanner.Client.Config;

using Microsoft.Extensions.Configuration;


public class RemoteConfigSource : IConfigurationSource
{
    public string KeyPath {get; init;}

    public string IVPath {get; init;}

    public RemoteConfigSource(string keyPath, string ivPath)
    {
        KeyPath = keyPath;
        IVPath = ivPath;
    }
    public IConfigurationProvider Build(IConfigurationBuilder _)
    {
        return new RemoteConfigProvider(KeyPath, IVPath);
    }
}