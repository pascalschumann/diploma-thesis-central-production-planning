﻿using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.Context;
using Master40.DB.Data.Initializer.Tables;

namespace Master40.DB.Data.Initializer
{
    public static class MasterDBInitializerTruck
    {
        public static void DbInitialize(MasterDBContext context)
        {
            context.Database.EnsureCreated();

            // Look for any Entrys.
            if (context.Articles.Any())
                return;   // DB has been seeded
            
            // Resource Definitions
            var resourceTools = MasterTableResourceTool.Init(context);
            // requires Tools
            var resource = MasterTableResource.Init(context);
            // requires Tools and Resources
            var resourceSkills = MasterTableResourceSkill.Init(context);
            // requires Tools, Resources, and Skills
            var resourceSetups = MasterTableResourceSetup.Init(context);

            // Article Definitions
            var units = MasterTableUnit.Init(context);
            var articleTypes = MasterTableArticleType.Init(context);
            // requires Units and Article Types 
            var articles = MasterTableArticle.Init(context);

            MasterTableStock.Init(context, articles);

            var operations = MasterTableOperation.Init(context);

            var boms = MasterTableBom.Init(context);

            var businessPartner = MasterTableBusinessPartner.Init(context);

            MasterTableArticleToBusinessPartner.Init(context);
        }
    }
}