using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.Context;
using Master40.DB.DataModel;
using Microsoft.EntityFrameworkCore;

namespace Zpp.Utils
{
    public class ArticleTree
    {
        private static readonly NLog.Logger LOGGER = NLog.LogManager.GetCurrentClassLogger();
        private readonly ProductionDomainContext _productionDomainContext;
        
        // int is the id of an article
        private readonly Dictionary<int, List<M_Article>> _adjacencyList = new Dictionary<int, List<M_Article>>() ;
        private int? rootArticleId = null;

        public ArticleTree(int articleId, ProductionDomainContext _productionDomainContext)
        {
            rootArticleId = articleId;
            this._productionDomainContext = _productionDomainContext;
            buildArticleTree(articleId);
        }
        
        /**
         * Build up the tree from given articleBom. ATTENTION: recursive
         */
        private void buildArticleTree(int articleId)
        {
            if (!_adjacencyList.ContainsKey(articleId))
            {
                _adjacencyList.Add(articleId, new List<M_Article>());
            }
            
            M_Article article = _productionDomainContext.Articles.Include(m => m.ArticleBoms)
                .ThenInclude(m => m.ArticleChild).Single(x => x.Id == articleId);
            if (article.ArticleBoms != null )
            {
                foreach (M_ArticleBom articleBom in article.ArticleBoms)
                {
                    _adjacencyList[article.Id].Add(articleBom.ArticleChild);
                    buildArticleTree(articleBom.ArticleChild.Id);
                }
            }
        }

        public override string ToString()
        {
            String mystring = "The ArticleTree of Article " + rootArticleId.ToString() + Environment.NewLine;
            foreach (int articleId in _adjacencyList.Keys)
            {
                if (!_adjacencyList[articleId].Any())
                {
                    continue;
                }
                mystring += "Childs of article " +articleId + ":";
                foreach (M_Article article in _adjacencyList[articleId])
                {
                    mystring += article.Id + ": " + article.Name + ", ";
                }

                mystring = mystring.Substring(0, mystring.Length-2);
                mystring += Environment.NewLine;
            }

            return mystring;
        }

    }
}