using LineMessagingAPISDK;
using LineMessagingAPISDK.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using static MessageService.Controllers.PostNotifyData;

namespace MessageService.Controllers
{
    public class MessageController : ApiController
    {

        //static string client_id;// = "uLKPUAVeW2Lkq0jr0CNN6x";
        //static string secrete;// = "j5QBWyd8rtJhDMDW8ihxDSb47ze916UF9D88hkCdzal";
        //static string redirect_url;// = "http://localhost:63971/Message/Callback";
        //static string iii_uri;
        //static string key = "0988163835";
        //static string ivkey = "TaiPower";

        //static MessageController()
        //{

        //    if (client_id != null)
        //        return;
        //    ReloadSysParam();
        //}

        public MessageController()
        {

        }

        //public static void ReloadSysParam()
        //{
        //    try
        //    {
        //        using (var db = new TaiPower.Models.MessageServiceDBEntities())
        //        {
        //            var rec = db.tblSys.FirstOrDefault();
        //            client_id = rec.client_id;
        //            secrete = rec.secrete;
        //            redirect_url = rec.redirect_uri;
        //            iii_uri = rec.iii_uri;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    };

        //}

        [HttpGet]
        public string  Hello()
        {

            return "hello";
        }

        [HttpGet]
        public async Task< ResultMessage> NotifyDavid()
        {
          return await   this.Notify("David","David", "Ub35c30e7d6ba2ff8778f4925556fa2b0","hello from taipower chat bot");
        }
        [HttpGet]
        public async Task<ResultMessage> SMS(string phone, string bodyMsg)
        {

            if (TaiPower.Util.SendSMS(phone, bodyMsg))
            {
                await AddSMSLog(phone, bodyMsg, true);
                return new ResultMessage() { IsSuccess = true, Message = "" };
            }
            else
            {
                await AddSMSLog(phone, bodyMsg, false);
                return new ResultMessage() { IsSuccess = false, Message = "簡訊傳送失敗" };
            }
        }

        //[HttpGet]
        //public ResultMessage Reload()
        //{
        //    ReloadSysParam();
        //    return new ResultMessage() { IsSuccess = true, Message = "" };
        //}
        // GET: api/LineNofify
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

      

        [HttpGet]
        //Notify?userid=xxx&name=xxx&Token=xxx&msg=xxx

        public async Task<ResultMessage> Notify(string userid, string name, string token, string msg)
        {
            LineClient lineClient = new LineClient(ConfigurationManager.AppSettings["ChannelToken"].ToString());

            System.Collections.Generic.List<Message> list = new List<Message>();
            Message sndmsg = new LineMessagingAPISDK.Models.TextMessage(msg);
            list.Add(sndmsg);
            PushMessage pushmsg = new PushMessage()
            {
                To = token,
                Messages = list
            };
            try
            {
                await lineClient.PushAsync(pushmsg);// LineNotifySDK.Utility.SendNotification(token, msg);

                await AddNotifyLog(userid, name, token, msg, true);
                return new ResultMessage() { IsSuccess = true, Message = "" };
            }
            catch (Exception ex)
            {
               
                await AddNotifyLog(userid, name, token, msg, false,ex.Message);
                return new ResultMessage() { IsSuccess = false, Message = $"{ex.Message}" };

            }
            //if (result.status == "200")
            //{
            //    await AddNotifyLog(userid, name, token, msg, true);
            //    return new ResultMessage() { IsSuccess = true, Message = "" };
            //}
            //else
            //{
            //await AddNotifyLog(userid, name, token, msg, false);
            //    return new ResultMessage() { IsSuccess = true, Message = $"status{result.status} {result.message}" };
            ////}
        }

        [HttpPost]
        //Notify?userid=xxx&name=xxx&Token=xxx&msg=xxx

