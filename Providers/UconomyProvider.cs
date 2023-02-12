using System;
using System.Reflection;
using fr34kyn01535.Uconomy;
using Rocket.Unturned.Player;
using SDG.Unturned;
using BalanceUI;
using System.Threading.Tasks;

namespace BalanceUI.Providers
{
    public interface ICurrencyProvider : IDisposable
    {
        Task<decimal> GetBalance(string id);
        void Init();
    }

    public class UconomyCurrencyProvider : ICurrencyProvider
    {
        public delegate void BalanceUpdated(UnturnedPlayer player, decimal amt);

        public void Init()
        {
            Uconomy.Instance.OnBalanceUpdate += OnBalanceUpdate;
        }

        public async Task<decimal> GetBalance(string id)
        {
            var bal = Task.Run(() => Uconomy.Instance.Database.GetBalance(id));
            var result = await bal;
            return result;
        }

        public void Dispose()
        {
            Uconomy.Instance.OnBalanceUpdate -= OnBalanceUpdate;
        }

        private async void OnBalanceUpdate(UnturnedPlayer player, decimal amt)
        {
            if (BalanceUI.Instance.Configuration.Instance.UseUconomy)
            {
                decimal bal = await GetBalance(player.CSteamID.ToString());
                EffectManager.sendUIEffectText(3174, player.Player.channel.owner.transportConnection, true, "BalanceUI_Text", BalanceUI.Instance.Configuration.Instance.BalancePrefix + bal.ToString() + BalanceUI.Instance.Configuration.Instance.BalanceSuffix);
            }
        }
    }
}
