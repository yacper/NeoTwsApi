using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NeoTwsApi.Tests
{
    public class Tests
    {
        IbClient client = new IbClient(TestConstants.Host, TestConstants.Port, TestConstants.ClientId);

        [SetUp]
        public async Task  Setup()
        {
            // Setup
            bool connected = await client.ConnectedAsync();

            Debug.WriteLine(client.Dump());
        }

        //[Test]
        //public async Task ConnectAsync()
        //{
        //    // Setup
       // IbClient client = new IbClient(TestConstants.Host, TestConstants.Port, TestConstants.ClientId);
        //    bool connected = await client.ConnectedAsync();

        //    Debug.WriteLine(client.Dump());


        //    //await client.DisconnectAsync();

        //    //Debug.WriteLine(client.Dump());
        //}

    }
}