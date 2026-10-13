using System.ServiceModel;
using TransactionReference;
using ServiceReference1;

namespace Ortho_xact_api.Services;

public interface IProcedureSysproGateway
{
    Task<string> Login();
    Task Logout(string session);
    Task<string> Build(string session, string businessObject, string xml);
    Task<string> Post(string session, string businessObject, string parameters, string xml);
}

public class ProcedureSysproGateway(IConfiguration config, ILogger<ProcedureSysproGateway> logger) : IProcedureSysproGateway
{
    private BasicHttpBinding Binding() => new(new Uri(config["SysproPosting:ServiceUrl"]!).Scheme == "https" ? BasicHttpSecurityMode.Transport : BasicHttpSecurityMode.None) {
        MaxReceivedMessageSize = 10 * 1024 * 1024, SendTimeout = TimeSpan.FromMinutes(2)
    };
    private EndpointAddress Endpoint(string service) =>
        new(config["SysproPosting:ServiceUrl"]!.TrimEnd('/') + "/" + service + ".asmx");
    private async Task<T> Invoke<T>(ICommunicationObject client, Func<Task<T>> call)
    {
        T result;
        try { result = await call(); }
        catch { client.Abort(); throw; }
        // A completed operation must retain its response even if channel cleanup fails.
        try { client.Close(); }
        catch (Exception ex)
        {
            client.Abort();
            logger.LogWarning(ex, "SYSPRO channel cleanup failed after receiving a response");
        }
        return result;
    }
    public async Task<string> Login()
    {
        var client = new utilitiesclassSoapClient(Binding(), Endpoint("utilities"));
        var result = await Invoke(client, () => client.LogonAsync(config["SysproPosting:Operator"],
            config["SysproPosting:Password"] ?? "", config["SysproPosting:Company"],
            config["SysproPosting:CompanyPassword"] ?? "", Language.AUTO, LogDetail.ldNoDebug, Instance.EncoreInstance_0, ""));
        return SysproSession.Parse(result?.Body?.LogonResult);
    }
    public async Task Logout(string session)
    {
        var client = new utilitiesclassSoapClient(Binding(), Endpoint("utilities"));
        await Invoke(client, () => client.LogoffAsync(session));
    }
    public async Task<string> Build(string session, string businessObject, string xml)
    {
        var client = new transactionclassSoapClient(Binding(), Endpoint("Transaction"));
        return (await Invoke(client, () => client.BuildAsync(session, businessObject, xml))).Body.BuildResult;
    }
    public async Task<string> Post(string session, string businessObject, string parameters, string xml)
    {
        var client = new transactionclassSoapClient(Binding(), Endpoint("Transaction"));
        return (await Invoke(client, () => client.PostAsync(session, businessObject, parameters, xml))).Body.PostResult;
    }
}
