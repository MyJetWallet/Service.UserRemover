using System.ServiceModel;
using System.Threading.Tasks;
using Service.UserRemover.Grpc.Models;

namespace Service.UserRemover.Grpc
{
    [ServiceContract]
    public interface IUserRemoverService
    {
        [OperationContract]
        Task<OperationResponse> RemoveUserClient(RemoveUserClientRequest request);
        
        [OperationContract]
        Task<OperationResponse> RemoveUserAdmin(RemoveUserAdminRequest request);
    }
}