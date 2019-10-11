using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Master40.DB.Interfaces;
using Zpp.Configuration;

namespace Zpp.DataLayer.DemandDomain.Wrappers
{
    public class CustomerOrderPart : Demand
    {
        private T_CustomerOrderPart _customerOrderPart;
        
        public CustomerOrderPart(IDemand demand) : base(demand)
        {
            _customerOrderPart = (T_CustomerOrderPart) demand;
        }

        public override IDemand ToIDemand()
        {
            return (T_CustomerOrderPart)_demand;
        }

        public override M_Article GetArticle()
        {
            return _dbMasterDataCache.M_ArticleGetById(GetArticleId());
        }

        public override Id GetArticleId()
        {
            return new Id(_customerOrderPart.ArticleId);
        }

        public override DueTime GetDueTime()
        {
            return GetStartTime();
        }

        public T_CustomerOrderPart GetValue()
        {
            return (T_CustomerOrderPart)_demand;
        }

        public override Duration GetDuration()
        {
            return Duration.Null();
        }

        public override void SetStartTime(DueTime startTime)
        {
            // is NOT allowed to change
            throw new System.NotImplementedException();
        }

        public override void SetDone()
        {
            _customerOrderPart.State = State.Finished;
        }

        public override void SetInProgress()
        {
            _customerOrderPart.State = State.Producing;
        }

        private void EnsureCustomerOrderIsLoaded()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            Id customerOrderId = new Id(_customerOrderPart.CustomerOrderId);
            _customerOrderPart.CustomerOrder =
                dbTransactionData.T_CustomerOrderGetById(customerOrderId);
        }

        public override DueTime GetEndTime()
        {
            EnsureCustomerOrderIsLoaded();

            DueTime dueTime = new DueTime(_customerOrderPart.CustomerOrder.DueTime);
            return dueTime;
        }

        public override bool IsDone()
        {
            return _customerOrderPart.State.Equals(State.Finished);
        }
    }
}