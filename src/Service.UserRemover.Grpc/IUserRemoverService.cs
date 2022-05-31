using System.ServiceModel;
using System.Threading.Tasks;
using Service.UserRemover.Grpc.Models;

namespace Service.UserRemover.Grpc
{
    [ServiceContract]
    public interface IUserRemoverService
    {
        [OperationContract]
        Task<OperationResponse> RemoveUser(RemoveUserRequest request);
    }
}