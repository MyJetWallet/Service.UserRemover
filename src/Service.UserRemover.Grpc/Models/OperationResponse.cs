using System.Runtime.Serialization;
using Service.UserRemover.Domain.Models;

namespace Service.UserRemover.Grpc.Models
{
    [DataContract]
    public class OperationResponse
    {
        [DataMember(Order = 1)]
        public bool IsSuccess { get; set; }
        [DataMember(Order = 2)]
        public string ErrorMessage { get; set; }
    }
}