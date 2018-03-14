using log4net;
using Newtonsoft.Json;
using RootProject.Models.Domain;
using RootProject.Services.Interfaces;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace RootProject.Web.Controllers.Api
{
    [RoutePrefix("TOKEN_AUTH_ENDPOINT"), AllowAnonymous]
    public class GoogleAuthController : ApiController
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(GoogleAuthController));
        private IGoogleAuthService _service;

        public string ClientID { get; private set; }

        [Route, HttpPost]
        public HttpResponseMessage Post(GoogleApiTokenInfo tokenId)
        {
            try
            {
                const string GoogleApiTokenInfoUrl = "https://www.googleapis.com/oauth2/v3/tokeninfo";

                var httpClient = new HttpClient();
                var requestUri = new Uri(string.Format(GoogleApiTokenInfoUrl, tokenId.tokenId));

                HttpResponseMessage httpResponseMessage;
                try
                {
                    httpResponseMessage = httpClient.GetAsync(requestUri).Result;
                }
                catch (Exception ex)
                {
                    log.Error("Error Authenticating Google Token", ex);
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message); ;
                }

                if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
                {
                    try
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, httpResponseMessage.StatusCode);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Bad Request from Google Auth Controller", ex);
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
                    }
                }

                var response = httpResponseMessage.Content.ReadAsStringAsync().Result;
                var googleApiTokenInfo = JsonConvert.DeserializeObject<GoogleApiTokenInfo>(response);

                ProviderUserDetails resp = new ProviderUserDetails
                {
                    Email = googleApiTokenInfo.email,
                    FirstName = googleApiTokenInfo.given_name,
                    LastName = googleApiTokenInfo.family_name,
                    Locale = googleApiTokenInfo.locale,
                    Name = googleApiTokenInfo.name,
                    ProviderUserId = googleApiTokenInfo.sub,
                    Picture = googleApiTokenInfo.picture
                };
                return Request.CreateResponse(HttpStatusCode.OK, resp);
            }
            catch (Exception ex)
            {
                log.Error("Google Auth Controller Failed", ex);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
            }
        }

        public GoogleAuthController(IGoogleAuthService Service)
        {
            _service = Service;
        }
    }
}
