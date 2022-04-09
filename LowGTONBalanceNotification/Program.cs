using System.Numerics;
using Discord;
using Discord.WebSocket;
using Nethereum.Contracts;
using Nethereum.Web3;

namespace LowGTONBalanceAlert 
{
    internal delegate void Alert(BigInteger balance);
    class Program
    {
        public static void Main() 
        {
            Program.main();
        }
        public static void main() 
        {
            Alerts.LowBalance += Alerts_LowBalance;
            Task.Run(() => Alerts.LowBalanceWatcher());
            Thread.Sleep(Timeout.Infinite);
        }

        private static void Alerts_LowBalance(BigInteger balance)
        {
            Console.WriteLine(Web3.Convert.FromWei(balance, 18));
        }
    }
    class Alerts
    {
        public static event Alert LowBalance;
        public static async void LowBalanceWatcher() 
        {
            var GTONTokenAddress = "0xc1be9a4d5d45beeacae296a7bd5fadbfc14602c4";
            var sGTONTokenAddress = "0xb0daab4eb0c23affaa5c9943d6f361b51479ac48";

            var GTONABI = File.ReadAllText("GTONTokenABI.json");
            var sGTONABI = File.ReadAllText("sGTONTokenABI.json");

            var web3 = new Web3("https://rpc.ftm.tools");

            var GTON = new Token(web3, GTONTokenAddress, GTONABI);
            var sGTON = new Token(web3, sGTONTokenAddress, sGTONABI);

            while (true) 
            {
                var gtonBalance = await GTON.BalanceOf("0xb0daab4eb0c23affaa5c9943d6f361b51479ac48");
                var sgtonBalance = await sGTON.amountStaked();

                if (sgtonBalance - gtonBalance > (10000 ^ (10 ^ 18))) 
                {
                    LowBalance?.Invoke(sgtonBalance - gtonBalance);
                }
            }
        }
    }
    class Token 
    {
        public string Address { get; private set; }
        public string ABI { get; private set; }
        public Contract Contract { get; private set; }
        public Token(Web3 web3, string Address, string ABI) 
        {
            this.Address = Address;
            this.ABI = ABI;
            this.Contract = web3.Eth.GetContract(this.ABI, this.Address);
        }
        public async Task<BigInteger> BalanceOf(string address) 
        {
            var balanceOf = this.Contract.GetFunction("balanceOf");
            object[] parameters = new object[1] { address };
            var balance = await balanceOf.CallAsync<BigInteger>(parameters);

            return balance;
        }
        public async Task<BigInteger> amountStaked()
        {
            var balanceOf = this.Contract.GetFunction("amountStaked");
            var balance = await balanceOf.CallAsync<BigInteger>();

            return balance;
        }
    }
    class Discord 
    {
        private readonly DiscordSocketClient _client;
        private ISocketMessageChannel ChannelID;
        public Discord()
        {
            _client = new DiscordSocketClient();
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
        }

        public async Task Start()
        {
            var token = System.Environment.GetEnvironmentVariable("KEY");
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            await Task.Delay(Timeout.Infinite);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private Task ReadyAsync()
        {
            Console.WriteLine($"{_client.CurrentUser} is connected!");

            return Task.CompletedTask;
        }
        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.Id == _client.CurrentUser.Id)
                return;

            if (message.Content == "!register")
            {
                this.ChannelID = message.Channel;
                await this.ChannelID.SendMessageAsync("I will ping you when new post on forum will be published");
            }
        }
        public async void OnLowBalance(string balance)
        {
            if (this.ChannelID != null)
            {
                await this.ChannelID.SendMessageAsync($"Staking dont have enought tokens, current funds: {balance}");
            }
        }
    }
}
