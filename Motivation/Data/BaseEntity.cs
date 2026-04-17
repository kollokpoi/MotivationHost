namespace Motivation.Data
{
    public class BaseEntity
    {
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        
        /// <summary>
        /// ID сущности во внешней системе (Bitrix24)
        /// </summary>
        public int? ExternalId { get; set; }
    }
}
