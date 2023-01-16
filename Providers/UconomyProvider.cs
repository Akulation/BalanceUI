using System;
using System.Reflection;
using fr34kyn01535.Uconomy;
using Rocket.Unturned.Player;
using SDG.Unturned;
using BalanceUI;

namespace BalanceUI.Providers
{
    public interface ICurrencyProvider : IDisposable
    {
        decimal GetBalance(string id);
        void Init();
    }

    public class UconomyCurrencyProvider : ICurrencyProvider
    {
        public delegate void BalanceUpdated(UnturnedPlayer player, decimal amt);

        public void Init()
        {
            Uconomy.Instance.OnBalanceUpdate += OnBalanceUpdate;
        }

        public decimal GetBalance(string id)
        {
            var bal = Uconomy.Instance.Database.GetBalance(id);
            return bal;
        }

        public void Dispose()
        {
            Uconomy.Instance.OnBalanceUpdate -= OnBalanceUpdate;
        }

        private void OnBalanceUpdate(UnturnedPlayer player, decimal amt)
        {
            if (BalanceUI.Instance.Configuration.Instance.UseUconomy)
            {
                decimal bal = GetBalance(player.CSteamID.ToString());
                EffectManager.sendUIEffectText(3174, player.Player.channel.owner.transportConnection, true, "BalanceUI_Text", BalanceUI.Instance.Configuration.Instance.BalancePrefix + bal.ToString() + BalanceUI.Instance.Configuration.Instance.BalanceSuffix);
            }
        }
    }
}
