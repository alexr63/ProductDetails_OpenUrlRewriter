﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Framework.Providers;
using Satrabel.HttpModules.Provider;
using SelectedHotelsModel;

namespace Satrabel.OpenUrlRewriter.ProductDetails
{
    public class ProductDetailsUrlRuleProvider : UrlRuleProvider
    {

        private const string ProviderType = "urlRule";
        private const string ProviderName = "ProductDetailsUrlRuleProvider";

        private readonly ProviderConfiguration _providerConfiguration = ProviderConfiguration.GetProviderConfiguration(ProviderType);
        private readonly bool includePageName = true;

        public ProductDetailsUrlRuleProvider() {
            var objProvider = (Provider)_providerConfiguration.Providers[ProviderName];
            if (!String.IsNullOrEmpty(objProvider.Attributes["includePageName"]))
            {
                includePageName = bool.Parse(objProvider.Attributes["includePageName"]);
            }
            //CacheKeys = new string[] { "ProductDetails-ProperyValues-All" };
        }

        public override List<UrlRule> GetRules(int PortalId)
        {
            List<UrlRule> Rules = new List<UrlRule>();

            using (SelectedHotelsEntities db = new SelectedHotelsEntities())
            {
                ModuleController moduleController = new ModuleController();
                ArrayList hotelListModules = moduleController.GetModulesByDefinition(PortalId, "HotelList");
                foreach (ModuleInfo module in hotelListModules.OfType<ModuleInfo>())
                {
                    List<TabInfo> childTabs = TabController.GetTabsByParent(module.TabID, PortalId);
                    object setting = module.ModuleSettings["location"];
                    int? locationId = null;
                    if (setting != null)
                    {
                        try
                        {
                            locationId = Convert.ToInt32(setting);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    if (locationId == null)
                    {
                        continue;
                    }

                    int? hotelTypeId = null;
                    HotelType hotelType = null;
                    setting = module.ModuleSettings["hoteltype"];
                    if (setting != null)
                    {
                        hotelTypeId = Convert.ToInt32(setting);
                        hotelType = db.HotelTypes.Find(hotelTypeId);
                    }

                    var hotels = db.HotelsInLocation(locationId.Value, hotelTypeId);
                    foreach (var hotel in hotels)
                    {
                        var locations =
                            hotel.HotelLocations.OrderByDescending(hl => hl.Location.LocationTypeId)
                                .Select(hl => hl.Location.Name);
                        locations.Reverse();

                        var rule = new UrlRule
                        {
                            CultureCode = module.CultureCode,
                            TabId = childTabs[0].TabID,
                            RuleType = UrlRuleType.Module,
                            Parameters = "id=" + hotel.Id,
                            Action = UrlRuleAction.Rewrite,
                            Url = String.Join("/", locations) + "/" + (hotelType != null ? hotelType.Name + "/" : String.Empty) + CleanupUrl(hotel.Name),
                            RemoveTab = !includePageName
                        };
                        Rules.Add(rule);
                    }
                }

                ArrayList clothDetailsModules = moduleController.GetModulesByDefinition(PortalId, "Cloth Details");
                foreach (ModuleInfo module in clothDetailsModules.OfType<ModuleInfo>())
                {
                    IEnumerable<Cloth> clothes = (from p in db.Products
                                                where !p.IsDeleted
                                                select p).OfType<Cloth>().ToList();
                    foreach (var cloth in clothes)
                    {
                        var deparmentsNames = cloth.Departments.Select(d => CleanupUrl(d.Name));
                        var rule = new UrlRule
                        {
                            CultureCode = module.CultureCode,
                            TabId = module.TabID,
                            RuleType = UrlRuleType.Module,
                            Parameters = "id=" + cloth.Id,
                            Action = UrlRuleAction.Rewrite,
                            Url = CleanupUrl(cloth.Brand.Name) + "/" + CleanupUrl(cloth.Name),
                            RemoveTab = !includePageName
                        };
                        Rules.Add(rule);
                    }
                }
            }
             
            return Rules;
        }
    }
}