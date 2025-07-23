# UKHO.ADDS.Clients

## Purpose

This repository stores code generated to serve as NuGet packages supporting the UKHO ADDS services.

These packages are designed as client libraries to facilitate easier consumption of our APIs.

## Contents

- **`/src`** - Contains the source code for the NuGet packages.
- **`/tests`** - Unit tests and integration tests for the packages.

## Getting Started

To get started with using or contributing to these packages, follow these steps:

1. **Clone the repository:**
   ```sh
   git clone <repository-url>

2. Navigate to the package you're interested in (located in /src) and follow the instructions in the README.md file.

## Kiota Clients
Kiota is a http client generation tool developed by Microsoft which we have used to generate clients using the OpenApi documents found on the UKHO github. For more infomation please refer to the documents here: [Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview).


1. **Consuming the client**
The Kiota Client can be easily consumed by registering the client using the helper methods found in the KiotaMiddlewareExtensions.cs

# Kiota Middleware & Client Registration Helpers

This library provides extension methods to simplify the registration and configuration of Kiota-generated API clients in ASP.NET Core applications using dependency injection. It ensures that Kiota middleware handlers, authentication providers, and HTTP clients are set up in a consistent and maintainable way.

## Features

- **Automatic registration of Kiota middleware handlers**
- **Centralized configuration of HTTP clients for Kiota clients**
- **Flexible authentication provider injection**
- **Simple client factory for creating Kiota clients with correct adapters**

## Usage

### 1. Register Kiota Defaults

Register the default Kiota handlers, client factory, and your authentication provider:

   ```sh
   services.AddKiotaDefaults(new T()); where T implemented IAuthenticationProvider
   ```

There are some standard AuthenticationProviders that can be used out of the box:

- When no authentication is required (interacting with mock services): AnonymousAuthenticationProvider
    
    ```sh
    services.AddKiotaDefaults(new AnonymousAuthenticationProvider());
    ```

- For Authentication with Azure:  AzureIdentityAuthenticationProvider
 
    ```sh
    services.AddKiotaDefaults(new AzureIdentityAuthenticationProvider(new DefaultAzureCredential()));
    ```

### 2. Register Kiota Client

Register the kiota client which will work with the previous defaults that have been registered:

   ```sh
   services.RegisterKiotaClient<MyKiotaClient>( "MyApi:Endpoint", new Dictionary<string, string> { { "Custom-Header", "Value" } } );
   ```

- `"MyApi:Endpoint"` is the configuration key for the API base URL.
- Optional headers can be provided as a dictionary.

### 3. Configuration Example

Add your endpoint to `appsettings.json`: { "MyApi:Endpoint": "https://api.example.com/" }


### 4. Dependency Injection

After registration, inject your Kiota client anywhere in your application:

```sh
public class MyService { private readonly MyKiotaClient _client;
public MyService(MyKiotaClient client)
{
    _client = client;
}

// Use _client to call your API
}
```

## Extension Methods Overview

- `AddKiotaDefaults<T>(IServiceCollection, T authProvider)`
  - Registers Kiota handlers, client factory, and authentication provider.

- `RegisterKiotaClient<TClient>(IServiceCollection, string endpointConfigKey, IDictionary<string, string>? headers = null)`
  - Registers a Kiota client and configures its HTTP client.

- Internal helpers:
  - `AddKiotaHandlers(IServiceCollection)`
  - `AddConfiguredHttpClient<TClient>(IServiceCollection, string endpointConfigKey, IDictionary<string, string>? headers = null)`

## Requirements

- .NET 8
- C# 12
- [Kiota](https://github.com/microsoft/kiota) generated clients
- ASP.NET Core Dependency Injection

## Notes

- Middleware handlers are discovered via `KiotaClientFactory.GetDefaultHandlerActivatableTypes()`.
- The client factory uses constructor injection for `IRequestAdapter`.

---

For further customization, extend or override the provided helpers as needed.

## Feedback
Your feedback is crucial to improving these packages. If you encounter issues or have suggestions, please open an issue on GitHub or contact the maintainers.

## License
This project is licensed under the MIT License. See the LICENSE file for details.