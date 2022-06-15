using System.Collections.Generic;
using MyNoSqlServer.Abstractions;

namespace Service.UserRemover.Domain.Models
{
    public class DeleteReasonsNoSqlEntity : MyNoSqlDbEntity
    {
        public const string TableName = "myjetwallet-remover-reasons";

        public static string GeneratePartitionKey() => "RemoveReasons";
        public static string GenerateRowKey() => "RemoveReasons";

        public List<string> ReasonTemplateIds { get; set; }

        public static DeleteReasonsNoSqlEntity Create(List<string> templates)
        {
            return new DeleteReasonsNoSqlEntity()
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(),
                ReasonTemplateIds = templates
            };
        }
    }
}