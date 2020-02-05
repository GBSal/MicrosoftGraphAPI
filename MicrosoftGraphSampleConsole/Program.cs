using Microsoft.Graph;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;
using RestSharp;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Identity.Client;
using AuthenticationResult = Microsoft.Identity.Client.AuthenticationResult;

namespace MicrosoftGraphSampleConsole
{
    class Program
    {
        static string _appId = "49cca033-1e54-4119-b997-b2564a8f50a2";
        static string _appKey = "gDYAw.[qRl.J-qlwNSOSqC1jCGfHl799";
        static string _GraphRootUrl = "https://graph.microsoft.com";


        static void Main(string[] args)
        {

            //Method 1 
            //GetImageString();

            //Method 2 
           var imgStr=  GetImageStringII(_appId,_appKey,_GraphRootUrl);

            Console.WriteLine(imgStr);


            Console.WriteLine("Press any key to continue");
            Console.ReadKey();

        }




        #region "Method 1 By Using RestSharp request and getting base64String"
        static void GetImageString()
        {

            var adsAuthToken = GetAdsAuthToken(_appId, _appKey, _GraphRootUrl);

            var request = new RestRequest("{id}", Method.GET);

            request.AddHeader("Authorization", $"Bearer {adsAuthToken}");

            request.AddHeader("Content-type", "image/jpg");

            var client = new RestClient("https://graph.microsoft.com/v1.0/");

            request.Resource = "/users/mariya.nousheen@arup.com/photo/$value";
            var response = client.Execute(request);


            var photoString = Convert.ToBase64String(response.RawBytes, 0, response.RawBytes.Length);

        }

        static string GetAdsAuthToken(string appId, string appKey, string adsGraphRootUrl)
        {
            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

            AuthenticationResult result = null;

            try
            {
                IConfidentialClientApplication app;
                app = ConfidentialClientApplicationBuilder.Create(appId)
                                                          .WithClientSecret(appKey)
                                                          .WithAuthority(new Uri("https://login.microsoftonline.com/4ae48b41-0137-4599-8661-fc641fe77bea/oauth2/token"))
                                                          .Build();


                result = app.AcquireTokenForClient(scopes)
                                 .ExecuteAsync().Result;

                if (result != null)
                    return result.AccessToken;

            }
            catch (MsalUiRequiredException ex)
            {
                // The application doesn't have sufficient permissions.
                // - Did you declare enough app permissions during app creation?
                // - Did the tenant admin grant permissions to the application?
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70011"))
            {
                // Invalid scope. The scope has to be in the form "https://resourceurl/.default"
                // Mitigation: Change the scope to be as expected.
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }

            return "";

        }

        #endregion


        #region Method 2 Using Microsoft Identity and Microsoft Graph Client"

        static string GetImageStringII(string appId, string appKey, string graphRootUrl) {

            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(appId)
                                                      .WithClientSecret(appKey)
                                                      .WithAuthority(new Uri("https://login.microsoftonline.com/4ae48b41-0137-4599-8661-fc641fe77bea/oauth2/token"))
                                                      .Build();
            
            var delegateAuthProvider = new DelegateAuthenticationProvider((requestMessage) =>
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", app.AcquireTokenForClient(scopes)
                                                          .ExecuteAsync()
                                                          .Result.AccessToken);

                return Task.FromResult(0);
            });


            GraphServiceClient graphClient = new GraphServiceClient(delegateAuthProvider);

            var stream = graphClient.Users["mir.ali@arup.com"].Photo.Content
                                    .Request()
                                    .GetAsync()
                                    .Result;

            var ms = new MemoryStream();
            stream.CopyTo(ms);

            var str = Convert.ToBase64String(ms.ToArray());
            if (!string.IsNullOrEmpty(str)) return str;
            return "";
        }



        #endregion


        #region Classic Way of getting Token
        static string GetAdsAuthTokenClasicII(string appId, string appKey, string adsGraphRootUrl)
        {
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/x-www-form-urlencoded");

            var token = new
            {
                client_id = appId,
                client_secret = appKey,
                grant_type = "client_credentials",
                scope = "https://graph.microsoft.com/.default"
            };

            request.AddParameter("application/x-www-form-urlencoded", $"client_id={token.client_id}&client_secret={token.client_secret}&grant_type={token.grant_type}&scope={token.scope}", ParameterType.RequestBody);

            var client = new RestClient("https://login.microsoftonline.com/4ae48b41-0137-4599-8661-fc641fe77bea/oauth2/token");
            var response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            var tokenResponse = JsonConvert.DeserializeObject<TokenValidationResponse>(response.Content);


            return tokenResponse.Access_Token;
        }
        #endregion



    }


    public class TokenValidationResponse
    {
        public string Resource { get; set; }

        public string Access_Token { get; set; }

        public string Token_Type { get; set; }


    }
}
