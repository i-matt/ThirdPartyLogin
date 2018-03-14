using log4net;
using RootProject.Data;
using RootProject.Models.Domain;
using RootProject.Models.Requests;
using RootProject.Models.ViewModels;
using RootProject.Services.Cryptography;
using RootProject.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RootProject.Services
{
    public enum Provider
    {
        LinkedIn,
        Facebook,
        Google
    }

    public class ThirdPartyLoginService : BaseService, IThirdPartyLoginService
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ThirdPartyLoginService));
        private IUserService _userService;
        private ICryptographyService _cryptoService;

        public int CreateBaseAccount(RegisterAddModel model)
        {
            model.Password = _cryptoService.GenerateRandomString(12);
            model.EmailConfirmed = true;
            model.ModifiedBy = "me";

            int accountId = _userService.InsertNewUser(model);
            return accountId;
        }

        //Returns an int > 0 if the email exists
        public int CheckEmail(string email)
        {
            UserBase uModel = new UserBase();
            uModel.Email = email;
            uModel = _userService.GetByEmail(uModel);
            return uModel.Id;
        }

        public PersonViewModel SelectPerson(int id)
        {
            PersonViewModel model = new PersonViewModel();
            DataProvider.ExecuteCmd(
                "Account_SelectInfoById",
                inputParamMapper: delegate (SqlParameterCollection paramCol)
                {
                    paramCol.AddWithValue("@Id", id);
                },
                singleRecordMapper: delegate (IDataReader reader, short set)
                {
                    switch (set)
                    {
                        case 0:
                            model = Mapper(reader);
                            break;
                        case 1:
                            if (model.Roles == null)
                            {
                                model.Roles = new List<string>();
                            }
                            model.Roles.Add(MapRoles(reader));
                            break;
                    }
                }
            );
            return model;
        }

        public bool LogIn(SocialProviderUser model, Provider type)
        {
            string tableProviderId = GetProviderId(model.Id, type);
            if (tableProviderId != null && tableProviderId != model.ProviderId)
            {
                log.Error("The provided Id: " + model.ProviderId + " does not match the one in the table: " + tableProviderId);
                return false;
            }

            if (tableProviderId == null)
            {
                //Create a record in the db
                SocialProviderAddRequest spModel = new SocialProviderAddRequest();
                spModel.Id = model.Id;
                spModel.ProviderId = model.ProviderId;
                InsertProviderAccount(spModel, type);
            }

            _userService.LogInSocial(model);
            return true;
        }

        public void InsertProviderAccount(SocialProviderAddRequest model, Provider type)
        {
            string t = null;
            switch(type)
            {
                case Provider.LinkedIn: t = "LinkedIn";
                    break;
                case Provider.Facebook: t = "Facebook";
                    break;
                case Provider.Google: t = "Google";
                    break;
                default: return;
            }

            DataProvider.ExecuteNonQuery(
                "Accounts_" + t + "_Insert",
                inputParamMapper: delegate(SqlParameterCollection paramCol)
                {
                    paramCol.AddWithValue("@Id", model.Id);
                    paramCol.AddWithValue("@ProviderId", model.ProviderId);
                }
            );
        }

        public string GetProviderId(int accountId, Provider type)
        {
            string t = null;
            switch (type)
            {
                case Provider.LinkedIn:
                    t = "LinkedIn";
                    break;
                case Provider.Facebook:
                    t = "Facebook";
                    break;
                case Provider.Google:
                    t = "Google";
                    break;
                default: return "";
            }

            string providerId = null;
            DataProvider.ExecuteCmd(
                "Accounts_" + t + "_SelectProviderId",
                inputParamMapper: delegate (SqlParameterCollection paramCol)
                {
                    paramCol.AddWithValue("@Id", accountId);
                },
                singleRecordMapper: delegate (IDataReader reader, short set)
                {
                    providerId = reader.GetSafeString(0);
                }
            );
            return providerId;
        }

        public async Task<FacebookUserAuth> GetFBUserInfo(string accessToken)
        {
            string url = "https://graph.facebook.com/me?access_token=" + accessToken + "&fields=email";
            var httpClient = new HttpClient();

            HttpResponseMessage httpResponseMessage;
            FacebookUserAuth content = new FacebookUserAuth();
            try
            {
                httpResponseMessage = await httpClient.GetAsync(url);
                httpResponseMessage.EnsureSuccessStatusCode();

                string strCont = await httpResponseMessage.Content.ReadAsStringAsync();

                JavaScriptSerializer js = new JavaScriptSerializer();
                content = js.Deserialize<FacebookUserAuth>(strCont);
            }
            catch (Exception ex)
            {
                log.Error("Error getting FaceBook user information", ex);
            }
            return content;
        }

        private PersonViewModel Mapper(IDataReader reader)
        {
            PersonViewModel model = new PersonViewModel();
            int index = 0;
            model.FirstName = reader.GetSafeString(index++);
            model.MiddleInitial = reader.GetSafeString(index++);
            model.LastName = reader.GetSafeString(index++);
            model.DOB = reader.GetSafeDateTime(index++);
            model.Email = reader.GetSafeString(index++);

            return model;
        }

        private string MapRoles(IDataReader reader)
        {
            string role = reader.GetSafeString(0);
            return role;
        }

        public ThirdPartyLoginService(IUserService UserService, ICryptographyService CryptoService)
        {
            _userService = UserService;
            _cryptoService = CryptoService;
        }
    }
}
