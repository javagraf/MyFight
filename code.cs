using ApiVK;
using ApiVK.Models;
using HPS.Api.VkApi.Models.Photos;
using HPS.Api.VkApi.Models.Photos.SaveWP;
using HPS.Api.VkApi.Models.Poll;
using HPS.Api.VkApi.Models.Wall;
using HPS.Models;
using HPS.Models.TableModels.DeckPost;
using HPS.Services.ApiService;
using HPS.Services.LoggerManager;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static HPS.Models.Enums;

namespace HPS.Jobs
{
    public class DeckPosterJob : IJob
    {
        public DeckPosterJob(IConfiguration config, ILoggerManager logger, ApplicationContext db, VKApiService vKApiService)
        {
            DB = db;
            Logger = logger;
            VKApiService = vKApiService;
            AppConfiguration = config;
        }
        private IConfiguration AppConfiguration { get; set; }
        private ILoggerManager Logger { get; set; }
        private ApplicationContext DB { get; set; }
        private VKApiService VKApiService { get; set; }

        private int _parsedCount = 0;

        private const string USER_ACCESS_TOKEN = "d00f9351271a991fadd07892331e2ec3d5df4d83d11c79204829a4d610150b41a7ab92d1fd234c265bb5a";
        VkApi vkApiUser = new VkApi(USER_ACCESS_TOKEN);


        private string GetPollAttachment()
        {
            string[] answers = new string[5];
            answers[0] = "5";
            answers[1] = "4";
            answers[2] = "3";
            answers[3] = "2";
            answers[4] = "1";

            PollModel newPoll = vkApiUser.PollsCreatePoll<PollModel>($"Дайте Вашу оценку колоде !", true, false, answers,
                 canShare: true);
            string pollAttachment = $"poll{newPoll.response.Owner_id}_{newPoll.response.Id}";
            return pollAttachment;
        }

        public void DoDeckPost()
        {
            try
            {

                string title = "";
                string link = "";
                string deckCode = "";
                string imageLink = "";
                int playerClass = 0;
                string html = "";
                string source = "";
                bool isStandart = false;
                bool isShouldPost = false;
                bool isTimeForNextPostElapsed = false;

                List<HtmlNode> nodes = new List<HtmlNode>();
                DeckInfo lastDeck = DB.ParsedDecks.OrderByDescending(x => x.PostDate).FirstOrDefault();
                if (lastDeck != null)
                {
                    DateTime timeNowMinusHour = DateTime.Now.Subtract(new TimeSpan(1, 0, 0));//Вычитаем час

                    if (lastDeck.PostDate < timeNowMinusHour)
                    {
                        isTimeForNextPostElapsed = true;
                        if (lastDeck.IsStandart)
                        {
                            html = "https://hearthstone-decks.net/wild-decks/";
                            isStandart = false;
                        }
                        else
                        {
                            html = "https://hearthstone-decks.net/standard-decks/";
                            isStandart = true;
                        }
                    }
                    else
                    {
                        isTimeForNextPostElapsed = false;
                    }
                }
                else
                {
                    html = "https://hearthstone-decks.net/standard-decks/";
                    isStandart = true;
                    isTimeForNextPostElapsed = true;
                }
                //if (isTimeForNextPostElapsed && _parsedCount != 2)
                //{
                //    ParseDeckData(html, ref title, ref link, ref deckCode, ref imageLink, ref playerClass, ref nodes, ref isShouldPost, ref isStandart);
                //}

                if (isTimeForNextPostElapsed)
                {
                    ParseDeckData(html, ref title, ref link, ref deckCode, ref imageLink, ref playerClass, ref nodes, ref isShouldPost, ref isStandart);
                }
                //  ParseDeckData(html, ref title, ref link, ref deckCode, ref imageLink, ref playerClass, ref nodes, ref isShouldPost, ref isStandart);
                if (isShouldPost)
                {
                    string imgPath = AppConfiguration.GetSection("ImagesPath").GetValue<string>("DECK_IMG_DIR");
                    string imagPlayerClassesPath = AppConfiguration.GetSection("ImagesPath").GetValue<string>("PLAYER_CLASS_IMG");

                    string fileName = Guid.NewGuid() + ".png";
                    string path = Path.Combine(imgPath, fileName);

                    PlayerClass pClass = (PlayerClass)playerClass;
                    string imgPlayerClassPath = Path.Combine(imagPlayerClassesPath, pClass.ToString() + ".jpg");

                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(new Uri(imageLink), path);
                    }

                    int groupId = -168211459;

                    string deckAttachment = VKApiService.SaveWallPhoto(vkApiUser, path, groupId);

                    string playerClassImgAttachment = VKApiService.SaveWallPhoto(vkApiUser, imgPlayerClassPath, groupId);

                    string attachment = $"{deckAttachment},{playerClassImgAttachment}, {GetPollAttachment()}";



                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    string groupStringId = "@simhs";
                    string headingMode = isStandart ? "Колода стандартного режима ! 🌝" : "Колода вольного режима ! 🌚";
                    string modeHashTag = isStandart ? $"#standart{groupStringId}" : $"#wild{groupStringId}";
                    string classHashTag = $"#{Enum.GetName(typeof(PlayerClass), playerClass)}{groupStringId}";

                    //string classHashTag = isStandart ? $"#{pClass.ToString()}StandartDeck@simhs" : $"#{pClass.ToString()}WildDeck@simhs";
                    string message = $"{headingMode}{title} 📌 Код деки: {Environment.NewLine} {deckCode} " +
                        $"{Environment.NewLine}{Environment.NewLine} #Hearthstone{groupStringId} {modeHashTag} {classHashTag}";


                    WallPostModel postModel = vkApiUser.WallPost<dynamic>(groupId, true, message: message, attachments: attachment);
                    int id = postModel.Response;
                    string repostAtachment = $"wall-168211459_{id}";
                    string ACCESS_TOKEN = AppConfiguration.GetSection("VK_ACCESS_TOKENS").GetValue<string>("HPS_ACCESS_TOKEN");

                    vkApiUser.SendMessage<dynamic>(0, "", peer_id: 2000000002, access_token: ACCESS_TOKEN, attachment: repostAtachment);



                    DB.ParsedDecks.Add(new DeckInfo()
                    {
                        Link = link,
                        Source = html,
                        PostDate = DateTime.Now,
                        PlayerClassId = playerClass,
                        IsStandart = isStandart
                    });
                    DB.SaveChanges();



                }
                DeleteOutdateRow();
            }
            catch (System.Exception e)
            {
                Logger.LogError(e.Message);
                Logger.LogError(e.StackTrace);
                Logger.LogError(e.InnerException.ToString());
            }
        }