        public async Task<ResultMessage> PostNotify(/*string userid, string name,string token,string msg*/ [FromBody]PostNotifyData d)
        {
            LineClient lineClient = new LineClient(ConfigurationManager.AppSettings["ChannelToken"].ToString());
            System.Collections.Generic.List<Message> list = new List<Message>();
            Message sndmsg = new LineMessagingAPISDK.Models.TextMessage(d.msg);
            list.Add(sndmsg);
            PushMessage pushmsg = new PushMessage()
            {
                To = d.token,
                Messages = list
            };
            try
            {
                await lineClient.PushAsync(pushmsg);// LineNotifySDK.Utility.SendNotification(token, msg);

                await AddNotifyLog(d.userid, d.name, d.token, d.msg, true);
                return new ResultMessage() { IsSuccess = true, Message = "" };
            }
            catch(Exception ex)
           
            {
                await AddNotifyLog(d.userid, d.name, d.token, d.msg, false,ex.Message);
                return new ResultMessage() { IsSuccess = false, Message = $"{ex.Message}" };
            }
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Callback(HttpRequestMessage request)
        {
            if (!await VaridateSignature(request))
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            Activity activity=null;
            try
            {
                
                //Activity activity = JsonConvert.DeserializeObject<Activity>
                //    (await request.Content.ReadAsStringAsync());
                  activity = JsonConvert.DeserializeObject<Activity>
                   (await request.Content.ReadAsStringAsync());
            }
            catch(Exception ex)
            {

            }

            // Line may send multiple events in one message, so need to handle them all.
            foreach (Event lineEvent in activity.Events)
            {
                LineMessageHandler handler = new LineMessageHandler(lineEvent,this);

                Profile profile = await handler.GetProfile(lineEvent.Source.UserId);

                switch (lineEvent.Type)
                {
                    case EventType.Beacon:
                        await handler.HandleBeaconEvent();
                        break;
                    case EventType.Follow:
                        await handler.HandleFollowEvent();

                        break;
                    case EventType.Join:
                        await handler.HandleJoinEvent();
                        break;
                    case EventType.Leave:
                        await handler.HandleLeaveEvent();
                        break;
                    case EventType.Message:
                        Message message = JsonConvert.DeserializeObject<Message>(lineEvent.Message.ToString());
                        switch (message.Type)
                        {
                            case MessageType.Text:
                                Message w = JsonConvert.DeserializeObject<Message>(lineEvent.Message.ToString());
                                //if (Global.EventCount == 1)
                                //{

                                //    await handler.sayname();
                                //    break;
                                //}
                                await handler.HandleTextMessage();

                                break;
                            case MessageType.Audio:
                            case MessageType.Image:
                            case MessageType.Video:
                                await handler.HandleMediaMessage();
                                break;
                            case MessageType.Sticker:
                                await handler.HandleStickerMessage();
                                break;
                            case MessageType.Location:
                                await handler.HandleLocationMessage();
                                break;
                        }
                        break;
                    case EventType.Postback:
                        await handler.HandlePostbackEvent();
                        break;
                    case EventType.Unfollow:
                        await handler.HandleUnfollowEvent();
                        break;
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }


        [HttpGet]
         
        //SendPicture?userid=xxxx&name=xxx&token=xxx&imgurl=xxx

        public async Task<ResultMessage> SendPicture(string userid,string name ,string token, string imgurl)
        {
           
            LineClient lineClient = new LineClient(ConfigurationManager.AppSettings["ChannelToken"].ToString());

            System.Collections.Generic.List<Message> list = new List<Message>();
            Message sndmsg = new LineMessagingAPISDK.Models.ImageMessage(imgurl, imgurl);
            list.Add(sndmsg);
            PushMessage pushmsg = new PushMessage()
            {
                To = token,
                Messages = list
            };
            try
            {
                await lineClient.PushAsync(pushmsg);// LineNotifySDK.Utility.SendNotification(token, msg);

                await AddNotifyLog(userid, name, token, imgurl, true);
                return new ResultMessage() { IsSuccess = true, Message = "" };
            }
            catch (Exception ex)
            {

                await AddNotifyLog(userid, name, token, imgurl, false,ex.Message);
                return new ResultMessage() { IsSuccess = false, Message = $"{ex.Message}" };

            }
           
        }

        [HttpGet]
      //  SendTask? userid = xxxx & token = xxxx & name = xxx & msg = xxx & taskid = xxxx




        public async Task <ResultMessage>SendTask(string userid,string token, string name,string msg,string taskid)
        {
            LineClient lineClient = new LineClient(ConfigurationManager.AppSettings["ChannelToken"].ToString());

            System.Collections.Generic.List<Message> list = new List<Message>();
            List<TemplateAction> alist = new List<TemplateAction>();
            alist.Add(new PostbackTemplateAction("任務完成",taskid,"傳送中"));
            Message sndmsg = new LineMessagingAPISDK.Models.TemplateMessage(msg, new ButtonsTemplate(null,"TaiPower",msg,alist));
            list.Add(sndmsg);
            PushMessage pushmsg = new PushMessage()
            {
                To = token,
                Messages = list
            };
            try
            {
                await lineClient.PushAsync(pushmsg);// LineNotifySDK.Utility.SendNotification(token, msg);

                await AddNotifyLog(userid, "", token, msg, true);
                return new ResultMessage() { IsSuccess = true, Message = "" };
            }
            catch (Exception ex)
            {

                await AddNotifyLog(token, name, token, msg, false,ex.Message);
                return new ResultMessage() { IsSuccess = false, Message = $"{ex.Message}" };

            }
        }


        public HttpResponseMessage Html_Response(ResultMessage result)
        {
            string header = "<!DOCTYPE html>" +
            "<html>" +
            "<head>" +
                "<meta charset = \"utf-8\" />" +

                "<title></title>" +
             "</head>" +
             "<body>";
            string html = $"<font size=\"7\" color=\"{(result.IsSuccess ? "black" : "red")}\">{result.Message}</font>";
            var response = new HttpResponseMessage();
            response.Content = new StringContent(header + html + "</body></html>");
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }




        private async Task<bool> VaridateSignature(HttpRequestMessage request)
        {

            var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["ChannelSecret"].ToString()));
            var computeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(await request.Content.ReadAsStringAsync()));
            var contentHash = Convert.ToBase64String(computeHash);
            var headerHash = Request.Headers.GetValues("X-Line-Signature").First();

            return contentHash == headerHash;
        }

