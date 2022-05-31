using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using Service.UserRemover.Grpc;

namespace Service.UserRemover.Client
{
    [UsedImplicitly]
    public class UserRemoverClientFactory: MyGrpcClientFactory
    {
        public UserRemoverClientFactory(string grpcServiceUrl) : base(grpcServiceUrl)
        {
        }

        public IUserRemoverService GetHelloService() => CreateGrpcService<IUserRemoverService>();
    }
}
