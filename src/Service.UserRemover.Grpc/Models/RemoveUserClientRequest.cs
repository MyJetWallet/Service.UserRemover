using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Service.UserRemover.Grpc.Models
{
    [DataContract]
    public class RemoveUserClientRequest
    {
        [DataMember(Order = 1)]
        public string ClientId { get; set; }
        [DataMember(Order = 2)]
        public string BrokerId { get; set; }        
        [DataMember(Order = 3)]
        public string BrandId { get; set; }
        [DataMember(Order = 4)]
        public List<string> Reasons { get; set; }
        [DataMember(Order = 5)]
        public string Token { get; set; }
    }
}