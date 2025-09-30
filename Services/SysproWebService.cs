namespace Ortho_xact_api.Services
{
    using ServiceReference1;
    using TransactionReference;

    using System.ServiceModel;
    using System.Text;
    using System.IO;
    using Ortho_xact_api.SysModels;

    public class SysproWebService
    {
        public async Task<LogonResponse> LoginAsync(string Username,string Password, string CompanyId)
        {
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                MaxReceivedMessageSize = 10485760
            };

            var endpoint = new EndpointAddress("http://192.168.16.70/SYSPRO8WebServices/utilities.asmx");
            //var endpoint = new EndpointAddress("http://192.168.23.157/SYSPRO8WebServices/utilities.asmx");

            var client = new utilitiesclassSoapClient(binding, endpoint);

            

            string xmlIn = "<Logon><Parameters><Device>Web</Device></Parameters></Logon>";

            // Make the call
            var resultStream = await client.LogonAsync(Username, Password, CompanyId, "", Language.AUTO, LogDetail.ldNoDebug, Instance.EncoreInstance_0, "");

             
            

            return resultStream;
        }
        public async Task<bool> LogoutAsync(string sessionToken)
        {
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                MaxReceivedMessageSize = 10485760
            };
            //var endpoint = new EndpointAddress("http://192.168.23.157/SYSPRO8WebServices/utilities.asmx");
            var endpoint = new EndpointAddress("http://192.168.16.70/SYSPRO8WebServices/utilities.asmx");

            var client = new utilitiesclassSoapClient(binding, endpoint);

            try
            {
                await client.LogoffAsync(sessionToken);
                return true;
            }
            catch (Exception ex)
            {
                // Handle error (e.g., log it)
                Console.WriteLine($"Logout failed: {ex.Message}");
                return false;
            }
        }

        public async Task<PostResponse> Transaction(string sessionId, string businessObject, string parameters, string xmlIn)
        {
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                MaxReceivedMessageSize = 10485760
            };
           // var endpoint = new EndpointAddress("http://192.168.23.157/SYSPRO8WebServices/Transaction.asmx");

            var endpoint = new EndpointAddress("http://192.168.16.70/SYSPRO8WebServices/Transaction.asmx");

            var client = new transactionclassSoapClient(binding, endpoint);



            
            // Make the call
            var resultStream = await client.PostAsync(sessionId, businessObject, parameters,xmlIn);




            return resultStream;
        }

    }

}