        public class LineMessageHandler
        {

            private Event lineEvent;
            private LineClient lineClient = new LineClient(ConfigurationManager.AppSettings["ChannelToken"].ToString());
            ApiController ctl;
            public LineMessageHandler(Event lineEvent,ApiController ctl)
            {
                this.lineEvent = lineEvent;
                this.ctl = ctl;
            }

            public async Task HandleBeaconEvent()
            {
            }

            public async Task HandleFollowEvent()
            {
                Message replyMessage = null;
              //  replyMessage = new TextMessage($"type:{lineEvent.Source.Type}\nuid:{lineEvent.Source.UserId}\nroomid:{lineEvent.Source.RoomId}\n groupid:{lineEvent.Source.GroupId}");
              
                if (lineEvent.Source.Type.ToString().ToLower() == "user")
                {
                    int res =await CheckLineUid(lineEvent.Source.UserId);
                    Message msg = null;
                    switch(res)
                    {
                        case -1:

                            replyMessage = new TextMessage("系統錯誤");
                            break;
                        case 0:
                             replyMessage=new TextMessage("您還沒完成註冊，請輸入驗證碼");
                            break;
                        case 1: //完成註冊
                            replyMessage = new TextMessage("歡迎回來");
                            break;
                    }
                }
                await Reply(replyMessage);

            }

            public async Task HandleJoinEvent()
            {
                Message replyMessage = null;
            
                //  Global.EventCount = 1;
               var res=await CheckLineGid(lineEvent.Source.GroupId);
                if (res != 1)
                {
                    replyMessage = new TextMessage($"本群組尚未完成註冊，請輸入驗證碼");
                }
                await Reply(replyMessage);
            }

            public async Task HandleLeaveEvent()
            {
                Message replyMessage = null;
                replyMessage = new TextMessage($"type:{lineEvent.Source.Type}\nuid:{lineEvent.Source.UserId}\nroomid:{lineEvent.Source.RoomId}\n groupid:{lineEvent.Source.GroupId}");
                //  Global.EventCount = 1;
                await Reply(replyMessage);
            }

