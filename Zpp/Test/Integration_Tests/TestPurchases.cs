using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Xunit;
using Zpp.DataLayer;
using Zpp.ZppSimulator;

namespace Zpp.Test.Integration_Tests
{
    public class TestPurchases : AbstractTest
    {

        public TestPurchases()
        {
            
        }
        
        [Fact]
        public void TestPurchaseQuantityIsAMultipleOfPackSize()
        {
            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();

        IDbMasterDataCache dbMasterDataCache =
            ZppConfiguration.CacheManager.GetMasterDataCache();
            IDbTransactionData persistedTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            List<T_PurchaseOrderPart> tPurchaseOrderParts = persistedTransactionData
                .PurchaseOrderPartGetAll().GetAllAs<T_PurchaseOrderPart>();

            foreach (var tPurchaseOrderPart in tPurchaseOrderParts)
            {
                M_ArticleToBusinessPartner articleToBusinessPartner =
                    dbMasterDataCache.M_ArticleToBusinessPartnerGetAllByArticleId(
                        new Id(tPurchaseOrderPart.ArticleId))[0];
                Quantity multiplier = new Quantity(1);
                while (multiplier.GetValue() * articleToBusinessPartner.PackSize <
                       tPurchaseOrderPart.Quantity)
                {
                    multiplier.IncrementBy(new Quantity(1));
                }

                Quantity expectedPurchaseQuantity = new Quantity(multiplier.GetValue() *
                                                                 articleToBusinessPartner.PackSize);
                Assert.True(tPurchaseOrderPart.GetQuantity().Equals(expectedPurchaseQuantity),
                    $"Quantity of PurchaseOrderPart ({tPurchaseOrderPart}) ist not a multiple of packSize.");
            }
        }
        
    }
}