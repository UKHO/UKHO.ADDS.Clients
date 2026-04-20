1. Install kiota tool globally using the following command:
   ```sh
   dotnet tool install --global Microsoft.OpenApi.Kiota
   ```
2. Generate the client using the following command for SCS:
   ```sh
    kiota generate --openapi "<path to>\deployment\salesCatalogue_ExchangeSetService_API_definition_v1.10.yaml" --output "<path to>\src\UKHO.ADDS.Clients.SalesCatalogueService\GeneratedClient" --language csharp --class-name KiotaSalesCatalogueService --namespace-name UKHO.ADDS.Clients.Kiota.SalesCatalogueService
    ```
