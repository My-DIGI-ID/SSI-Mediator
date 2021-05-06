using Hyperledger.Aries.Routing;
using System.Collections.Generic;

namespace SSIMediator.Agent
{

    ///<inheritdoc/>
    public class CustomDeviceInfoRecord : DeviceInfoRecord
    {
        public Dictionary<string, string> Metadata { get; set; }
    }
}
