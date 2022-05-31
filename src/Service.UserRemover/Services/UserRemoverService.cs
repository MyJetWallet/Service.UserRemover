using System;
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
using Service.PersonalData.Grpc;
using Service.PersonalData.Grpc.Contracts;
using Service.UserRemover.Grpc;
using Service.UserRemover.Grpc.Models;
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

        public UserRemoverService(ILogger<UserRemoverService> logger, IPersonalDataServiceGrpc personalData,
            IClientProfileService clientProfile, IClientWalletService clientWalletService, IWalletService walletService,
            IClientCommentsService clientCommentsService, IKycStatusService kycStatusService,
            IServiceBusPublisher<ClientAuditLogModel> publisher)
        {
            _logger = logger;
            _personalData = personalData;
            _clientProfile = clientProfile;
            _clientWalletService = clientWalletService;
            _walletService = walletService;
            _clientCommentsService = clientCommentsService;
            _kycStatusService = kycStatusService;
            _publisher = publisher;
        }

        public async Task<OperationResponse> RemoveUser(RemoveUserRequest request)
        {
            try
            {
                _logger.LogInformation("Removing user {request}", request.ToJson());

                var pdResponse = await _personalData.DeactivateClientAsync(new DeactivateClientRequest()
                {
                    Id = request.ClientId,
                    AuditLog = new AuditLogGrpcContract
                    {
                        TraderId = request.ClientId,
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
                    ClientId = request.ClientId,
                    ManagerId = $"Service.UserRemover | {request.Officer}",
                    Text = request.Comment
                });

                if (!commentResponse.IsSuccess)
                    return new OperationResponse()
                    {
                        IsSuccess = false,
                        ErrorMessage = commentResponse.Error
                    };
                
                var profileResponse = await _clientProfile.AddBlockerToClient(new AddBlockerToClientRequest
                {
                    ClientId = request.ClientId,
                    BlockerReason = request.Comment,
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
                    ClientId = request.ClientId,
                    Agent = request.Officer,
                    Comment = request.Comment,
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
                    await _walletService.GetDefaultWalletAsync(new JetClientIdentity(request.BrokerId, request.BrandId,
                        request.ClientId));

                await _clientWalletService.SetEarnProgramByWalletAsync(new SetEarnProgramByWalletRequest()
                {
                    WalletId = wallet.WalletId,
                    EnableEarnProgram = false
                });

                await _publisher.PublishAsync(new ClientAuditLogModel
                {
                    Module = "Service.UserRemover",
                    Data = null,
                    ClientId = request.ClientId,
                    UnixDateTime = DateTime.UtcNow.UnixTime(),
                    Message = $"User deleted by officer {request.Officer} with comment: {request.Comment}"
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
    }
}