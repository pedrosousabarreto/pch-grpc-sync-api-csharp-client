# vNext C# gRPC Sync API Client 

To use this client, create a GrpcSyncApiClient instance in your code.

## Checkout the Test.cs file for an implementation example

### Confirm/Change top level constants:

- TOKEN - auth token, see example below
- FSP_ID - id of the FSP (ex: csharpbank) 
- URL - host and port of the gRPC endpoint in HOST:PORT format - dev is: grpc-api.vnext.dev.pch:443

Make sure the certsOptions object is correct. For dev env the `Mode` should be `GrpcSecurityMode.SERVER_TLS` and the CaCertFilePath should be the absolute path for the server certificate file "_grpc-api.vnext.dev.pch.pem_" in this directory. 

Example dev token that will last until 1st of March 2025: 
```
eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6InNud0F0QVR6MlJmRmhLZUVlR0NTV0pjTTBoV25QQ3U2OEtRZ25obVNkREkifQ.eyJ0eXAiOiJCZWFyZXIiLCJhenAiOiJzZWN1cml0eS1iYy11aSIsInVzZXJUeXBlIjoiSFVCIiwicGxhdGZvcm1Sb2xlcyI6WyJhZG1pbiJdLCJwYXJ0aWNpcGFudFJvbGVzIjpbXSwiaWF0IjoxNzI1MzE4NTkxLCJleHAiOjE3NDA4NzA1OTEsImF1ZCI6Im1vamFsb29wLnZuZXh0LmRldi5kZWZhdWx0X2F1ZGllbmNlIiwiaXNzIjoibW9qYWxvb3Audm5leHQuZGV2LmRlZmF1bHRfaXNzdWVyIiwic3ViIjoidXNlcjo6YWRtaW4iLCJqdGkiOiI5OGFmNTlhMC1iZjc1LTQwOWYtOWU3Mi1kNjhmOWVhNTBmZDAifQ.cJhL0C2zLs8GRgjCASVEqfWHFrr2qoHAAbUuSSUaS6cQGwpwlIRMa1PzvQvGyg-XozIbXu962hzLj4ojvte6TS7E-mx4CFm5egR3SVAv-CVuklWgVUpbYbWmpuAWM0yFp2ChVr_hDNpZpV8KWTQyc4XYBemnixK_WI-sKx4y7ez8pGbZSgbo3ZUDx4blTju-6LhQmA7N9GIYzjwnPBWqOam5pS24pGqkt9LoTtrD3NbaGLX1o2AsbXV_IgAikqOKd3jRojoHr8cPWjxwFlN4sWCZ_5XJmrEvOLW4ncgvZyflxwxV7O8f103iyjUviIL97bbFrTPhwKf2cCxwnbEu3Q
```

NOTE: this token is a temporary auth mechanism, in the future, mTLS will be used to authenticate the clients.

### Build and run test client

To build and run the test client execute the following command in the same directory of the csproj file:

```console
dotnet run
```

### Hooking the PartyLookup and AcceptTransfer handler functions

See the test.cs example, for how to hook the handlers:

```typescript

// Hook your PartyLookupHandler function on the client     
// This code will be executed when the server sends an PartyInfoRequest to the client
client.setPartyLookupHandler((req) =>
{
    // must return valid LookupPartyResponse object
});

// Hook your AcceptTransferHandler function on the client     
// This code will be executed when the server sends an AcceptTransferRequest to the client
client.setAcceptTransferHandler(req =>
{
    // must return valid ServerAcceptTransferResponse object
});

```