            public async Task HandlePostbackEvent()
            {
                var res=await  TaskFinishReport(lineEvent.Source.UserId.ToString(), lineEvent.Postback.Data);
                var replyMessage = new TextMessage(res.Message);
                await Reply(replyMessage);
            }

            public async Task HandleUnfollowEvent()
            {
                Message replyMessage = null;
                //replyMessage = new TextMessage($"type:{lineEvent.Source.Type}\nuid:{lineEvent.Source.UserId}\nroomid:{lineEvent.Source.RoomId}\n groupid:{lineEvent.Source.GroupId}");
              
                //await Reply(replyMessage);
            }

            public async Task<Profile> GetProfile(string mid)
            {
                return await lineClient.GetProfile(mid);
            }
            //public async Task sayname()
            //{
            //    var textMessage = JsonConvert.DeserializeObject<TextMessage>(lineEvent.Message.ToString());
            //    Message replyMessage = null;
            //    Message message = JsonConvert.DeserializeObject<Message>(lineEvent.Message.ToString());
            //    replyMessage = new TextMessage("OK我記住啦");
            //    Global.IdName[Global.IdNamecount, 0] = message.Id;
            //    Global.IdName[Global.IdNamecount, 1] = textMessage.Text.ToLower();
            //    Global.EventCount = 0;
            //    await Reply(replyMessage);




            //}
            public async Task HandleTextMessage()
            {
                Message replyMessage = null;
                var textMessage = JsonConvert.DeserializeObject<TextMessage>(lineEvent.Message.ToString());

                if (lineEvent.Source.Type.ToString().ToLower() == "user")
                {


                    int res = await CheckLineUid(lineEvent.Source.UserId);

                    if (textMessage.Text.Trim() == "關於我")
                        replyMessage = new TextMessage($"lineuid:{lineEvent.Source.UserId}\n {(res == 0 ? "未驗證" : "已驗證")}");
                    else
                        if (res == 0) //未驗證
                    {
                        ResultMessage result = await UpdateToken(textMessage.Text.Trim(), lineEvent.Source.UserId);
                    
                        if (result.IsSuccess)
                            replyMessage = new TextMessage(string.IsNullOrEmpty(result.Message.Trim())?$"您已通過驗證":$"{result.Message}");
                        else
                            replyMessage = new TextMessage(string.IsNullOrEmpty(result.Message) ? "抱歉您未過驗證":$"抱歉您未過驗證,{result.Message}");
                    }
                        else
                        { //已驗證
                            if (textMessage.Text.Trim() == "測試傳圖片")
                            {
                                string imgurl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRGlIv7b-2xOKDwUzhCAJhygtDE9bz8C2qsRA&s";
                                using (var client = new WebClient())
                                {
                                    var ret = await client.DownloadStringTaskAsync(new Uri(ctl.Request.RequestUri.AbsoluteUri.Replace("/Callback", "") + $"/SendPicture?userid=testid&name=testname&token={lineEvent.Source.UserId}&imgurl={imgurl}"));
                                   // replyMessage = new TextMessage(ret);
                                }
                                //await   Reply(new ImageMessage("https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRGlIv7b-2xOKDwUzhCAJhygtDE9bz8C2qsRA&s",
                                //    "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRGlIv7b-2xOKDwUzhCAJhygtDE9bz8C2qsRA&s"));
                            }
                            else if(textMessage.Text.Trim() == "測試傳任務")
                            {
                            using (var client = new WebClient())
                            {
                             // SendTask? token = xxxx & name = xxx & msg = xxx & taskid = xxxx

                                var ret = await client.DownloadStringTaskAsync(new Uri(ctl.Request.RequestUri.AbsoluteUri.Replace("/Callback", "") + $"/SendTask?userid=testid&name=testname&token={lineEvent.Source.UserId}&msg=test task&taskid=123"));
                                //replyMessage = new TextMessage(ret);
                            }
                        }
                            else if (textMessage.Text.Trim() == "任務清單")
                            {
                                TaskInfo[] list =await GetUnfinishTaskList(lineEvent.Source.UserId.ToString());
                                foreach(var  task in list)
                                {
                               // LineClient lineClient = new LineClient(ConfigurationManager.AppSettings["ChannelToken"].ToString());

                                List<TemplateAction> alist = new List<TemplateAction>();
                                alist.Add(new PostbackTemplateAction("任務完成", task.taskid, "傳送中"));
                                Message sndmsg = new LineMessagingAPISDK.Models.TemplateMessage(task.msg, new ButtonsTemplate(null, "TaiPower", task.msg, alist));
                                replyMessage = sndmsg;
                                await Reply(replyMessage);
                               // var ret = await client.DownloadStringTaskAsync(new Uri(ctl.Request.RequestUri.AbsoluteUri.Replace("/Callback", "") + $"/SendTask?userid=testid&name=testname&token={lineEvent.Source.UserId}&msg=test task&taskid=123"));
                               }
                            return;
                            }
                        }



                    if (replyMessage != null)
                        await Reply(replyMessage);
                }
                else if(lineEvent.Source.Type.ToString().ToLower() == "group")
                {
                  

                    //  Global.EventCount = 1;
                    var res = await CheckLineGid(lineEvent.Source.GroupId);
                    if (res != 1)
                    {
                        var result = await UpdateGroupToken(textMessage.Text.Trim(), lineEvent.Source.UserId.ToLower(), lineEvent.Source.GroupId.ToString());
                        replyMessage = new TextMessage(result.Message+(result.IsSuccess?"":"\n請輸入驗證碼"));
                    }
                    else //已註冊
                    {

                    }




                    await Reply(replyMessage);
                }
            }

