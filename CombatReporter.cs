using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class CombatReporter
{
    private static UdpClient udpClient = new UdpClient();
    private static string wpfIP = "127.0.0.1";
    private static int wpfPort = 8080;

    public static void SendKillReport(string targetName)
    {
        string json = $"{{\"Type\":\"CombatReport\",\"Event\":\"Kill\",\"Target\":\"{targetName}\"}}";
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        try
        {
            udpClient.Send(bytes, bytes.Length, wpfIP, wpfPort);
        }
        catch { }
    }
}