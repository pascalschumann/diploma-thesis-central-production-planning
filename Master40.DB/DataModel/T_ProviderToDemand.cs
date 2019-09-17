using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.Interfaces;

namespace Master40.DB.DataModel
{
    public class T_ProviderToDemand : BaseEntity, ILinkDemandAndProvider
    {
        public int ProviderId { get; set; }
        public int DemandId { get; set; }
        
        public decimal Quantity { get; set; }

        public override string ToString()
        {
            return $"provider: {ProviderId}, demand: {DemandId}";
        }
        
        public Id GetProviderId()
        {
            return new Id(ProviderId);
        }

        public Id GetDemandId()
        {
            return new Id(DemandId);
        }

        public Quantity GetQuantity()
        {
            return new Quantity(Quantity);
        }
    }
}