            public async Task HandleMediaMessage()
            {
                Message message = JsonConvert.DeserializeObject<Message>(lineEvent.Message.ToString());
                // Get media from Line server.
                Media media = await lineClient.GetContent(message.Id);
                Message replyMessage = null;

                // Reply Image 
                switch (message.Type)
                {
                    case MessageType.Image:
                    case MessageType.Video:
                    case MessageType.Audio:
                        replyMessage = new ImageMessage("https://github.com/apple-touch-icon.png", "https://github.com/apple-touch-icon.png");
                        break;
                }

                await Reply(replyMessage);
            }

            public async Task HandleStickerMessage()
            {
                //https://devdocs.line.me/files/sticker_list.pdf
                var stickerMessage = JsonConvert.DeserializeObject<StickerMessage>(lineEvent.Message.ToString());
                var replyMessage = new StickerMessage("1", "1");
                await Reply(replyMessage);
            }

            public async Task HandleLocationMessage()
            {
                var locationMessage = JsonConvert.DeserializeObject<LocationMessage>(lineEvent.Message.ToString());
                LocationMessage replyMessage = new LocationMessage(
                    locationMessage.Title,
                    locationMessage.Address,
                    locationMessage.Latitude,
                    locationMessage.Longitude);
                await Reply(replyMessage);
            }

            private async Task Reply(Message replyMessage)
            {
                try
                {
                    await lineClient.ReplyToActivityAsync(lineEvent.CreateReply(message: replyMessage));
                }
                catch
                {
                    await lineClient.PushAsync(lineEvent.CreatePush(message: replyMessage));
                }
            }

            public async Task<int> CheckLineUid(string lineuid)
            {
                try
                {
                    string iii_url = ConfigurationManager.AppSettings["IIIUrl"].ToString();
                    string validKey = ConfigurationManager.AppSettings["validKey"].ToString();
                    using (WebClient client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;

                        string urlstr = iii_url + $"/CheckLineUid?lineuid={lineuid}&validKey={validKey}";
                        var res = await client.DownloadStringTaskAsync(new Uri(urlstr));
                        return int.Parse(res);
                    }
                }
                catch (Exception ex)
                {
                    return -1;

                }
            }
            public async Task<int> CheckLineGid(string linegid)
            {
                try
                {
                    string iii_url = ConfigurationManager.AppSettings["IIIUrl"].ToString();
                    using (WebClient client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        string validKey = ConfigurationManager.AppSettings["validKey"].ToString();
                        string urlstr = iii_url + $"/CheckLineGid?linegid={linegid}&validKey={validKey}";
                        
                        var res = await client.DownloadStringTaskAsync(new Uri(urlstr));
                        return int.Parse(res);
                    }
                }
                catch (Exception ex)
                {
                    return -1;

                }
            }

