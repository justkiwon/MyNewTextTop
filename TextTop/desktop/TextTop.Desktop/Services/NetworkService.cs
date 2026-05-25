namespace TextTop.Desktop.Services;

public static class NetworkService
{
    public static bool LooksOnline() => System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
}
