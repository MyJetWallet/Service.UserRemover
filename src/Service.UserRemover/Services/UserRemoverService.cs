using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyJetWallet.Sdk.WalletApi.Wallets;
using Service.AdminDatasource.Grpc;
using Service.AdminDatasource.Grpc.Models.ClientComments.Requests;
using Service.ClientAuditLog.Domain.Models;
using Service.ClientProfile.Domain.Models;
using Service.ClientProfile.Grpc;
using Service.ClientProfile.Grpc.Models.Requests;
using Service.ClientWallets.Grpc;
using Service.ClientWallets.Grpc.Models;
using Service.KYC.Domain.Models.Enum;
using Service.KYC.Grpc;
using Service.KYC.Grpc.Models;
using Service.MessageTemplates.Client;
using Service.MessageTemplates.Grpc;
using Service.MessageTemplates.Grpc.Models;
using Service.PersonalData.Grpc;
using Service.PersonalData.Grpc.Contracts;
using Service.UserRemover.Grpc;
using Service.UserRemover.Grpc.Models;
using Service.VerificationCodes.Grpc;
using Service.VerificationCodes.Grpc.Models;
using OperationResponse = Service.UserRemover.Grpc.Models.OperationResponse;

namespace Service.UserRemover.Services
{
    public class UserRemoverService : IUserRemoverService
    {
        private readonly ILogger<UserRemoverService> _logger;
        private readonly IPersonalDataServiceGrpc _personalData;
        private readonly IClientProfileService _clientProfile;
        private readonly IClientWalletService _clientWalletService;
        private readonly IClientCommentsService _clientCommentsService;
        private readonly IWalletService _walletService;
        private readonly IKycStatusService _kycStatusService;
        private readonly IServiceBusPublisher<ClientAuditLogModel> _publisher;
        private readonly ITemplateService _templateClient;
        private readonly IVerificationService _verificationService;
        public UserRemoverService(ILogger<UserRemoverService> logger, IPersonalDataServiceGrpc personalData,
            IClientProfileService clientProfile, IClientWalletService clientWalletService, IWalletService walletService,
            IClientCommentsService clientCommentsService, IKycStatusService kycStatusService,
            IServiceBusPublisher<ClientAuditLogModel> publisher, ITemplateService templateClient, IVerificationService verificationService)
        {
            _logger = logger;
            _personalData = personalData;
            _clientProfile = clientProfile;
            _clientWalletService = clientWalletService;
            _walletService = walletService;
            _clientCommentsService = clientCommentsService;
            _kycStatusService = kycStatusService;
            _publisher = publisher;
            _templateClient = templateClient;
            _verificationService = verificationService;
        }

        public async Task<OperationResponse> RemoveUserClient(RemoveUserClientRequest request)
        {
            _logger.LogInformation("Removing user by client request {request}", request.ToJson());
            var tokenResponse = await _verificationService.UseToken(new ValidateTokenRequest()
            {
                ClientId = request.ClientId,
                TokenId = request.Token
            });

            if (!tokenResponse.IsValid)
            {
                return new OperationResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid Token"
                };
            }
            
            var comment = await GetComment(request.Reasons);
            return await RemoveUser(request.ClientId, request.BrokerId, request.BrandId,request.ClientId, comment);
        }

        public async Task<OperationResponse> RemoveUserAdmin(RemoveUserAdminRequest request)
        {
            _logger.LogInformation("Removing user by admin request {request}", request.ToJson());
            var comment = $"User deleted by officer {request.Officer} with comment: {request.Comment}";
            return await RemoveUser(request.ClientId, request.BrokerId, request.BrandId, request.Officer, comment);
        }

        private async Task<OperationResponse> RemoveUser(string clientId, string brokerId, string brandId, string officer, string comment)
        {
            try
            {
                var pdResponse = await _personalData.DeactivateClientAsync(new DeactivateClientRequest()
                {
                    Id = clientId,
                    AuditLog = new AuditLogGrpcContract
                    {
                        TraderId = clientId,
                        ServiceName = "Service.UserRemover",
                        Context = "User deactivation by service"
                    }
                });

                if (!pdResponse.Ok)
                    return new OperationResponse()
                    {
                        IsSuccess = false,
                        ErrorMessage = "Unable to deactivate client in PD"
                    };
                
                var commentResponse = await _clientCommentsService.AddAsync(new AddClientCommentRequest
                {
                    ClientId = clientId,
                    ManagerId = $"Service.UserRemover | {officer}",
                    Text = comment
                });

                if (!commentResponse.IsSuccess)
                    return new OperationResponse()
                    {
                        IsSuccess = false,
                        ErrorMessage = commentResponse.Error
                    };
                
                var profileResponse = await _clientProfile.AddBlockerToClient(new AddBlockerToClientRequest
                {
                    ClientId = clientId,
                    BlockerReason = comment,
                    Type = BlockingType.Login,
                    ExpiryTime = DateTime.MaxValue
                });

                if (!profileResponse.IsSuccess)
                    return new OperationResponse()
                    {
                        IsSuccess = false,
                        ErrorMessage = profileResponse.Error
                    };
                
                var kycResponse = await _kycStatusService.SetKycStatusesAsync(new SetOperationStatusRequest
                {
                    ClientId = clientId,
                    Agent = officer,
                    Comment = comment,
                    DepositStatus = KycOperationStatus.Blocked,
                    TradeStatus = KycOperationStatus.Blocked,
                    WithdrawalStatus = KycOperationStatus.Blocked
                });

                if (!kycResponse.IsSuccess)
                    return new OperationResponse()
                    {
                        IsSuccess = false,
                        ErrorMessage = kycResponse.Error
                    };
                
                var wallet =
                    await _walletService.GetDefaultWalletAsync(new JetClientIdentity(brokerId, brandId,
                        clientId));

                await _clientWalletService.SetEarnProgramByWalletAsync(new SetEarnProgramByWalletRequest()
                {
                    WalletId = wallet.WalletId,
                    EnableEarnProgram = false
                });

                await _publisher.PublishAsync(new ClientAuditLogModel
                {
                    Module = "Service.UserRemover",
                    Data = null,
                    ClientId = clientId,
                    UnixDateTime = DateTime.UtcNow.UnixTime(),
                    Message = comment
                });

                return new OperationResponse
                {
                    IsSuccess = true
                };
            }
            catch (Exception e)
            {
                return new OperationResponse
                {
                    IsSuccess = false,
                    ErrorMessage = e.Message
                };
            }
        }

        private async Task<string> GetComment(List<string> reasons)
        {
            reasons ??= new List<string>();
            var reasonsStr = string.Empty;
            try
            {
                var reasonsFull = new List<string>();
                foreach (var reason in reasons)
                {
                    var reasonFull = await _templateClient.GetTemplateBody(new GetTemplateBodyRequest()
                    {
                        TemplateId = reason,
                        Brand = "default",
                        Lang = "default"
                    });
                    reasonsFull.Add(reasonFull.Body);
                }

                reasonsStr = string.Join(", ", reasonsFull);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to parse deletion reasons");
            }

            return $"User deleted by request with reasons: {reasonsStr}";
        }
    }
}