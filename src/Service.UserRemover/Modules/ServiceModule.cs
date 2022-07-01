using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using MyJetWallet.Sdk.NoSql;
using MyJetWallet.Sdk.ServiceBus;
using MyJetWallet.Sdk.WalletApi.Wallets;
using Service.AdminDatasource.Client;
using Service.Authorization.Client;
using Service.ClientAuditLog.Client;
using Service.ClientProfile.Client;
using Service.ClientWallets.Client;
using Service.HighYieldEngine.Client;
using Service.KYC.Client;
using Service.MessageTemplates.Client;
using Service.PersonalData.Client;
using Service.VerificationCodes.Client;

namespace Service.UserRemover.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<WalletService>()
                .As<IWalletService>()
                .SingleInstance();
            
            var myNoSqlClient = builder.CreateNoSqlClient(Program.Settings.MyNoSqlReaderHostPort, Program.LogFactory);

            builder.RegisterKycStatusClientsGrpcOnly(Program.Settings.KycGrpcServiceUrl);
            builder.RegisterClientWalletsClients(myNoSqlClient, Program.Settings.ClientWalletsGrpcServiceUrl);
            builder.RegisterPersonalDataClient(Program.Settings.PersonalDataGrpcServiceUrl);
            builder.RegisterClientProfileClients(myNoSqlClient, Program.Settings.ClientProfileGrpcServiceUrl);
            builder.RegisterClientCommentsClient(Program.Settings.AdminDatasourceGrpcServiceUrl);
            builder.RegisterMessageTemplatesClient(Program.Settings.MessageTemplatesGrpcServiceUrl);
            builder.RegisterVerificationCodesClient(Program.Settings.VerificationCodesGrpcUrl);
            builder.RegisterHighYieldEngineBackofficeService(Program.Settings.HighYieldEngineGrpcServiceUrl);
            builder.RegisterAuthorizationClient(Program.Settings.AuthorizationGrpcServiceUrl);
            
            var authMyServiceBusClient = builder.RegisterMyServiceBusTcpClient(()=>Program.Settings.AuthServiceBusHostPort, Program.LogFactory);
            builder.RegisterClientAuditLogPublisher(authMyServiceBusClient);
        }
    }
}