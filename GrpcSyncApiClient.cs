using Grpc.Core;

namespace mojaloop_grpc_sync_api_client;

public delegate Task<LookupPartyResponse> PartyLookupHandlerDelegate(ServerPartyInfoRequest req);
public delegate Task<ServerAcceptTransferResponse> AcceptTransferHandlerDelegate(ServerAcceptTransferRequest req);

public enum GrpcSecurityMode
{
    INSECURE,
    SERVER_TLS,
    MUTUAL_TLS
}

public class GrpcSecurityOptions
{
    public GrpcSecurityMode Mode;
    public string? CaCertFilePath;
    public string? PrivateKeyFilePath;
    public string? CertChainFilePath;
}

public class GrpcSyncApiClient
{
    private static string GRPC_METADATA_TOKEN_FIELD_KEY = "accessToken";
    private static string GRPC_METADATA_FSPID_FIELD_KEY = "fspId";
    private readonly string _serviceUrl;
    private readonly string _fspId;
    private readonly string _accessToken;
    private Metadata _meta;
    private readonly InteropGrpcApi.InteropGrpcApiClient _client;
    private AsyncDuplexStreamingCall<StreamFromClientMsg, StreamToClientMsg> _stream;

    private PartyLookupHandlerDelegate? _partyLookupHandler;
    private AcceptTransferHandlerDelegate? _acceptTransferHandler;

    public GrpcSyncApiClient(string serviceUrl, string fspId, string accessToken, GrpcSecurityOptions? securityOptions)
    {
        this._serviceUrl = serviceUrl;
        this._fspId = fspId;
        this._accessToken = accessToken;

        // default
        Grpc.Core.ChannelCredentials creds = ChannelCredentials.Insecure;

        if (null != securityOptions || securityOptions.Mode != GrpcSecurityMode.INSECURE)
        {
            var rootCert = File.ReadAllText(securityOptions.CaCertFilePath);

            if (securityOptions.Mode == GrpcSecurityMode.SERVER_TLS)
            {
                creds = new SslCredentials(rootCert);
            }
            else
            {
                var certChain = File.ReadAllText(securityOptions.CertChainFilePath);

                var clientKey = File.ReadAllText(securityOptions.PrivateKeyFilePath);
                var keyPair = new KeyCertificatePair(certChain, clientKey);

                creds = new SslCredentials(rootCert, keyPair);
            }
        }
        
        // keep  alive options
        var channelOptions = new List<ChannelOption>();
        channelOptions.Add(new ChannelOption("grpc.keepalive_time_ms", 10000));
        channelOptions.Add(new ChannelOption("grpc.keepalive_timeout_ms", 5000));
        channelOptions.Add(new ChannelOption("grpc.keepalive_permit_without_calls", 1));
        

        var channel = new Channel(this._serviceUrl, creds, channelOptions);
        this._client = new InteropGrpcApi.InteropGrpcApiClient(channel);

        Console.WriteLine($"GrpcSyncApiClient created with serviceUrl: {this._serviceUrl} and fspId: {this._fspId}");
    }

    public async Task Init()
    {
        this._meta = new Metadata();
        this._meta.Add(GRPC_METADATA_TOKEN_FIELD_KEY, this._accessToken);
        this._meta.Add(GRPC_METADATA_FSPID_FIELD_KEY, this._fspId);

        this._stream = this._client.StartStream(this._meta);
        var responseTask = Task.Run(async () =>
        {
            while (await this._stream.ResponseStream.MoveNext())
            {
                var serverMessage = this._stream.ResponseStream.Current;

                if (serverMessage.ResponseTypeCase == StreamToClientMsg.ResponseTypeOneofCase.PartyInfoRequest)
                {
                    await this._handleServerPartyInfoRequest(serverMessage.PartyInfoRequest);
                }
                else if (serverMessage.ResponseTypeCase ==
                         StreamToClientMsg.ResponseTypeOneofCase.AcceptTransferRequest)
                {
                    await this._handleServerAcceptTransferRequest(serverMessage.AcceptTransferRequest);
                }
                else if (serverMessage.ResponseTypeCase ==
                         StreamToClientMsg.ResponseTypeOneofCase.AcceptTransferRequest)
                {
                    // ignore
                }
                else
                {
                    Console.WriteLine($"ERROR - Unhandled Server Message Type: {serverMessage.ResponseTypeCase}");
                }
            }
        });

        // send the initial client request
        var initialReq = new StreamClientInitialRequest();
        initialReq.ClientName = "C# Client";
        initialReq.FspId = this._fspId;
        var clientMsg = new StreamFromClientMsg();
        clientMsg.InitialRequest = initialReq;
        await this._stream.RequestStream.WriteAsync(clientMsg);

        return;
    }


    private async Task _handleServerPartyInfoRequest(ServerPartyInfoRequest serverReq)
    {
        Console.WriteLine("Got a ServerPartyInfoRequest from the server...");
        if (null != this._partyLookupHandler)
        {
            var handlerResponse = await this._partyLookupHandler(serverReq);
            await this._stream.RequestStream.WriteAsync(new StreamFromClientMsg
            {
                LookupPartyInfoResponse = handlerResponse
            });
            Console.WriteLine("ServerPartyInfoRequest handled!");
        }
        else
        {
            Console.WriteLine("ERROR - no partyLookupHandler defined");
        }
    }

    private async Task _handleServerAcceptTransferRequest(ServerAcceptTransferRequest serverReq)
    {
        Console.WriteLine("Got a ServerAcceptTransferRequest from the server...");
        if (null != this._acceptTransferHandler)
        {
            var handlerResponse = await this._acceptTransferHandler(serverReq);
            await this._stream.RequestStream.WriteAsync(new StreamFromClientMsg
            {
                AcceptTransferResponse = handlerResponse
            });
            Console.WriteLine("ServerAcceptTransferRequest handled!");
        }
        else
        {
            Console.WriteLine("ERROR - no partyLookupHandler defined");
        }
    }

    public void setPartyLookupHandler(PartyLookupHandlerDelegate fn)
    {
        this._partyLookupHandler = fn;
    }

    public void setAcceptTransferHandler(AcceptTransferHandlerDelegate fn)
    {
        this._acceptTransferHandler = fn;
    }

    public async Task<LookupParticipantResponse> ParticipantLookup(LookupParticipantRequest req)
    {
        var resp = await this._client.LookupParticipantAsync(req, this._meta);
        return resp;
    }

    public async Task<LookupPartyResponse> PartyLookup(LookupPartyRequest req)
    {
        var resp = await this._client.LookupPartyAsync(req, this._meta);
        return resp;
    }

    public async Task<TransferResponse> ExecuteTransfer(TransferRequest req)
    {
        var resp = await this._client.ExecuteTransferAsync(req, this._meta);
        return resp;
    }
}