using Rocket.API;

namespace BalanceUI
{
    public class Config : IRocketPluginConfiguration
    {
        public bool UseEXP { get; set; }
        public bool UseUconomy { get; set; }
        public bool UseItemCurrency { get; set; }
        public string ItemCurrencyGUID { get; set; }
        public string BalancePrefix { get; set; }
        public string BalanceSuffix { get; set; }
        public ushort UIEffectID { get; set; }
        public void LoadDefaults()
        {
            UseEXP = true;
            UseUconomy = false;
            UseItemCurrency = false;
            ItemCurrencyGUID = "5150ca8f765d4a68bfe54912146da410";
            BalancePrefix = "$";
            BalanceSuffix = "";
            UIEffectID = 16480;
        }
    }
}