        public void DeleteOutdateRow()
        {
            DateTime timeNowMinusMonth = DateTime.Now.Subtract(new TimeSpan(31, 0, 0, 0));//Вычитаем месяц
            IEnumerable<DeckInfo> outDatedDecks = DB.ParsedDecks.Where(x => x.PostDate < timeNowMinusMonth);
            DB.ParsedDecks.RemoveRange(outDatedDecks);
        }

        public void ParseDeckData(string html, ref string title, ref string link, ref string deckCode, ref string imageLink, ref int playerClass, ref List<HtmlNode> nodes, ref bool isShouldPost, ref bool isStandart)
        {
            try
            {
                isShouldPost = false;
                //html = "https://hearthstone-decks.net/wild-decks/";

                HtmlWeb web = new HtmlWeb();

                var htmlDoc = web.Load(html);

                nodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='medium-content']").ToList();
                nodes.Reverse();
                for (int i = 0; i < nodes.Count; i++)
                {
                    title = nodes[i].SelectSingleNode($".//div[@class='entry-title']").InnerText;
                    link = nodes[i].SelectSingleNode($".//a").Attributes["href"].Value;

                    htmlDoc = web.Load(nodes[i].SelectSingleNode($".//a").Attributes["href"].Value);
                    try
                    {
                        deckCode = htmlDoc.DocumentNode.SelectSingleNode($".//input[@id='Code1']").Attributes["value"].Value;
                    }
                    catch
                    {
                        deckCode = htmlDoc.DocumentNode.SelectSingleNode($".//input[@class='deck-code']").Attributes["value"].Value;
                    }


                    imageLink = htmlDoc.DocumentNode.SelectSingleNode($".//div[@class='elementor-image']").SelectSingleNode($".//img").Attributes["src"].Value;

                    playerClass = 1;

                    foreach (PlayerClass playerC in Enum.GetValues(typeof(PlayerClass)))
                    {
                        if (title.Contains("Demon") && title.Contains("Hunter"))
                        {
                            playerClass = (int)PlayerClass.DemonHunter;
                            break;
                        }
                        else if (title.Contains(playerC.ToString()))
                        {
                            playerClass = (int)playerC;
                        }
                    }
                    string newLink = link;
                    DeckInfo checDeck = DB.ParsedDecks.FirstOrDefault(x => x.Link == newLink);
                    if (checDeck == null)
                    {
                        isShouldPost = true;
                        break;
                    }
                    //если в стандарте ничего, чекаем ваилд и наоборот
                    else if (nodes[i] == nodes.Last() && _parsedCount != 2)
                    {
                        if (html == "https://hearthstone-decks.net/wild-decks/")
                        {
                            _parsedCount++;
                            html = "https://hearthstone-decks.net/standard-decks/";
                            isStandart = true;
                            ParseDeckData(html, ref title, ref link, ref deckCode, ref imageLink, ref playerClass, ref nodes, ref isShouldPost, ref isStandart);
                        }
                        else
                        {
                            _parsedCount++;
                            html = "https://hearthstone-decks.net/wild-decks/";
                            isStandart = false;
                            ParseDeckData(html, ref title, ref link, ref deckCode, ref imageLink, ref playerClass, ref nodes, ref isShouldPost, ref isStandart);
                        }
                    }
                }
                DB.SaveChanges();
            }
            catch (System.Exception e)
            {
                Logger.LogError(e.Message);
                Logger.LogError(e.StackTrace);
                //Logger.LogError(e.InnerException.ToString());
            }
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(action: Exec);
        }

        private void Exec()
        {
            DeckPosterJob dpj = new DeckPosterJob(AppConfiguration, Logger, DB, VKApiService);
            dpj.DoDeckPost();
        }
    }
}
