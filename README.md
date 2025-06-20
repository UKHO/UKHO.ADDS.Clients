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
Kiota is a http client generation tool developed by Microsoft which we have used to generate clients using the openApi documents found on the UKHO github. For more infomation please refer to the documents here: [Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview).


1. **Consuming the client**
When consuming a kiota client a client request adpater is required. HttpClientRequestAdapter can be found in the library Microsoft.Kiota.Http.HttpClientLibrary.

Set the baseUrl:
  
   ```sh
   adapter.BaseUrl = {baseURL}
   ```

To inspect headers in the client, pass in 
   
   ```sh
   HeadersInspectionHandlerOption headers = new HeadersInspectionHandlerOption() { InspectResponseHeaders = true, InspectRequestHeaders = true };
   ```

consume the header handler option via

   ```sh
    response = await {path}.PostAsync(requestConfiguration => requestConfiguration.Options.Add(headers));
   ```
Inspect the headers by inspecting the HeadersInspectionHandlerOption object.

2. **Authentication**
   An AuthenticationProvider must be provided to the adapter. This can be achieved by implementing the interface IAuthenticationProvider found in the library Microsoft.Kiota.Abstractions.Authentication.
   An example implementation can be found below:

   ```sh
        public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Request information cannot be null.");
            }
            return GetManagedIdentityAuthAsync(resourceId).ContinueWith(task =>
            {
                request.Headers.Add("Authorization", "Bearer " + task.Result);
            }, cancellationToken);
        }
   ```

## Feedback
Your feedback is crucial to improving these packages. If you encounter issues or have suggestions, please open an issue on GitHub or contact the maintainers.

## License
This project is licensed under the MIT License. See the LICENSE file for details.