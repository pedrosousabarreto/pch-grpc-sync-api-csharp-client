using System.Runtime.InteropServices.JavaScript;
using Google.Protobuf.WellKnownTypes;
using mojaloop_grpc_sync_api_client;

const string TOKEN = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6InNud0F0QVR6MlJmRmhLZUVlR0NTV0pjTTBoV25QQ3U2OEtRZ25obVNkREkifQ.eyJ0eXAiOiJCZWFyZXIiLCJhenAiOiJzZWN1cml0eS1iYy11aSIsInVzZXJUeXBlIjoiSFVCIiwicGxhdGZvcm1Sb2xlcyI6WyJhZG1pbiJdLCJwYXJ0aWNpcGFudFJvbGVzIjpbXSwiaWF0IjoxNzI1MzE4NTkxLCJleHAiOjE3NDA4NzA1OTEsImF1ZCI6Im1vamFsb29wLnZuZXh0LmRldi5kZWZhdWx0X2F1ZGllbmNlIiwiaXNzIjoibW9qYWxvb3Audm5leHQuZGV2LmRlZmF1bHRfaXNzdWVyIiwic3ViIjoidXNlcjo6YWRtaW4iLCJqdGkiOiI5OGFmNTlhMC1iZjc1LTQwOWYtOWU3Mi1kNjhmOWVhNTBmZDAifQ.cJhL0C2zLs8GRgjCASVEqfWHFrr2qoHAAbUuSSUaS6cQGwpwlIRMa1PzvQvGyg-XozIbXu962hzLj4ojvte6TS7E-mx4CFm5egR3SVAv-CVuklWgVUpbYbWmpuAWM0yFp2ChVr_hDNpZpV8KWTQyc4XYBemnixK_WI-sKx4y7ez8pGbZSgbo3ZUDx4blTju-6LhQmA7N9GIYzjwnPBWqOam5pS24pGqkt9LoTtrD3NbaGLX1o2AsbXV_IgAikqOKd3jRojoHr8cPWjxwFlN4sWCZ_5XJmrEvOLW4ncgvZyflxwxV7O8f103iyjUviIL97bbFrTPhwKf2cCxwnbEu3Q";
const string FSP_ID = "csharpbank";
const string URL = "grpc-api.vnext.dev.pch:443";

// SERVER TLS example, use the same certificate of the server and no private key or cert chain
GrpcSecurityOptions certsOptions = new GrpcSecurityOptions
{
    Mode = GrpcSecurityMode.SERVER_TLS,
    CaCertFilePath = Path.Combine(Directory.GetCurrentDirectory(),"grpc-api.vnext.dev.pch.pem"),
    PrivateKeyFilePath = null,
    CertChainFilePath = null
};

var client = new GrpcSyncApiClient(URL, FSP_ID, TOKEN, certsOptions);

await client.Init();

var lookupParticipantReq = new LookupParticipantRequest
{
    PartyId = "2512",
    PartyIdType = "MSISDN" 
};
DateTime startDt =  DateTime.Now;
var participantLookupResponse = await client.ParticipantLookup(lookupParticipantReq);
var ms = DateTime.Now.Subtract(startDt).TotalMilliseconds;

Console.WriteLine($"LookupParticipantRequest for PartyIdType: '{lookupParticipantReq.PartyIdType}' PartyId: {lookupParticipantReq.PartyId}, response fspId is: {participantLookupResponse.FspId} - took: {ms}");


client.setPartyLookupHandler((req) =>
{

    var successResp = new LookupPartySuccessResponse
    {
        PartyId = req.PartyId,
        PartyIdType = req.PartyIdType,
        FirstName = "Pedro C#",
        LastName = DateTime.Now.ToLongTimeString()
    };

    var respEnvelope = new LookupPartyResponse();
    respEnvelope.Response = successResp;
    
    respEnvelope.RequestId = req.PendingRequestId;
    
    // switch the fspIds for the response (we are now the source and the request source is the destination)
    respEnvelope.SourceFspId = req.DestinationFspId;
    respEnvelope.DestinationFspId = req.SourceFspId;
    
    return Task.FromResult(respEnvelope);
});

client.setAcceptTransferHandler(req =>
{
    var successResp = new ServerAcceptTransferResponse();
    successResp.TransferId = req.TransferId;
    successResp.RequestId = req.RequestId;

    var from = req.From;
    if (null == from) throw new Exception("Invalid from in ServerAcceptTransferRequest");

    successResp.DestinationFspId = from.FspId;
    successResp.HomeTransactionId = req.HomeTransactionId;

    var to = req.From;
    if (null == to) throw new Exception("Invalid to in ServerAcceptTransferRequest");

    successResp.SourceFspId = to.FspId;

    return Task.FromResult(successResp);
});

await Task.Run(async () =>  // <- marked async
{
    while (true)
    {
        Thread.Sleep(1);
    }
});
