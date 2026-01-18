namespace UnikraftScanner.Client.Config;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Threading.Tasks;


// if values in the remote config are changed, the CLI client should not be update
// however if the structure is changed, the developer must introduce a new release so that the CLI client will check for updates
// before starting the program
public class RemoteConfig
{
    public string UnikraftCoverageDbConnectionString {get; set;}
}
public class RemoteConfigProvider : ConfigurationProvider
{
    public string KeyPath {get; init;}

    public string IVPath {get; init;}

    private string infraConfigUri = "https://raw.githubusercontent.com/CSBonLaboratory/Unikraft-Scanner/refs/heads/old-scanner-python/src/coverage.py";

    public RemoteConfigProvider(string keyPath, string ivPath)
    {
        KeyPath = keyPath;
        IVPath  = ivPath;
    }

    public override void Load()
    {
        byte[] encryptedInfraConfig;

        try
        {
            using (HttpClient infraConfigFetcher = new HttpClient())
            {
                Task<HttpResponseMessage> response = infraConfigFetcher.GetAsync(infraConfigUri);

                response.Wait();

                Task<byte[]> body = response.Result.Content.ReadAsByteArrayAsync();

                body.Wait();

                encryptedInfraConfig = Convert.FromBase64String(System.Text.Encoding.ASCII.GetString(body.Result));
            }
        

            byte[] key = File.ReadAllBytes(KeyPath);

            byte[] iv = File.ReadAllBytes(IVPath);

            string remoteConfigRaw;

            using (Aes aes = Aes.Create())
            {
                using(CryptoStream cryptoStream = new (
                    stream: new MemoryStream(encryptedInfraConfig),
                    transform: aes.CreateDecryptor(key, iv),
                    mode: CryptoStreamMode.Read))
                {
                    using(StreamReader decryptReader = new(cryptoStream))
                    {
                        remoteConfigRaw = decryptReader.ReadToEnd();
                    }
                }
            }

            base.Data["RemoteConfig"] = remoteConfigRaw;

        }
        catch(Exception _)
        {
            
        }



    }

}