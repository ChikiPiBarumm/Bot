using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace F1Bot
{
    public class tgbot
    {
        static TelegramBotClient botClient = new TelegramBotClient("6161845555:AAE2tcDmeCii6reMCKvG0EFlj4Zbuthseaw");
        private static string url = "https://localhost:7035/api/";
        static HttpClient client = new HttpClient();

        

        
        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message)
            {
                OnMessageHandler(botClient, update);
            }
            
            if(update.Type == UpdateType.CallbackQuery)
            {
                OnCallbackQueryHandler(botClient, update);
            }

        }

        public static async void OnCallbackQueryHandler(ITelegramBotClient botClient, Update update)
        {
            /*
            var User = await GetUserByChatId(update.CallbackQuery.From.Id.ToString());
            */
            var ChatId = update.CallbackQuery.From.Id.ToString();
            var Action = update.CallbackQuery.Data.Split(".")[0];
            var Year = update.CallbackQuery.Data.Split(".")[1];
            var Round = update.CallbackQuery.Data.Split(".")[2];

            if (Action == "SELECT_SEASON_FOR_SCHEDULE")
            {
                var currentSeason = await MakeGetRequest<List<RaceScheduleEntry>>("Ergast/race-schedule/"+Year);
                var Message = "Розклад гонок поточного сезону \n"+ " \n";
                foreach (var race in currentSeason)
                {
                    Message += $"*{race.RaceName}*" + " - " + race.Date + " \n" +
                               "---------------------------------------" + "\n";

                }

                
                
                
                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                InlineKeyboardButton button = new InlineKeyboardButton("lsadk")
                    { CallbackData = $"MORE_INFO_BUTTON.{Year}.{Round}", Text = "Додаткова інформація про гонку" };
                InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button };
                list.Add(row);
                
                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(ChatId, Message, replyMarkup: inline, parseMode: ParseMode.Markdown);
            }
            else if (Action == "MORE_INFO_BUTTON")
            {
                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                var specificSeason = await MakeGetRequest<List<RaceScheduleEntry>>("Ergast/race-schedule/" + Year);
                foreach (var race in specificSeason)
                {
                    InlineKeyboardButton button = InlineKeyboardButton.WithUrl(Convert.ToString(race.RaceName), race.Url);
                    InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button };
                    list.Add(row);
                }

                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(
                    chatId: ChatId,
                    text: "Оберіть гонку, щоб побачити додаткову інформацію про неї",
                    replyMarkup: inline
                );

                return;
            }
            else if (Action == "SHOW_ALL_SEASONS_FOR_SCHEDULE")
            {
                var seasons = await MakeGetRequest<List<int>>("Ergast/list-of-seasons");

                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                foreach (var season in seasons)
                {
                    InlineKeyboardButton button = new InlineKeyboardButton("lsadk")
                        { CallbackData = $"SPECIFIC_SEASON_SCHEDULE.{season}.{Round}", Text = Convert.ToString(season) };
                    InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button };
                    list.Add(row);

                }

                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(ChatId, "Оберіть рік, щоб побачити розклад гонок цього сезону", replyMarkup: inline);

                return;
            }
            else if (Action == "SHOW_ALL_SEASONS_FOR_RESULTS")
            {
                var seasons = await MakeGetRequest<List<int>>("Ergast/list-of-seasons");

                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                foreach (var season in seasons)
                {
                    InlineKeyboardButton button = new InlineKeyboardButton("lsadk")
                        { CallbackData = $"SELECT_SEASON_FOR_RESULTS.{season}.{Round}", Text = Convert.ToString(season) };
                    InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button };
                    list.Add(row);

                }

                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(ChatId, "Оберіть рік, щоб побачити розклад гонок цього сезону", replyMarkup: inline);

                return;
            }
            else if (Action == "SHOW_ALL_SEASONS_FOR_STANDINGS")
            {
                var seasons = await MakeGetRequest<List<int>>("Ergast/list-of-seasons");

                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                foreach (var season in seasons)
                {
                    InlineKeyboardButton button = new InlineKeyboardButton("lsadk")
                        { CallbackData = $"SELECT_SCOREBOARD_TYPE.{season}.last", Text = Convert.ToString(season) };
                    InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button };
                    list.Add(row);

                }

                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(ChatId, "Оберіть рік, для того щоб продовжити", replyMarkup: inline);

                return; 
            }
            else if (Action == "SELECT_SCOREBOARD_TYPE")
            {
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Водії", $"DRIVER_SCOREBOARD.{Year}.last"),
                        InlineKeyboardButton.WithCallbackData("Команди", $"CONSTRUCTOR_SCOREBOARD.{Year}.last")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Обидва", $"CURRENT_SCOREBOARD.{Year}.last") 
                    }
                });
                await botClient.SendTextMessageAsync(ChatId, "Виберіть, який саме рейтинг Вас цікавить:", replyMarkup: inlineKeyboard);
            }
            else if (Action == "SPECIFIC_SEASON_SCHEDULE")
            {
                var specificSeason = await MakeGetRequest<List<RaceScheduleEntry>>("Ergast/race-schedule/"+Year);
                var Message = $"Розклад гонок сезону {Year} року: \n"+ " \n";
                foreach (var race in specificSeason)
                {
                    Message += $"*{race.RaceName}*" + " - " + race.Date + " \n" +
                               "---------------------------------------" + "\n";
                }
                
                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                InlineKeyboardButton button = new InlineKeyboardButton("lsadk")
                    { CallbackData = $"MORE_INFO_BUTTON.{Year}.{Round}", Text = "Додаткова інформація про гонку" };
                InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button };
                list.Add(row);
                
                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(ChatId, Message, replyMarkup: inline, parseMode: ParseMode.Markdown);

                
            }
            else if (Action == "SELECT_SEASON_FOR_RESULTS")
            {
                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                var specificSeason = await MakeGetRequest<List<RaceScheduleEntry>>("Ergast/race-schedule/" + Year);
                foreach (var race in specificSeason)
                {
                    InlineKeyboardButton button = new InlineKeyboardButton("lsadk")
                        { CallbackData = $"SPECIFIC_RACE_RESULT.{Year}.{race.Round}", Text = Convert.ToString(race.RaceName) };
                    InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button };
                    list.Add(row);
                }

                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(ChatId, "Оберіть гонку, щоб побачити її результати",
                    replyMarkup: inline);

                return;
            }
            else if (Action=="SPECIFIC_RACE_RESULT")
            {
                var specificSeasonResult = await MakeGetRequest<List<ScoreboardEntry>>($"Ergast/race-result/{Year}/{Round}/results");
                var Message = $"Результати {Round} раунду сезону {Year} року: \n"+ " \n";
                foreach (var result in specificSeasonResult)
                {
                    Message += result.Position +" - "+ $"*{result.GivenName}*" + " "+ $"*{result.FamilyName}*" + " "+ $"_({result.ConstructorName})_" + " - " + result.Points + " \n";
                }
                
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Додаткова інформація", $"EXTRA_INFO_MENU.{Year}.{Round}"),
                    },
                    
                });
                await botClient.SendTextMessageAsync(ChatId, Message, replyMarkup: inlineKeyboard, parseMode: ParseMode.Markdown);
                
            }
            else if (Action == "EXTRA_INFO_MENU")
            {
                var specificCircuit = await MakeGetRequest<List<CircuitTable>>($"Ergast/circuits-info/{Year}/{Round}/circuits");
                var circuit = specificCircuit[0];
                
                var specificSeason = await MakeGetRequest<List<RaceScheduleEntry>>("Ergast/race-schedule/" + Year);
                var race = specificSeason[0];
                
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Driver Info", $"SELECT_DRIVER.{Year}.{Round}"),
                        InlineKeyboardButton.WithCallbackData("Constructor Info", $"SELECT_CONSTRUCTOR.{Year}.{Round}"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithUrl("Circuit Info", Convert.ToString(circuit.Url)),
                        InlineKeyboardButton.WithUrl("Race Info", Convert.ToString(race.Url)),
                    },
                    new []
                    {
                        /*InlineKeyboardButton.WithCallbackData("Lap Times", $"SHOW_LAP_TIMES.{Year}.{Round}"),*/
                        InlineKeyboardButton.WithCallbackData("Quali Info", $"SHOW_QUALI_RESULTS.{Year}.{Round}"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Scoreboard", $"CURRENT_SCOREBOARD.{Year}.{Round}"),
                    },
                    
                });
                await botClient.SendTextMessageAsync(ChatId, "Виберіть, про що саме хоче дізнатись детальніше", replyMarkup: inlineKeyboard);
            }
            else if (Action == "SELECT_DRIVER")
            {
                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                var specificDriver = await MakeGetRequest<List<DriverInfo>>($"Ergast/driver-info/{Year}/{Round}");
                foreach (var driver in specificDriver)
                {
                    InlineKeyboardButton button = InlineKeyboardButton.WithUrl(Convert.ToString(driver.GivenName)+" "+Convert.ToString(driver.FamilyName), driver.Url);
                    InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button };
                    list.Add(row);
                }

                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(
                    chatId: ChatId,
                    text: "Оберіть гонщика, щоб побачити додаткову інформацію про нього",
                    replyMarkup: inline
                );

                return;
            }
            else if (Action == "SELECT_CONSTRUCTOR")
            {
                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                var specificConstructor = await MakeGetRequest<List<ConstructorInfo>>($"Ergast/constructor-info/{Year}/{Round}");
                foreach (var constructor in specificConstructor)
                {
                    InlineKeyboardButton button = InlineKeyboardButton.WithUrl(Convert.ToString(constructor.Name), constructor.Url);
                    InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button };
                    list.Add(row);
                }

                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(
                    chatId: ChatId,
                    text: "Оберіть команду, щоб побачити додаткову інформацію про неї",
                    replyMarkup: inline
                );

                return;
            }
            else if (Action == "SHOW_LAP_TIMES")
            {
                
            }
            else if (Action == "SHOW_QUALI_RESULTS")
            {
                var qualiResult = await MakeGetRequest<List<QualifyingResultEntry>>($"Ergast/qualifying-results/{Year}/{Round}/qualifying");
                var Message = $"Результати кваліфікації {Round} раунду сезону {Year} року: \n"+ 
                              "-----------------------------------" + "\n";
                foreach (var result in qualiResult)
                {
                    Message += $"[{result.Position}]" + " - " + $"*{result.GivenName} {result.FamilyName}*" + " " +
                               $"(_{result.ConstructorName}_)" +
                               "\n" + "\n" +
                               "Q1" + "                    " + "Q2" + "                   " + "Q3" + "\n" +
                               result.Q1Time + "     " + result.Q2Time + "     " + result.Q3Time + "\n" +
                               "\n" +
                               "-----------------------------------"+"\n";
                }

                await botClient.SendTextMessageAsync(ChatId, Message, parseMode: ParseMode.Markdown);
            }
            else if (Action == "CURRENT_SCOREBOARD")
            {
                var currentScoreboardDrivers =
                   await MakeGetRequest<List<DriverStandingsEntry>>($"Ergast/driver-standings/{Year}/{Round}/driverStandings");
                var driversMessage = $"Таблиця очок гонщиків {Round} раунду сезону {Year} року: \n"+ " \n";
                foreach (var result in currentScoreboardDrivers)
                {
                    driversMessage += $"[{result.Position}]"+" - "+ $"*{result.GivenName} {result.FamilyName}*" + " "+ $"(_{result.ConstructorName}_)" + " - " + result.Points + " \n";
                }
                
                await botClient.SendTextMessageAsync(ChatId, driversMessage, parseMode:ParseMode.Markdown); 
                
                var currentScoreboardConstructors =
                    await MakeGetRequest<List<ConstructorStandingsEntry>>($"Ergast/constructor-standings/{Year}/{Round}/constructorStandings");
                var constructorsMessage = $"Таблиця очок команд {Round} раунду сезону {Year} року: \n"+ " \n";
                
                foreach (var result in currentScoreboardConstructors)
                {
                    constructorsMessage += $"[{result.Position}]"+" - "+ $"*{result.ConstructorName}*" + " "+ $"(_{result.Nationality}_)" + " - " + result.Points + " \n";
                }
                await botClient.SendTextMessageAsync(ChatId, constructorsMessage, parseMode: ParseMode.Markdown);

            }
            else if (Action == "DRIVER_SCOREBOARD")
            {
                var currentScoreboardDrivers =
                    await MakeGetRequest<List<DriverStandingsEntry>>($"Ergast/driver-standings/{Year}/last/driverStandings");
                var driversMessage = $"Таблиця очок гонщиків сезону {Year} року: \n"+ " \n";
                foreach (var result in currentScoreboardDrivers)
                {
                    driversMessage += $"[{result.Position}]"+" - "+ $"*{result.GivenName} {result.FamilyName}*" + " "+ $"(_{result.ConstructorName}_)" + " - " + result.Points + " \n";
                }
                
                await botClient.SendTextMessageAsync(ChatId, driversMessage, parseMode: ParseMode.Markdown);
            }
            else if (Action == "CONSTRUCTOR_SCOREBOARD")
            {
                var currentScoreboardConstructors =
                    await MakeGetRequest<List<ConstructorStandingsEntry>>($"Ergast/constructor-standings/{Year}/last/constructorStandings");
                var constructorsMessage = $"Таблиця очок команд сезону {Year} року: \n"+ " \n";
                
                foreach (var result in currentScoreboardConstructors)
                {
                    constructorsMessage += $"[{result.Position}]"+" - "+ $"*{result.ConstructorName}*" + " "+ $"(_{result.Nationality}_)" + " - " + result.Points + " \n";
                }
                await botClient.SendTextMessageAsync(ChatId, constructorsMessage, parseMode: ParseMode.Markdown);
            }
            else if (Action == "SET_DRIVER")
            {
                /*User.DriverId = Year;
                await MakePutRequest<User>(User.ChatId + "/users/update", User);
                botClient.SendTextMessageAsync(ChatId, "Вітаю! Ви додали свого улюбленого гонщика!");
                
                return;*/
            }
        }
        public static async void OnMessageHandler(ITelegramBotClient botClient, Update update)
        {
            var message = update.Message;
            if (message.Text.ToLower() == "/start")
            {
                await botClient.SendTextMessageAsync(message.Chat, "Приємно познайомитись, " + message.Chat.FirstName + " " + message.Chat.LastName + "!");
                SendCommandsStart(message.Chat);
                return;
            }

            else if (message.Text.ToLower() == "/info_bot")
            {
                await botClient.SendTextMessageAsync(message.Chat, "Вітаю, " + message.Chat.FirstName + " " + message.Chat.LastName + "!" + " " + "Відтепер я твій бот-помінчик, який буде тобі допомогати слідкувати та дізнаватися цікаву інформацію про Формулу 1!");
                SendCommandsInfo(message.Chat);
                return;
            }

            /*else if (message.Text.ToLower() == "/schedule")
            {
                var seasons = await MakeGetRequest<List<int>>("list-of-seasons");

                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                foreach (var season in seasons)
                {
                    InlineKeyboardButton button = new InlineKeyboardButton("lsadk")
                        { CallbackData = "SET_SEASON." + season, Text = Convert.ToString(season) };
                    InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button };
                    list.Add(row);

                }

                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(message.Chat, "Оберіть рік, щоб побачити розклад гонок", replyMarkup: inline);

                return;
            }*/

            else if (message.Text.ToLower()=="/schedule")
            {
                InlineKeyboardMarkup inlineKeyboard = await ChooseSeason("SCHEDULE");
                await botClient.SendTextMessageAsync(message.Chat, "Виберіть, який саме розклад Вас цікавить:", replyMarkup: inlineKeyboard);
            }
            else if (message.Text.ToLower() == "/results")
            {
                InlineKeyboardMarkup inlineKeyboard = await ChooseSeason("RESULTS");
                await botClient.SendTextMessageAsync(message.Chat,
                    "Виберіть, результати якого саме сезону Вас цікавлять:", replyMarkup: inlineKeyboard);
                /*var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Результати гонок", "CHOOSE_RESULT_TYPE.race_result"),
                        InlineKeyboardButton.WithCallbackData("Результати кваліфікаціі", "CHOOSE_RESULT_TYPE.quali_result")
                    },
                });
                await botClient.SendTextMessageAsync(message.Chat, "Виберіть, чого саме результати Вас цікавлять:", replyMarkup: inlineKeyboard);*/
            }
            else if (message.Text.ToLower() == "/standings")
            {
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Таблиця очок поточного сезону", $"CURRENT_SCOREBOARD.{DateTime.Now.Year.ToString()}.last"),
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Таблиці очок минулих сезонів", "SHOW_ALL_SEASONS_FOR_STANDINGS.null.null") 
                    }
                });
                await botClient.SendTextMessageAsync(message.Chat, "Виберіть, який саме рейтинг Вас цікавить:", replyMarkup: inlineKeyboard);
            }
            else if (message.Text.ToLower() == "/notification_bot" || message.Text.ToLower()== "/favourite")
            {
                await botClient
                    .SendTextMessageAsync(
                        message.Chat,
                        "WORK IN PROGRESS \n" +
                        " \n" +
                        "На жаль, зараз ця функція знаходиться на стадії розробки( \n" + 
                        "Вже скоро я зможу, надсилати Вам новини там нагадування, згідно з ваших налаштувань та бажаємих гонщиків або команд. \n" +
                        " \n" +
                        "Повертаю Вас до початкового меню...");
                SendCommandsStart(message.Chat);
            }
            else if (message.Text.ToLower()== "/back")
            {
                SendCommandsInfo(message.Chat);
            }
            /*else if (message.Text.ToLower() == "/favourite")
            {
                var drivers = await MakeGetRequest<List<DriverInfo>>($"Ergast/driver-info/current/last");
                
                List<InlineKeyboardButton[]> list = new List<InlineKeyboardButton[]>();
                foreach (var driver in drivers)
                {
                    InlineKeyboardButton button = new InlineKeyboardButton(driver.GivenName) { CallbackData = "SET_DRIVER."+ driver.DriverId, Text = $"{driver.GivenName} {driver.FamilyName}" };
                    InlineKeyboardButton[] row = new InlineKeyboardButton[1] { button }; 
                    list.Add(row);
                    
                }
                
                var inline = new InlineKeyboardMarkup(list);
                await botClient.SendTextMessageAsync(message.Chat, "Оберіть гонщика, щоб додати його як улюбленого", replyMarkup: inline);
                
                return;
            }*/
            


        }

        public static async Task<T> MakeGetRequest<T>(string apiUrl)
        {
            Console.WriteLine(url + apiUrl);
            HttpResponseMessage response = await client.GetAsync(url + apiUrl);


            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response);
                throw new Exception("Api get error");
            }

            
            string jsonResponse = await response.Content.ReadAsStringAsync();
            T DeserializedObject = JsonConvert.DeserializeObject<T>(jsonResponse);

            return DeserializedObject;
        }
        
        public static async Task<T> MakePostRequest<T>(string apiUrl, dynamic body = null)
        {
            var jsonRequest = "";
            if (body != null)
            {
                jsonRequest = JsonConvert.SerializeObject(body);
            }
            
            HttpContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url + apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response);
                throw new Exception("Api post error");
            }
            
            string jsonResponse = await response.Content.ReadAsStringAsync();
            T DeserializedObject = JsonConvert.DeserializeObject<T>(jsonResponse);

            return DeserializedObject;
        }
        
        public static async Task<T> MakePutRequest<T>(string apiUrl, dynamic body = null)
        {
            var jsonRequest = "";
            if (body != null)
            {
                jsonRequest = JsonConvert.SerializeObject(body);
            }
            HttpContent content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PutAsync(url + apiUrl, content);
            

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response);
                throw new Exception("Api put error");
            }
            
            string jsonResponse = await response.Content.ReadAsStringAsync();
            T DeserializedObject = JsonConvert.DeserializeObject<T>(jsonResponse);

            return DeserializedObject;
        }

        public static async Task<InlineKeyboardMarkup> ChooseSeason(string type)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("Поточний сезон", $"SELECT_SEASON_FOR_{type}.{DateTime.Now.Year.ToString()}.null"),
                    InlineKeyboardButton.WithCallbackData("Попередні сезони", $"SHOW_ALL_SEASONS_FOR_{type}.null.null")
                },
                /*new[]
                {
                    InlineKeyboardButton.WithCallbackData("Найближча гонка", "SELECT_SEASON.next") 
                }*/
            });
            return inlineKeyboard;
        }
        
        public static async Task<User> GetUserByChatId(string chatId)
        {
            var UserExist = await MakeGetRequest<string>($"Users/api/{chatId}/users/check");

            if (UserExist == "true")
            {
                var ExistUser = await MakeGetRequest<User>($"Users/api/{chatId}/users");
                return ExistUser;
            }
            
            var user = await MakePostRequest<User>("Users/api/register",new RegisterUser(chatId));

            return user;
        }

        public static async void SendCommandsStart(Chat Chat)
        {
            await botClient
                .SendTextMessageAsync(
                    Chat,
                    "/start - Розпочати роботу\n " +
                    "/favourite - Додати улюбленого гонщика  \n " +
                    "/info_bot - Режим бота для отримання інформації про F1 \n " +
                    "/notification_bot - Режим бота для слідкування за новинами");
        }

        public static async void SendCommandsInfo(Chat Chat)
        {
            await botClient
                .SendTextMessageAsync(
                    Chat,
                    "/back - Повернутися на головну\n " +
                    "/schedule - Розклад гонок у сезоні \n " +
                    "/results - Результати гонок \n " +
                    "/standings - Рейтингові таблиці з очками");
        }


        static void Main(string[] args)
        {
            Console.WriteLine("Запущен бот " + botClient.GetMeAsync().Result.FirstName);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.ReadLine();
        }
    }
    public class User
    {
        public Guid Id { get; set; }
        public string ChatId { get; set; }
        public string DriverId { get; set; }
    }
    public class RaceScheduleEntry
    {
        public int Round { get; set; }
        public string RaceName { get; set; }
        public string CircuitName { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Url { get; set; }
    }
    
    public class ScoreboardEntry
    {
        public string RaceName { get; set; }
        public string CircuitName { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string ConstructorName { get; set; }
        public string Position { get; set; }
        public string Points { get; set; }
    }
    
    public class DriverStandingsEntry
    {
        public string Position { get; set; }
        public string Points { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string Nationality { get; set; }
        public string ConstructorName { get; set; }
    }
    
    public class ConstructorStandingsEntry
    {
        public string Position { get; set; }
        public string Points { get; set; }
        public string ConstructorName { get; set; }
        public string Nationality { get; set; }
    }
    
    public class Circuit
    {
        public string CircuitId { get; set; }
        public string Url { get; set; }
        public string CircuitName { get; set; }
        public Location Location { get; set; }
    }
    
    public class CircuitTable
    {
        public List<Circuit> Circuits { get; set; }
        public string Url { get; set; }
    }
    
    public class DriverInfo
    {
        public string DriverId { get; set; }
        public string Url { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string Nationality { get; set; }
    }
    
    public class ConstructorInfo
    {
        public string ConstructorId { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public string Nationality { get; set; }
    }
    public class QualifyingResultEntry
    {
        public string RaceName { get; set; }
        public string CircuitName { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string ConstructorName { get; set; }
        public string Position { get; set; }
        public string Q1Time { get; set; }
        public string Q2Time { get; set; }
        public string Q3Time { get; set; }
    }
    public class RegisterUser
    {
        public string ChatId { get; set; }
        
        public RegisterUser(string ChatId)
        {
            this.ChatId = ChatId;
        }
    }
}