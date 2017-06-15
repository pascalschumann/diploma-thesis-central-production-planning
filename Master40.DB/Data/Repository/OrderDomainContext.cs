﻿using Master40.DB.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Master40.DB.Data.Repository
{
    public class OrderDomainContext : DbContext
    { 
        public OrderDomainContext(DbContextOptions<OrderDomainContext> options) : base(options) 
        {
            Orders = base.Set<Order>();
            OrderParts = base.Set<OrderPart>();
            BusinessPartners = base.Set<BusinessPartner>();
            Articles = base.Set<Article>();
            Stocks = base.Set<Stock>();
            Units = base.Set<Unit>();
            ArticleTypes = base.Set<ArticleType>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<ArticleBom>();
            modelBuilder.Ignore<WorkSchedule>();
            modelBuilder.Ignore<ProductionOrder>();
            modelBuilder.Ignore<DemandToProvider>();
            modelBuilder.Ignore<DemandOrderPart>();
            modelBuilder.Ignore<Purchase>();

            modelBuilder.Entity<Article>()
                .HasOne(a => a.Stock)
                .WithOne(s => s.Article)
                .HasForeignKey<Stock>(b => b.ArticleForeignKey);

            modelBuilder.Entity<ArticleToBusinessPartner>()
                .HasAlternateKey(x => new { x.ArticleId, x.BusinessPartnerId });

        }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderPart> OrderParts { get; set; }
        public DbSet<BusinessPartner> BusinessPartners { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<ArticleType> ArticleTypes { get; set; }

        //complex Querys
        public IQueryable<Order> GetAllOrders
        {
            get
            {
                return Orders.Include(x => x.OrderParts)
                                .Include(x => x.BusinessPartner)
                                .Where(x => x.BusinessPartner.Debitor)
                                .AsNoTracking();
            }
        }

        public IQueryable<Order> ById(int id)
        {
            return Orders.Include(x => x.OrderParts)
                            .Include(x => x.BusinessPartner)
                            .Where(x => x.BusinessPartner.Debitor)
                            .Where(x => x.Id == id);
        }

        public IQueryable<Article> GetSellableArticles
        {
            get
            { 
                return Articles.Include(x => x.ArticleType)
                            .Where(t => t.ArticleType.Name == "Assembly")
                            .AsNoTracking();
            }
        }

        public IQueryable<Article> GetPuchaseableArticles
        {
            get
            {
                return Articles.Include(x => x.ArticleType)
                    .Where(t => t.ArticleType.Name == "Material")
                    .AsNoTracking();
            }
        }



    }
}