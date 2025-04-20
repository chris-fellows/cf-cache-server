using CFConnectionMessaging.Models;
using System.Net;

namespace CFCacheServer.UI
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // TODO: Read from config
            var remoteEndpointInfo = new EndpointInfo() { Ip = "192.168.1.45", Port = 10200 };
            var securityKey = "ABCDE";

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(remoteEndpointInfo, securityKey));
        }
    }
}