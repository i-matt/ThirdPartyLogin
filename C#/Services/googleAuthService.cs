using RootProject.Models.Requests;
using RootProject.Services.Interfaces;
using System.Data.SqlClient;

namespace RootProject.Services
{
    public class GoogleAuthService : BaseService, IGoogleAuthService 
    {
        public void Insert(SocialProviderAddRequest model)
        {
            this.DataProvider.ExecuteNonQuery(
                "Accounts_Google_Insert",
                inputParamMapper: delegate (SqlParameterCollection paramCol)
                {
                    paramCol.AddWithValue("@Id", model.Id);
                    paramCol.AddWithValue("@ProviderId", model.ProviderId);
                },
                returnParameters: null
            );
        }
    }
}
