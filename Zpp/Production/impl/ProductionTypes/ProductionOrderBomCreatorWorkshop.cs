using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.Configuration;
using Zpp.DataLayer;
using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.DataLayer.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.ProviderDomain.Wrappers;
using Zpp.Util;

namespace Zpp.Production.impl.ProductionTypes
{
    /**
     * (quantity * articleBom.Quantity) ProductionOrderBoms will be created
     * ProductionOrderOperation.Duration == articleBom.Duration
     */
    public class ProductionOrderBomCreatorWorkshop : IProductionOrderBomCreator
    {
        private readonly Dictionary<M_Operation, List<ProductionOrderOperation>>
            _alreadyCreatedProductionOrderOperations =
                new Dictionary<M_Operation, List<ProductionOrderOperation>>();
        private readonly IDbMasterDataCache _dbMasterDataCache =
            ZppConfiguration.CacheManager.GetMasterDataCache();

        public ProductionOrderBomCreatorWorkshop()
        {
            if (Configuration.ZppConfiguration.ProductionType.Equals(ProductionType.WorkshopProduction) == false)
            {
                throw new MrpRunException("This is class is intended for productionType WorkshopProduction.");
            }
        }

        public Demands CreateProductionOrderBomsForArticleBom(
            M_ArticleBom articleBom, Quantity quantity,
            ProductionOrder parentProductionOrder)
        {

            Demands newProductionOrderBoms = new Demands();
            for (int i = 0; i < quantity.GetValue(); i++)
            {
                ProductionOrderOperation productionOrderOperation = null;
                if (articleBom.OperationId == null)
                {
                    throw new MrpRunException(
                        "Every PrOBom must have an operation. Add an operation to the articleBom.");
                }

                // load articleBom.Operation
                if (articleBom.Operation == null)
                {
                    articleBom.Operation =
                        _dbMasterDataCache.M_OperationGetById(
                            new Id(articleBom.OperationId.GetValueOrDefault()));
                }

                // don't create an productionOrderOperation twice, take existing
                if (_alreadyCreatedProductionOrderOperations.ContainsKey(articleBom.Operation))
                {
                    if (_alreadyCreatedProductionOrderOperations[articleBom.Operation].Count > i)
                    {
                        productionOrderOperation =
                            _alreadyCreatedProductionOrderOperations[articleBom.Operation][i];
                    }
                }
                else
                {
                    _alreadyCreatedProductionOrderOperations.Add(articleBom.Operation,
                        new List<ProductionOrderOperation>());
                }

                ProductionOrderBom newProductionOrderBom =
                    ProductionOrderBom.CreateTProductionOrderBom(articleBom, parentProductionOrder,
                        productionOrderOperation, new Quantity(1));

                if (newProductionOrderBom.HasOperation() == false)
                {
                    throw new MrpRunException(
                        "Every PrOBom must have an operation. Add an operation to the articleBom.");
                }
                
                if (_alreadyCreatedProductionOrderOperations[articleBom.Operation].Count <= i)
                {
                    _alreadyCreatedProductionOrderOperations[articleBom.Operation].Add(
                        newProductionOrderBom.GetProductionOrderOperation());
                }
                
                newProductionOrderBoms.Add(newProductionOrderBom);
                
            }

            return newProductionOrderBoms;
        }
    }
}