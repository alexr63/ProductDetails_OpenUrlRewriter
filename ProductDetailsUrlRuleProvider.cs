using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using DotNetNuke.Entities.Modules;
using DotNetNuke.Framework.Providers;
using ProductList;
using Satrabel.HttpModules.Provider;

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
                ModuleController mc = new ModuleController();
                ArrayList modules = mc.GetModulesByDefinition(PortalId, "Hotel Details");
                foreach (ModuleInfo module in modules.OfType<ModuleInfo>())
                {
                    var hotels = Utils.HotelsInLocation(db, 1069);
                    foreach (var hotel in hotels)
                    {
                        var rule = new UrlRule
                        {
                            CultureCode = module.CultureCode,
                            TabId = module.TabID,
                            RuleType = UrlRuleType.Module,
                            Parameters = "id=" + hotel.Id.ToString(),
                            Action = UrlRuleAction.Rewrite,
                            Url = CleanupUrl(hotel.Name),
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