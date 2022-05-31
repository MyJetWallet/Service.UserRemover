using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.UserRemover.Settings
{
    public class SettingsModel
    {
        [YamlProperty("UserRemover.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("UserRemover.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("UserRemover.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }
        
        [YamlProperty("UserRemover.MyNoSqlReaderHostPort")]
        public string MyNoSqlReaderHostPort { get; set; }
        
        [YamlProperty("UserRemover.KycGrpcServiceUrl")]
        public string KycGrpcServiceUrl { get; set; }
        
        [YamlProperty("UserRemover.ClientWalletsGrpcServiceUrl")]
        public string ClientWalletsGrpcServiceUrl { get; set; }
        
        [YamlProperty("UserRemover.PersonalDataGrpcServiceUrl")]
        public string PersonalDataGrpcServiceUrl { get; set; }
        
        [YamlProperty("UserRemover.ClientProfileGrpcServiceUrl")]
        public string ClientProfileGrpcServiceUrl { get; set; }
        
        [YamlProperty("UserRemover.AdminDatasourceGrpcServiceUrl")]
        public string AdminDatasourceGrpcServiceUrl { get; set; }
        
        [YamlProperty("UserRemover.AuthServiceBusHostPort")]
        public string AuthServiceBusHostPort { get; set; }
    }
}
