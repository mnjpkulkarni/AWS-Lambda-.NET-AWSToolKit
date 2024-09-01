using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdaProxyIntegration;

public class Function
{

    private static string BASE_URL = "https://reqres.in";

    /// <summary>
    /// A function that takes APIGatewayProxyRequest and returns APIGatewayProxyResponse
    /// </summary>
    /// <param name="apiGatewayProxyRequest">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apiGatewayProxyRequest, ILambdaContext context)
    {
        Console.WriteLine("Proxy Request Event: " + apiGatewayProxyRequest);
        var apiResponse = await MakeHTTPRequest(apiGatewayProxyRequest);
        var response = new APIGatewayProxyResponse()
        {
            StatusCode = 200,
            IsBase64Encoded = false,
            Body = apiResponse,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" }, { "Access-Control-Allow-Credentials", "true" } }
        };
        Console.WriteLine("Proxy Response: " + response);
        return response;
    }

    private async Task<string> MakeHTTPRequest(APIGatewayProxyRequest apiGatewayProxyRequest)
    {
        var client = new HttpClient();
        var path = apiGatewayProxyRequest.Path;
        var query = "?";

        if (apiGatewayProxyRequest.MultiValueQueryStringParameters != null)
        {

            foreach (KeyValuePair<string, IList<string>> keyValuePair in apiGatewayProxyRequest.MultiValueQueryStringParameters)
            {
                for (int i = 0; i < keyValuePair.Value.Count; i++)
                {
                    query = $"{query}{keyValuePair.Key}={keyValuePair.Value[i]}&";
                }
            }
        }

        if (query.EndsWith('&'))
        {
            query = query.Remove(query.Length - 1);
        }


        var finalURL = Function.BASE_URL + path + query;

        try
        {
            HttpMethod httpMethod = HttpMethod.Get;
            if (apiGatewayProxyRequest.HttpMethod.ToLower().Equals("post"))
            {
                httpMethod = HttpMethod.Post;
            }
            else if (apiGatewayProxyRequest.HttpMethod.ToLower().Equals("put"))
            {
                httpMethod = HttpMethod.Put;
            }
            var request = new HttpRequestMessage(httpMethod, finalURL);
            var responseJson = await client.SendAsync(request);
            return await responseJson.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine("ERROR: " + e.Message);
            return string.Empty;
        }


    }
}
