using System;
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
                ArrayList modules = moduleController.GetModulesByDefinition(PortalId, "Hotel Details");
                foreach (ModuleInfo module in modules.OfType<ModuleInfo>())
                {
                    TabController tabController = new TabController();
                    TabInfo tabInfo = tabController.GetTab(module.ParentTab.TabID);
                    TabInfo parentTabInfo = tabController.GetTab(tabInfo.ParentId);
                    ModuleInfo hotelListModuleInfo = null;
                    foreach (var childModule in parentTabInfo.ChildModules)
                    {
                        if (childModule.Value.ModuleDefinition.DefinitionName == "HotelList")
                        {
                            hotelListModuleInfo = childModule.Value;
                        }
                    }

                    if (hotelListModuleInfo == null)
                    {
                        continue;
                    }

                    object setting = hotelListModuleInfo.ModuleSettings["location"];
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

                    var hotels = db.HotelsInLocation(locationId.Value, null);
                    foreach (var hotel in hotels)
                    {
                        List<string> locations = new List<string>();
                        if (hotel.Location != null)
                        {
                            locations.Add(CleanupUrl(hotel.Location.Name));
                            if (hotel.Location.ParentLocation != null)
                            {
                                locations.Add(CleanupUrl(hotel.Location.ParentLocation.Name));
                                if (hotel.Location.ParentLocation.ParentLocation != null)
                                {
                                    locations.Add(CleanupUrl(hotel.Location.ParentLocation.ParentLocation.Name));
                                }
                            }
                        }
                        locations.Reverse();

                        var rule = new UrlRule
                        {
                            CultureCode = module.CultureCode,
                            TabId = module.TabID,
                            RuleType = UrlRuleType.Module,
                            Parameters = "id=" + hotel.Id,
                            Action = UrlRuleAction.Rewrite,
                            Url = String.Join("/", locations) + "/" + CleanupUrl(hotel.Name),
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