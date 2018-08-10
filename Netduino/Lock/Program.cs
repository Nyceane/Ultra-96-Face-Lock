using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace Lock
{
    public class Program
    {
        // Main method is called once as opposed to a loop method called over and over again
        public static void Main()
        {
            Thread.Sleep(5000);
            App app = new App();
            app.Run();

            OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);
            OutputPort relay = new OutputPort(Pins.GPIO_PIN_D5, false);

            int port = 80;

            Socket listenerSocket = new Socket(AddressFamily.InterNetwork,
                                   SocketType.Stream,
                                   ProtocolType.Tcp);
            IPEndPoint listenerEndPoint = new IPEndPoint(IPAddress.Any, port);

            Debug.Print("setting up socket");

            // bind to the listening socket
            listenerSocket.Bind(listenerEndPoint);
            // and start listening for incoming connections
            listenerSocket.Listen(1);
            Debug.Print("listening");


            while (true)
            {
                Debug.Print(".");

                // wait for a client to connect
                Socket clientSocket = listenerSocket.Accept();
                // wait for data to arrive
                bool dataReady = clientSocket.Poll(5000000, SelectMode.SelectRead);


                // if dataReady is true and there are bytes available to read,
                // then you have a good connection.
                if (dataReady && clientSocket.Available > 0)
                {
                    byte[] buffer = new byte[clientSocket.Available];
                    clientSocket.Receive(buffer);
                    string request =
                      new string(System.Text.Encoding.UTF8.GetChars(buffer));

                    Debug.Print(request);

                    if (request.IndexOf("ON") >= 0)
                    {
                        led.Write(true);
                        relay.Write(true);
                        Thread.Sleep(5000);
                        led.Write(false);
                        relay.Write(false);
                    }


                    string statusText = "Lock is " + (led.Read() ? "ON" : "OFF") + ".";
                    // return a message to the client letting it
                    // know if the LED is now on or off.
                    string response = statusText ;
                    clientSocket.Send(System.Text.Encoding.UTF8.GetBytes(response));

                }
                // important: close the client socket
                clientSocket.Close();

            }

        }
    }

    public class App
    {
        NetworkInterface[] _interfaces;

        public string NetDuinoIPAddress { get; set; }
        public bool IsRunning { get; set; }

        public void Run()
        {
            //this.IsRunning = true;
            bool goodToGo = InitializeNetwork();

            this.IsRunning = false;
        }


        protected bool InitializeNetwork()
        {
            if (Microsoft.SPOT.Hardware.SystemInfo.SystemID.SKU == 3)
            {
                Debug.Print("Wireless tests run only on Device");
                return false;
            }

            Debug.Print("Getting all the network interfaces.");
            _interfaces = NetworkInterface.GetAllNetworkInterfaces();

            // debug output
            ListNetworkInterfaces();

            // loop through each network interface
            foreach (var net in _interfaces)
            {
                // debug out
                ListNetworkInfo(net);

                switch (net.NetworkInterfaceType)
                {
                    case (NetworkInterfaceType.Ethernet):
                        Debug.Print("Found Ethernet Interface");
                        break;
                    case (NetworkInterfaceType.Wireless80211):
                        Debug.Print("Found 802.11 WiFi Interface");
                        break;
                    case (NetworkInterfaceType.Unknown):
                        Debug.Print("Found Unknown Interface");
                        break;
                }

                // check for an IP address, try to get one if it's empty
                return CheckIPAddress(net);
            }

            // if we got here, should be false.
            return false;
        }

        public void MakeWebRequest(string url)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Method = "GET";
            httpWebRequest.Timeout = 1000;
            httpWebRequest.KeepAlive = false;

            httpWebRequest.GetResponse();
            /*
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                Debug.Print("this is what we got from " + url + ": " + result);
            }*/
        }


        protected bool CheckIPAddress(NetworkInterface net)
        {
            int timeout = 10000; // timeout, in milliseconds to wait for an IP. 10,000 = 10 seconds

            // check to see if the IP address is empty (0.0.0.0). IPAddress.Any is 0.0.0.0.
            if (net.IPAddress == IPAddress.Any.ToString())
            {
                Debug.Print("No IP Address");

                if (net.IsDhcpEnabled)
                {
                    Debug.Print("DHCP is enabled, attempting to get an IP Address");

                    // ask for an IP address from DHCP [note this is a static, not sure which network interface it would act on]
                    int sleepInterval = 10;
                    int maxIntervalCount = timeout / sleepInterval;
                    int count = 0;
                    while (IPAddress.GetDefaultLocalAddress() == IPAddress.Any && count < maxIntervalCount)
                    {
                        Debug.Print("Sleep while obtaining an IP");
                        Thread.Sleep(10);
                        count++;
                    };

                    // if we got here, we either timed out or got an address, so let's find out.
                    if (net.IPAddress == IPAddress.Any.ToString())
                    {
                        Debug.Print("Failed to get an IP Address in the alotted time.");
                        return false;
                    }

                    Debug.Print("Got IP Address: " + net.IPAddress.ToString());
                    return true;

                    //NOTE: this does not work, even though it's on the actual network device. [shrug]
                    // try to renew the DHCP lease and get a new IP Address
                    //net.RenewDhcpLease ();
                    //while (net.IPAddress == "0.0.0.0") {
                    //    Thread.Sleep (10);
                    //}

                }
                else
                {
                    Debug.Print("DHCP is not enabled, and no IP address is configured, bailing out.");
                    return false;
                }
            }
            else
            {
                Debug.Print("Already had IP Address: " + net.IPAddress.ToString());
                return true;
            }

        }

        protected void ListNetworkInterfaces()
        {
            foreach (var net in _interfaces)
            {
                switch (net.NetworkInterfaceType)
                {
                    case (NetworkInterfaceType.Ethernet):
                        Debug.Print("Found Ethernet Interface");
                        break;
                    case (NetworkInterfaceType.Wireless80211):
                        Debug.Print("Found 802.11 WiFi Interface");
                        break;
                    case (NetworkInterfaceType.Unknown):
                        Debug.Print("Found Unknown Interface");
                        break;
                }
            }
        }

        protected void ListNetworkInfo(NetworkInterface net)
        {
            Debug.Print("MAC Address: " + BytesToHexString(net.PhysicalAddress));
            Debug.Print("DHCP enabled: " + net.IsDhcpEnabled.ToString());
            Debug.Print("Dynamic DNS enabled: " + net.IsDynamicDnsEnabled.ToString());
            Debug.Print("IP Address: " + net.IPAddress.ToString());
            Debug.Print("Subnet Mask: " + net.SubnetMask.ToString());
            Debug.Print("Gateway: " + net.GatewayAddress.ToString());

            if (net is Wireless80211)
            {
                var wifi = net as Wireless80211;
                Debug.Print("SSID:" + wifi.Ssid.ToString());
            }

            this.NetDuinoIPAddress = net.IPAddress.ToString();

        }

        private static string BytesToHexString(byte[] bytes)
        {
            string hexString = string.Empty;

            // Create a character array for hexadecimal conversion.
            const string hexChars = "0123456789ABCDEF";

            // Loop through the bytes.
            for (byte b = 0; b < bytes.Length; b++)
            {
                if (b > 0)
                    hexString += "-";

                // Grab the top 4 bits and append the hex equivalent to the return string.        
                hexString += hexChars[bytes[b] >> 4];

                // Mask off the upper 4 bits to get the rest of it.
                hexString += hexChars[bytes[b] & 0x0F];
            }

            return hexString;
        }

        public string getIPAddress()
        {
            return this.NetDuinoIPAddress;
        }
    }
}