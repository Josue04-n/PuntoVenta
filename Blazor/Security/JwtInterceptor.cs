using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace Blazor.Security;

public class JwtInterceptor : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "authToken";

    public JwtInterceptor(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.RequestUri!.IsAbsoluteUri || request.RequestUri.Host == "localhost")
        {
            try 
            {
                var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch 
            {
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
