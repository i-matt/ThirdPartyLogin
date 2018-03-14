using log4net;
using RootProject.Models.Domain;
using RootProject.Models.Requests;
using RootProject.Models.Responses;
using RootProject.Models.ViewModels;
using RootProject.Services;
using RootProject.Services.Interfaces;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace RootProject.Web.Controllers
{
    [RoutePrefix("THIRD_PARTY"), AllowAnonymous]
    public class ThirdPartyLoginController : ApiController
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ThirdPartyLoginController));
        private IThirdPartyLoginService _service;
        private IAssignRoleService _roleService;
        private IConfigSettingsService _configService;

        [Route("{provider}"), HttpPost]
        public async Task<HttpResponseMessage> RegisterThirdParty(string provider, ThirdPartyCompleteModel model)
        {
            string errorMsg = "provider";
            try
            {
                Provider type;
                switch(provider)
                {
                    case "linkedin":
                        type = Provider.LinkedIn;
                        errorMsg = "LinkedIn";
                        break;
                    case "google":
                        type = Provider.Google;
                        errorMsg = "Google";
                        break;
                    case "facebook":
                        type = Provider.Facebook;
                        errorMsg = "Facebook";
                        break;
                    default: return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "No provider exists with this name");
                }

                if(type == Provider.Facebook)
                {
                    FacebookUserAuth userInfo = await _service.GetFBUserInfo(model.AccessToken);
                    model.Email = userInfo.Email;
                    model.ProviderId = userInfo.Id;
                }

                int accountId = _service.CheckEmail(model.Email);
                if(accountId == 0)
                {
                    accountId = Register(model, model.Role);
                    if(accountId == -1)
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Login Error");
                    }
                }

                SocialProviderUser spu = new SocialProviderUser();
                spu.Email = model.Email;
                spu.Id = accountId;
                spu.ProviderId = model.ProviderId;
                if (_service.LogIn(spu, type))
                {
                    ItemResponse<PersonViewModel> resp = new ItemResponse<PersonViewModel>();
                    resp.Item = _service.SelectPerson(spu.Id);
                    return Request.CreateResponse(HttpStatusCode.OK, resp);
                }
                else
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Something went wrong logging in with " + errorMsg);
                }
            }
            catch(Exception ex)
            {
                log.Error("Error registering " + errorMsg + " account", ex);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [Route("STORES_PRIVATE_INFO/{provider}"), HttpGet]
        public HttpResponseMessage GetApiKey(string provider)
        {
            try
            {
                ItemResponse<string> resp = new ItemResponse<string>();
                switch (provider)
                {
                    case "linkedin": resp.Item = _configService.GetConfigValueByName("linkedin:APIKey").ConfigValue;
                        break;
                    case "facebook": resp.Item = _configService.GetConfigValueByName("facebook:APPKey").ConfigValue;
                        break;
                    case "google": resp.Item = _configService.GetConfigValueByName("google:clientId").ConfigValue;
                        break;
                    default: return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "This provider could not be found");
                }
                return Request.CreateResponse(HttpStatusCode.OK, resp);
            }
            catch(Exception ex)
            {
                log.Error("Error getting " + provider + " api key", ex);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        private int Register(RegisterAddModel model, int role)
        {
            if (role == 0)
            {
                return -1;
            }

            //Create base account
            int accountId = _service.CreateBaseAccount(model);

            //Assign Role
            AssignRoles arModel = new AssignRoles();
            arModel.AccountId = accountId;
            arModel.RoleId = role;
            _roleService.Insert(arModel);

            arModel.RoleId = 3;
            _roleService.Insert(arModel);

            return accountId;
        }

        public ThirdPartyLoginController(IThirdPartyLoginService Service, IAssignRoleService RoleService, IConfigSettingsService ConfigService)
        {
            _service = Service;
            _roleService = RoleService;
            _configService = ConfigService;
        }
    }
}
