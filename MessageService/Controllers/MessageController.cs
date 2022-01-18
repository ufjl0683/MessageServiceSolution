using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace MessageService.Controllers
{
    public class MessageController : ApiController
    {

        static string client_id;// = "uLKPUAVeW2Lkq0jr0CNN6x";
        static string secrete;// = "j5QBWyd8rtJhDMDW8ihxDSb47ze916UF9D88hkCdzal";
        static string redirect_url;// = "http://localhost:63971/Message/Callback";
        static string iii_uri;
        static string key = "0988163835";
        static string ivkey="TaiPower";

        static MessageController()
        {

            if (client_id != null)
                return;
            ReloadSysParam();
        }

      
        public   static  void  ReloadSysParam()
        {
            try
            {
                using (var db = new TaiPower.Models.MessageServiceDBEntities())
                {
                    var rec = db.tblSys.FirstOrDefault();
                    client_id = rec.client_id;
                    secrete = rec.secrete;
                    redirect_url = rec.redirect_uri;
                    iii_uri = rec.iii_uri;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            };

        }


        [HttpGet]
        public async Task< ResultMessage> SMS(string phone,string bodyMsg)
        {
             
            if(TaiPower.Util.SendSMS(phone, bodyMsg))
            {
                await  AddSMSLog(phone, bodyMsg, true);
                return new ResultMessage() { IsSuccess=true, Message="" };
            }
            else
            {
                await AddSMSLog(phone, bodyMsg, false);
                return new ResultMessage() { IsSuccess = false, Message = "簡訊傳送失敗" };
            }
        }

        [HttpGet]
        public ResultMessage Reload()
        {
            ReloadSysParam();
            return new ResultMessage() {  IsSuccess=true,Message=""};
        }
        // GET: api/LineNofify
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/LineNofify/5
        [HttpGet]
        //[Route("api/LineNotify/Token")]
        public string Token(string code)
        {
           var message=  LineNotifySDK.Utility.GetToeknFromCode(code, redirect_url, client_id, secrete);
            return message.access_token;
        }

        // POST: api/LineNofify

          //[Route("api/LineNotifiction/Notification")]

            [HttpGet]
    
        public async Task<ResultMessage> Notify(string userid,string name,string token,string msg)
        {
            var result = LineNotifySDK.Utility.SendNotification(token, msg);
            if (result.status == "200")
            {
               await AddNotifyLog(userid, name, token, msg, true);
                return new ResultMessage() { IsSuccess = true, Message = "" };
            }
            else
            {
               await AddNotifyLog(userid, name, token, msg, false);
                return new ResultMessage() { IsSuccess = true, Message = $"status{result.status} {result.message}" };
            }
        }

        [HttpPost]
        public async Task<ResultMessage> PostNotify(/*string userid, string name,string token,string msg*/ [FromBody]PostNotifyData d)
        {
            var result = LineNotifySDK.Utility.SendNotification(d.token, d.msg);
            if (result.status == "200")
            {
                await AddNotifyLog(d.userid, d.name, d.token, d.msg, true);
                return new ResultMessage() { IsSuccess = true, Message = "" };
            }
            else
            {
                await AddNotifyLog(d.userid, d.name, d.token, d.msg, false);
                return new ResultMessage() { IsSuccess = true, Message = $"status{result.status} {result.message}" };
            }
        }



        //[HttpGet]
        //public void Notification(string token,string message)
        //{
        //  var result=  LineNotifySDK.Utility.SendNotification(token, message);
        //    if(result.status=="401")
        //    {
        //        var msg = new HttpResponseMessage(HttpStatusCode.Unauthorized) { ReasonPhrase = result.message };
        //        throw new HttpResponseException(msg);
        //    }
        //  // var str= LineNotifySDK.Utility.GenerateHTMLString(clientid, redir_url);
        //}


        [HttpGet]
        
        public string RegisterUrl(string userid,string name)
        {

            LineStateData line_state_data = new LineStateData() { userid =userid, name=name };
            string s = Newtonsoft.Json.JsonConvert.SerializeObject(line_state_data);
             var state_enc= HttpUtility.UrlEncode( aesEncryptBase64(s,key,ivkey));
            return $"https://notify-bot.line.me/oauth/authorize?response_type=code&scope=notify&response_mode=form_post&client_id={client_id}&redirect_uri={redirect_url}&state={state_enc}";
        }

        [HttpPost]
        public async Task<ResultMessage> Callback([FromBody]Models.RegisterCallBackData value)
        {
            
            var dec_str =  aesDecryptBase64(value.state, key, ivkey);
            LineStateData lsd=null;
            try
            {
                lsd = Newtonsoft.Json.JsonConvert.DeserializeObject<LineStateData>(dec_str);
                if(lsd==null)
                {
                    throw new Exception("state 解密失敗");
                }
            }
            catch(Exception ex)
            {
                 return new ResultMessage() { IsSuccess = false, Message = "state 解密失敗" };
            }
                
            if(DateTime.Now.Subtract(lsd.timestamp)> TimeSpan.FromHours(24))
            {
                return new ResultMessage() { IsSuccess = false, Message = "url 綁定逾時" };
            }

            var token = Token(value.code);
            if (token == null)
                return new ResultMessage() { IsSuccess = false, Message = "連動失敗" };
            await UpdateToken(lsd.userid, lsd.name, token);

            return new ResultMessage() { IsSuccess=true,Message="連動成功" };
        }

   
       
        [HttpGet]
      
        public LineNotifySDK.Struct.GetStatusResponse Status(string token)
        {
            var result = LineNotifySDK.Utility.GetStatus(token);
            return result;
        }
        [HttpGet]
      
        public ResultMessage RevokeToken(string userid, string name, string token)
        {
            try
            {
                var res = LineNotifySDK.Utility.RevokeToken(token);
                if (res.status == "200")
                {
                    try
                    {
                        using (var db = new TaiPower.Models.MessageServiceDBEntities())
                        {
                            var rec = new TaiPower.Models.tblTokenUpdateLog() { timestamp = DateTime.Now, cmd = "revoke", userid = userid, name = name, token = token };
                            db.tblTokenUpdateLog.Add(rec);
                            db.SaveChanges();
                        }
                    }
                    catch {; }
                    return new ResultMessage() { IsSuccess = true, Message = "" };
                }
                else
                    return new ResultMessage() { IsSuccess = false, Message = $"status:{res.status} {res.message}" };


            }
            catch (Exception ex)
            {
                return new ResultMessage() { IsSuccess = false, Message = ex.Message };
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

        async System.Threading.Tasks.Task AddNotifyLog(string userid, string name, string token, string message, bool is_successs)
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
                    userid_tel = userid
                };
                db.tblMessageLog.Add(log);
                await db.SaveChangesAsync();
            }
        }

        async System.Threading.Tasks.Task AddSMSLog(string phone,  string message, bool is_successs)
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
                    phone_no=phone
                };
                db.tblSMSLog.Add(log);
                await db.SaveChangesAsync();
            }
        }

        ///   void AddSMSLog()

        public  async Task UpdateToken(string userid, string name, string token)
        {
            bool success = false;
            string errmsg="";
            try {
                WebClient client = new WebClient();
                client.Encoding = Encoding.UTF8;

                string urlstr = iii_uri+$"/updateToken?accessToken={token}&userid={userid}";
                 var res= await client.DownloadStringTaskAsync(new Uri(urlstr));
                IIIResult result = Newtonsoft.Json.JsonConvert.DeserializeObject<IIIResult>(res);
                success = (result.code == 200);
                if (!success)
                    errmsg = result.msg;
            }
            catch(Exception ex)
            {
                success = false;
                errmsg = ex.Message;

            }

            //call 資策會 here
            using (var db = new TaiPower.Models.MessageServiceDBEntities())
            {
                var rec = new TaiPower.Models.tblTokenUpdateLog() { timestamp = DateTime.Now, cmd = "update", userid = userid, name = name, token = token , is_success= success, error_message=errmsg};
                db.tblTokenUpdateLog.Add(rec);
                db.SaveChanges();
            }


        }

       

    }


    public class ResultMessage
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    public class LineStateData
    {
        public string userid { get; set;}
        public string name { get; set; }
        public DateTime timestamp { get; set; }
    }


    public  class IIIResult
    {
       public int code { get; set; }
        public string msg { get; set; }
    }

    public class PostNotifyData
    {
        public string userid { get; set; }
        public string name { get; set; }
        public string token { get; set; }

        public string msg { get; set; }
    }

}