            public async Task<ResultMessage>TaskFinishReport(string lineuid,  string taskid)
            {
                try
                {
                    string iii_url = ConfigurationManager.AppSettings["IIIUrl"].ToString();
                    string validKey = ConfigurationManager.AppSettings["validKey"].ToString();
                    using (WebClient client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;

                        string urlstr = iii_url + $"/TaskFinishReport?lineuid={lineuid}&taskid={taskid}&validKey={validKey}";
                        var res = await client.DownloadStringTaskAsync(new Uri(urlstr));
                        var ret = JsonConvert.DeserializeObject<ResultMessage>(res);
                        //using (var db = new TaiPower.Models.MessageServiceDBEntities())
                        //{
                        //    var rec = new TaiPower.Models.tblTokenUpdateLog() { timestamp = DateTime.Now, cmd = "update", userid = "", name = "", token = lineuid, is_success = ret.IsSuccess, error_message = ret.Message };
                        //    db.tblTokenUpdateLog.Add(rec);
                        //    db.SaveChanges();
                        //}
                        return ret;
                    }
                }
                catch (Exception ex)
                {
                    return new ResultMessage() { IsSuccess = false, Message = ex.Message };

                }
            }
            public async Task<ResultMessage> UpdateToken( string code, string lineuid)
            {

                try
                {
                    string iii_url = ConfigurationManager.AppSettings["IIIUrl"].ToString();
                    string validKey= ConfigurationManager.AppSettings["validKey"].ToString();
                    using (WebClient client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;

                        string urlstr = iii_url + $"/UpdateToken?code={code}&accessToken={lineuid}&validKey={validKey}";
                        var res = await client.DownloadStringTaskAsync(new Uri(urlstr));
                        var ret= JsonConvert.DeserializeObject<ResultMessage>(res);
                        using (var db = new TaiPower.Models.MessageServiceDBEntities())
                        {
                            var rec = new TaiPower.Models.tblTokenUpdateLog() { timestamp = DateTime.Now, cmd = "update", userid = "", name = "", token = lineuid, is_success = ret.IsSuccess, error_message = ret.Message };
                            db.tblTokenUpdateLog.Add(rec);
                            db.SaveChanges();
                        }
                        return ret;
                    }
                }
                catch (Exception ex)
                {
                    return new ResultMessage() { IsSuccess=false,Message=ex.Message};

                }
            
            }

            public async Task<ResultMessage> UpdateGroupToken(string code, string lineuid,string linegid)
            {

                try
                {
                    string iii_url = ConfigurationManager.AppSettings["IIIUrl"].ToString();
                    string validKey = ConfigurationManager.AppSettings["validKey"].ToString();
                    using (WebClient client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;

                        string urlstr = iii_url + $"/UpdateGroupToken?code={code}&lineuid={lineuid}&linegid={linegid}&validKey={validKey}";
                        var res = await client.DownloadStringTaskAsync(new Uri(urlstr));
                        var ret = JsonConvert.DeserializeObject<ResultMessage>(res);
                        using (var db = new TaiPower.Models.MessageServiceDBEntities())
                        {
                            var rec = new TaiPower.Models.tblTokenUpdateLog() { timestamp = DateTime.Now, cmd = "update", userid = "", name = "", token = lineuid, is_success = ret.IsSuccess, error_message = ret.Message };
                            db.tblTokenUpdateLog.Add(rec);
                            db.SaveChanges();
                        }
                        return ret;
                    }
                }
                catch (Exception ex)
                {
                    return new ResultMessage() { IsSuccess = false, Message = ex.Message };

                }

            }

            //GetUnfinishTaskList? lineuid

