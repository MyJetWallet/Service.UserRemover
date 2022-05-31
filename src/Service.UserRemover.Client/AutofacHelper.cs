using Autofac;
using Service.UserRemover.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.UserRemover.Client
{
    public static class AutofacHelper
    {
        public static void RegisterUserRemoverClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new UserRemoverClientFactory(grpcServiceUrl);

            builder.RegisterInstance(factory.GetHelloService()).As<IUserRemoverService>().SingleInstance();
        }
    }
}
