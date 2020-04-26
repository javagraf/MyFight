using ApiVK;
using ChatBot.Api.VkApi.Models.Messages;
using ChatBot.Models.TableModels;
using ChatBot.Services.LoggerManager;
using HPS.Api.VkApi.Models.Users;
using HPS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChatBot.Jobs.MostMsg.MostMsg
{
    [DisallowConcurrentExecution]
    public class MostMsgJob : IJob
    {
        public MostMsgJob(ILoggerManager logger, IConfiguration config,/* ApplicationContext context,*/ IServiceScopeFactory serviceScopeFactory)
        {
            ServiceScopeFactory = serviceScopeFactory;
            Logger = logger;
            AppConfiguration = config;
            // DB = context;
        }

        public IServiceScopeFactory ServiceScopeFactory;
        private ILoggerManager Logger { get; set; }
        private IConfiguration AppConfiguration { get; set; }
        private ApplicationContext DB { get; set; }

        private VkApi VkApi { get; set; }

        private void CalcMsg()
        {
            try
            {
                int chat_id = 2000000002;
                string ACCESS_TOKEN = AppConfiguration.GetSection("VK_ACCESS_TOKENS").GetValue<string>("SERGIO_ACCESS_TOKEN_KM");
              
                VkApi = new VkApi(ACCESS_TOKEN);

                using (var scope = ServiceScopeFactory.CreateScope())
                {
                    DB = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                    CalcMessage cm;
                    using (var transaction = DB.Database.BeginTransaction())
                    {
                        cm = DB.CalcMessages.FirstOrDefault();
                        if (cm == null)
                        {
                            CalcMessage calcMessage = new CalcMessage
                            {
                                MarkerMessageId = 0,
                                StartMessageId = 0,
                                Stage = "firstStart"
                            };
                            DB.CalcMessages.Add(calcMessage);
                            DB.SaveChanges();
                        }
                        cm = DB.CalcMessages.FirstOrDefault();
                        if (cm.Stage == "firstStart")
                        {
                            MessageHistory history = VkApi.GetMsgHistory<MessageHistory>(peer_id: chat_id, count: 1);
                            //Thread.Sleep(350);
                            List<Item> items = history.Response.Items;
                            cm.MarkerMessageId = items.First().Id;
                            cm.StartMessageId = items.First().Id;
                            cm.Stage = "calcToEnd";
                            DB.SaveChanges();
                        }
                        transaction.Commit();
                    }

                    if (cm.Stage == "calcToEnd")
                    {
                        while (true)
                        {
                            using (var transaction = DB.Database.BeginTransaction())
                            {
                                MessageHistory history = VkApi.GetMsgHistory<MessageHistory>(peer_id: chat_id, count: 200, startMessageId: cm.StartMessageId);
                                List<Item> items = history.Response.Items;
                                //маркер это конец для следующего подсчета
                                cm.StartMessageId = items.Last().Id;
                               /// DB.Entry(cm).Property("StartMessageId").IsModified = true;

                                if (items.Count == 1)
                                {
                                    cm.Stage = "firstCalcToMarker";
                                    DB.SaveChanges();
                                    transaction.Commit();
                                    break;
                                }

                                for (int i = 0; i < items.Count; i++)
                                {
                                   
                                    User checkUser = DB.Users.FirstOrDefault(x => x.Id == items[i].From_id);

                                    if (checkUser != null)
                                    {
                                        UserMessage userMessage = DB.UserMessages.FirstOrDefault(x => x.UserId == items[i].From_id);
                                        userMessage.Message++;
                                        DB.Entry(userMessage).Property("Message").IsModified = true;
                                        userMessage.LastMessageDate = items[i].Date;
                                        DB.Entry(userMessage).Property("LastMessageDate").IsModified = true;
                                    }
                                    else if (checkUser == null && items[i].From_id > 0)
                                    {
                                        UsersGetModel ugm = VkApi.UsersGet<UsersGetModel>(items[i].From_id);
                                        Thread.Sleep(350);
                                        string firstName = ugm.Response.Single().First_name;
                                        string lastName = ugm.Response.Single().Last_name;
                                        User newUser = new User
                                        {
                                            Id = items[i].From_id,
                                            FirstName = firstName,
                                            LastName = lastName,
                                            Activity = DateTime.Now
                                        };
                                        DB.Users.Add(newUser);

                                        UserMessage userMessage = new UserMessage
                                        {
                                            User = newUser,
                                            Message = 1
                                        };
                                        DB.UserMessages.Add(userMessage);
                                    }
                                    DB.SaveChanges();
                                }
                                transaction.Commit();
                            }
                        }
                    }

                    else if (cm.Stage == "firstCalcToMarker")
                    {

                        using (var transaction = DB.Database.BeginTransaction())
                        {
                            MessageHistory history = VkApi.GetMsgHistory<MessageHistory>(peer_id: chat_id, count: 200);
                            List<Item> items = history.Response.Items;

                            for (int i = 0; i < items.Count; i++)
                            {
                                if (items[i].Id == cm.MarkerMessageId)//если сообщений меньше 200 и мы уже дошли до маркера
                                {
                                    cm.MarkerMessageId = items.First().Id;
                                    cm.Stage = "firstCalcToMarker";//это излишне
                                    DB.SaveChanges();
                                    break;
                                }
                                else if (i == 199)//с продолжением
                                {
                                    cm.NewMarkerMessageId = items.First().Id;
                                    cm.Stage = "calcToMarker";
                                    cm.StartMessageId = items.Last().Id;
                                    DB.SaveChanges();
                                    break;
                                }
                                User checkUser = DB.Users.FirstOrDefault(x => x.Id == items[i].From_id);
                                if (checkUser != null)
                                {
                                    UserMessage userMessage = DB.UserMessages.FirstOrDefault(x => x.UserId == items[i].From_id);
                                    userMessage.Message++;
                                    userMessage.LastMessageDate = items[i].Date;
                                }
                                else if (checkUser == null && items[i].From_id > 0)
                                {
                                    UsersGetModel ugm = VkApi.UsersGet<UsersGetModel>(items[i].From_id);
                                    Thread.Sleep(350);
                                    string firstName = ugm.Response.Single().First_name;
                                    string lastName = ugm.Response.Single().Last_name;
                                    User newUser = new User
                                    {
                                        Id = items[i].From_id,
                                        FirstName = firstName,
                                        LastName = lastName,
                                        Activity = DateTime.Now
                                    };
                                    DB.Users.Add(newUser);

                                    UserMessage userMessage = new UserMessage
                                    {
                                        User = newUser,
                                        Message = 1
                                    };
                                    DB.UserMessages.Add(userMessage);
                                }
                                DB.SaveChanges();
                            }
                            transaction.Commit();
                        }
                    }

                    else if (cm.Stage == "calcToMarker")
                    {
                        bool isCalc = true;
                        while (isCalc)
                        {
                            using (var transaction = DB.Database.BeginTransaction())
                            {
                                MessageHistory history = VkApi.GetMsgHistory<MessageHistory>(peer_id: chat_id, count: 200, startMessageId: cm.StartMessageId);
                                List<Item> items = history.Response.Items;

                                cm.StartMessageId = items.Last().Id;
                                for (int i = 0; i < items.Count; i++)
                                {
                                    if (items[i].Id == cm.MarkerMessageId)
                                    {
                                        cm.Stage = "firstCalcToMarker";
                                        cm.MarkerMessageId = cm.NewMarkerMessageId;
                                        DB.SaveChanges();
                                        isCalc = false;
                                        break;
                                    }
                                    User checkUser = DB.Users.FirstOrDefault(x => x.Id == items[i].From_id);
                                    if (checkUser != null)
                                    {
                                        UserMessage userMessage = DB.UserMessages.FirstOrDefault(x => x.UserId == items[i].From_id);
                                        userMessage.Message++;
                                        DB.Entry(userMessage).Property("Message").IsModified = true;
                                        userMessage.LastMessageDate = items[i].Date;
                                        DB.Entry(userMessage).Property("LastMessageDate").IsModified = true;
                                    }
                                    else if (checkUser == null && items[i].From_id > 0)
                                    {
                                        UsersGetModel ugm = VkApi.UsersGet<UsersGetModel>(items[i].From_id);
                                        Thread.Sleep(350);
                                        string firstName = ugm.Response.Single().First_name;
                                        string lastName = ugm.Response.Single().Last_name;
                                        User newUser = new User
                                        {
                                            Id = items[i].From_id,
                                            FirstName = firstName,
                                            LastName = lastName,
                                            Activity = DateTime.Now
                                        };
                                        DB.Users.Add(newUser);

                                        UserMessage userMessage = new UserMessage
                                        {
                                            User = newUser,
                                            Message = 1
                                        };
                                        DB.UserMessages.Add(userMessage);
                                    }
                                    DB.SaveChanges();
                                }
                                transaction.Commit();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                string ANNOUNCER_ACCESS_TOKEN = AppConfiguration.GetSection("VK_ACCESS_TOKENS").GetValue<string>("ANNOUNCER_ACCESS_TOKEN");
                VkApi.SendMessage<dynamic>(chatId: 1, message: "ПРИВЕТ ! ПРИВЕТ ! ПРИВЕТ !", accessToken: ANNOUNCER_ACCESS_TOKEN);
                Logger.LogError(e.Message);
                Logger.LogError(e.StackTrace);
            }
        }

        public async Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.MergedJobDataMap;

            MostMsgJob mostMsgJob = new MostMsgJob(Logger, AppConfiguration, ServiceScopeFactory)
            {

            };
            Task task = new Task(mostMsgJob.CalcMsg);
            task.Start();
            await task;
        }
    }
}
