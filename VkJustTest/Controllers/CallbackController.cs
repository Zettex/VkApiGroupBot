using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using VkNet.Abstractions;
using VkNet.Model;
using VkNet.Model.GroupUpdate;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;
using VkNet.Utils;

namespace VkJustTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallbackController : ControllerBase
    {
        /// <summary>
        /// Конфигурация приложения
        /// </summary>
        private readonly IConfiguration _configuration;

        private readonly IVkApi _vkApi;

        private UsersState _usersState;

        private Dictionary<string, MessageKeyboard> keyboards = new Dictionary<string, MessageKeyboard>()
        {
            { "goodMan", new KeyboardBuilder().AddButton("Да", "goodMan").AddButton("Нет", "goodMan").Build() },
            { "respect", new KeyboardBuilder().AddButton("Да", "respect").AddButton("Нет", "respect").Build() },
        };
        
        public CallbackController(IVkApi vkApi, IConfiguration configuration, UsersState usersState)
        {
            _vkApi = vkApi;
            _configuration = configuration;
            _usersState = usersState;
        }

        [HttpPost]
        public IActionResult Callback([FromBody] Updates updates)
        {
            switch (updates.Type)
            {
                case "confirmation":
                    return Ok(_configuration["Config:Confirmation"]);
                case "message_new":
                    {
                        // Десериализация
                        var msg = Message.FromJson(new VkResponse(updates.Object));
                        var peerId = msg.PeerId;

                        if (msg.Text == "/log")
                        {
                            LogMessage(updates, msg);
                            break;
                        }

                        if (JoinGroupTest(peerId, msg, updates.GroupId))
                        {
                            break;
                        }

                        var user = _usersState.GetUser(peerId.Value);
                        if (user == null || user != null && !user.RequestToJoinGroptSent)
                        {
                            FirstTimeEnter(peerId, updates.GroupId);
                        }
                        else
                        {
                            SendMessage(peerId, "Если хочешь вступить в эту супер илитную групку, то отвечай на вопросы! " +
                                "И используй кнопки! 😠");
                            SendMessage(peerId, user.LastQuestion, user.LastKeyboard);
                        }

                        break;
                    }
                case "group_join":
                    {
                        var peerId = GroupJoin.FromJson(new VkResponse(updates.Object)).UserId;

                        FirstTimeEnter(peerId, updates.GroupId);

                        break;
                    }
            }

            return Ok("ok");
        }

        private bool JoinGroupTest(long? peerId, Message msg, long groupId)
        {
            if (!CheckRequestGroupJoin(peerId, groupId))
            {
                SendMessage(peerId, "Вы отменили заявку на вступление...");
                ClearUserState(peerId);
                return false;
            }

            VkPayload payload = null;
            try
            {
                // try to get data from buttons
                payload = JsonConvert.DeserializeObject<VkPayload>(msg.Payload);
            }
            catch { }
            
            if (payload?.Value != null)
            {
                var user = _usersState.GetUser(peerId.Value);
                bool isNatural = payload.Value == "goodMan";
                bool isRespect = payload.Value == "respect";
                var answer = msg.Text.ToLower() == "да" ? Answer.Yes : msg.Text.ToLower() == "нет" ? Answer.No : Answer.None;

                if (isNatural && answer == Answer.Yes)
                {
                    //if ()
                    //{
                    //    SendMessage(peerId, "Хмм...🤔🤔  Вот насчет тебя у меня сомнения... Ну да ладно...");
                    //}

                    SendMessage(peerId, user.LastQuestion = "Вы уважаете Администратора??", user.LastKeyboard = keyboards["respect"]);
                }
                else if (isNatural && answer == Answer.No)
                {
                    SendMessage(peerId, "Тогда уходи от сюда!");
                    ClearUserState(peerId);
                }

                if (isRespect && answer == Answer.Yes)
                {
                    var groupAdminId = long.Parse(_configuration["Config:GroupAdminId"]);
                    var userInfo = _vkApi.Users.Get(new long[] { peerId.Value }).FirstOrDefault();

                    SendMessage(peerId, "⭐Вы прошли опрос!⭐");
                    SendMessage(peerId, "Уведомление о том, что вы прошли опрос, будет отправлено Администратору. Ожидайте...");
                    _usersState.RemoveUser(peerId.Value);

                    SendMessage(groupAdminId, $"Пользователь @id{peerId} ({userInfo.FirstName} {userInfo.LastName}) прошел опрос." +
                        $"\r\nhttps://vk.com/club88781591?act=users&tab=requests");
                }
                else if (isRespect && answer == Answer.No)
                {
                    SendMessage(peerId, "Тогда уходи от сюда!");
                    ClearUserState(peerId);
                }

                return true;
            }

            return false;
        }

        private void FirstTimeEnter(long? peerId, long groupId)
        {
            var groupMember = _vkApi.Groups.IsMember(groupId.ToString(), peerId, null, true).FirstOrDefault();
            bool isMember = groupMember.Member;

            if (isMember)
            {
                SendMessage(peerId, "Вы итак уже в группе❗");
            }
            else if (CheckRequestGroupJoin(peerId, groupId))
            {
                _usersState.AddUser(peerId.Value);

                var user = _usersState.GetUser(peerId.Value);
                user.RequestToJoinGroptSent = true;

                SendMessage(peerId, "Хотите вступить в группу?");
                SendMessage(peerId, "Для этого нужно пройти опрос...");
                SendMessage(peerId, user.LastQuestion = "Итак... Вы хороший человек ?", user.LastKeyboard = keyboards["goodMan"]);
            }
            else
            {
                SendMessage(peerId, "Сначала подайте заявку на вступление в группу❗");
            }
        }

        private bool CheckRequestGroupJoin(long? peerId, long groupId)
        {
            return _vkApi.Groups.IsMember(groupId.ToString(), peerId, null, true).FirstOrDefault().Request == true;
        }

        private void ClearUserState(long? peerId)
        {
            _usersState.RemoveUser(peerId.Value);
        }

        private void SendMessage(long? userId, string msgText, MessageKeyboard keyboard = null)
        {
            if (keyboard == null)
                keyboard = new KeyboardBuilder().Clear().Build();

            _vkApi.Messages.Send(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = userId,
                Message = msgText,
                Keyboard = keyboard
            });
        }


        private async void LogMessage(Updates updates, Message msg)
        {
            await _vkApi.Messages.SendAsync(new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = msg.PeerId.Value,
                Message = JsonConvert.SerializeObject(updates) + ", " + JsonConvert.SerializeObject(msg)
            });
        }
    }


    //_vkApi.Messages.Send(new MessagesSendParams
    //                    {
    //                        RandomId = new DateTime().Millisecond,
    //                        PeerId = msg.FromId.Value,
    //                        Message = string.Join("", new Stack<char>(msg.Text))
    //                    });
}