            public async Task<TaskInfo[]> GetUnfinishTaskList(string lineuid)
            {
                try
                {
                    string iii_url = ConfigurationManager.AppSettings["IIIUrl"].ToString();
                    string validKey = ConfigurationManager.AppSettings["validKey"].ToString();
                    using (WebClient client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;

                        string urlstr = iii_url + $"/GetUnfinishTaskList?lineuid={lineuid}&validKey={validKey}";
                        var res = await client.DownloadStringTaskAsync(new Uri(urlstr));
                        var ret = JsonConvert.DeserializeObject<TaskInfo[]>(res);
                        //using (var db = new TaiPower.Models.MessageServiceDBEntities())
                        //{
                        //    var rec = new TaiPower.Models.tblTokenUpdateLog() { timestamp = DateTime.Now, cmd = "update", userid = "", name = "", token = lineuid, is_success = ret.IsSuccess, error_message = ret.Message };
                        //    db.tblTokenUpdateLog.Add(rec);
                        //    db.SaveChanges();
                        //}
                        return ret;
                    }
                }
                catch (Exception ex)
                {
                    return null;

                }
            }

        }

        public static string aesEncryptBase64(string SourceStr, string CryptoKey, string ivkey)
        {
            string encrypt = "";

            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider();
            byte[] key = sha256.ComputeHash(Encoding.UTF8.GetBytes(CryptoKey));
            byte[] iv = md5.ComputeHash(Encoding.UTF8.GetBytes(ivkey));
            aes.Key = key;
            aes.IV = iv;

            byte[] dataByteArray = Encoding.UTF8.GetBytes(SourceStr);
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(dataByteArray, 0, dataByteArray.Length);
                cs.FlushFinalBlock();
                encrypt = Convert.ToBase64String(ms.ToArray());
            }


            return encrypt;
        }

        /// <summary>
        /// 字串解密(非對稱式)
        /// </summary>
        /// <param name="Source">解密前字串</param>
        /// <param name="CryptoKey">解密金鑰</param>
        /// <returns>解密後字串</returns>
        public static string aesDecryptBase64(string SourceStr, string CryptoKey, string ivkey)
        {
            string decrypt = "";

            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider();
            byte[] key = sha256.ComputeHash(Encoding.UTF8.GetBytes(CryptoKey));
            byte[] iv = md5.ComputeHash(Encoding.UTF8.GetBytes(ivkey));
            aes.Key = key;
            aes.IV = iv;

            byte[] dataByteArray = Convert.FromBase64String(SourceStr);
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(dataByteArray, 0, dataByteArray.Length);
                    cs.FlushFinalBlock();
                    decrypt = Encoding.UTF8.GetString(ms.ToArray());
                }
            }

            return decrypt;
        }

        async System.Threading.Tasks.Task AddNotifyLog(string userid, string name, string token, string message, bool is_successs,string err_msg=null)
        {

            if (message.Length > 500)
                message = message.Substring(0, 500);
            using (var db = new TaiPower.Models.MessageServiceDBEntities())
            {
                TaiPower.Models.tblMessageLog log = new TaiPower.Models.tblMessageLog()
                {
                    timestamp = DateTime.Now,
                    is_success = is_successs,
                    message = message,
                    name = name,
                    token = token,
                    type = "LINE",
                    err_msg=err_msg,
                    userid_tel = userid
                };
                db.tblMessageLog.Add(log);
                await db.SaveChangesAsync();
            }
        }

        async System.Threading.Tasks.Task AddSMSLog(string phone, string message, bool is_successs)
        {

            if (message.Length > 500)
                message = message.Substring(0, 500);
            using (var db = new TaiPower.Models.MessageServiceDBEntities())
            {
                TaiPower.Models.tblSMSLog log = new TaiPower.Models.tblSMSLog()
                {
                    timestamp = DateTime.Now,
                    is_success = is_successs,
                    message = message,
                    phone_no = phone
                };
                db.tblSMSLog.Add(log);
                await db.SaveChangesAsync();
            }
        }

        ///   void AddSMSLog()

     

    }


    public class ResultMessage
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
    public class TaskInfo
    {
        public string taskid { get; set; }
        public string msg { get; set; }

    };
  
    public class PostNotifyData
    {
        public string userid { get; set; }
        public string name { get; set; }
        public string token { get; set; }

        public string msg { get; set; }


      
    }

}
