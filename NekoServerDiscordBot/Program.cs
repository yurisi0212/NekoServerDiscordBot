using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace NekoServerDiscordBot {
    class Program {

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        private TokenManager _token;
        private bool _get_now = false;

        static void Main() {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync() {
            _client = new DiscordSocketClient(new DiscordSocketConfig {
                LogLevel = LogSeverity.Info
            });
            _client.Log += Log;
            _commands = new CommandService();
            _services = new ServiceCollection().BuildServiceProvider();
            _client.MessageReceived += CommandRecieved;
            _token = new TokenManager();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _client.LoginAsync(TokenType.Bot, _token.DiscordToken);
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private async Task CommandRecieved(SocketMessage messageParam) {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            if (message.Author.IsBot) return;

            var context = new CommandContext(_client, message);
            var CommandContext = message.Content;

            if (CommandContext == "/online") {
                if (_get_now) {
                    await message.Channel.SendMessageAsync("取得中...");
                    return;
                }
                _get_now = true;
                var embed = getEmbed();
                await message.Channel.SendMessageAsync(embed: embed.Build());
                _get_now = false;
            }
        }

        private Task Log(LogMessage message) {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }

        private EmbedBuilder getEmbed() {
            var embed = new EmbedBuilder();
            embed.WithTitle("猫鯖状況");
            embed.WithDescription("");
            embed.WithColor(Color.Purple);
            embed.WithTimestamp(DateTime.Now);
            var json = "";
            using (var webClient = new System.Net.WebClient()) {
                webClient.Encoding = System.Text.Encoding.UTF8;
                json = webClient.DownloadString("https://api.mcsrvstat.us/2/203.137.54.66:19132").Replace("\n", "");

            }
            var jObject = JObject.Parse(json);
            if (!(bool)jObject["online"]) {
                embed.AddField("現在の状況", "オフライン", true);
                return embed;
            }

            var online = jObject["players"]["online"].ToString();
            var max = jObject["players"]["max"].ToString();
            var user = "";
            if(int.Parse(online) > 0) {
                foreach (var item in jObject["players"]["list"]) {
                    user += item + "\n";
                }
            }
            embed.AddField("現在の状況", "オンライン");
            embed.AddField("サーバー内人数", online + " / " + max);
            embed.AddField("ユーザー", user);
            return embed;
        }

    }
}