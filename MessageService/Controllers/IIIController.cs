using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace MessageService.Controllers
{
    
    public class IIIController : ApiController
    {
       public static string uid = "";
        public static string gid = "";
        // GET: api/Register
        [HttpGet]
     public  int CheckLineUid(string lineuid) //檢查是否已註冊
        {
            return lineuid == uid?1:0;
            
        }
        [HttpGet]
        public string hello()
        {
            return "hello";
        }


        [HttpGet]
        public int CheckLineGid(string linegid) //檢查是否已註冊
        {
            
            return linegid == gid ? 1 : 0;
        
        }

        [HttpGet]
        //    UpdateToken? code = 驗證碼& accessToken = lineuid
        public ResultMessage UpdateToken(string code,string accessToken) //檢查是否已註冊
        {
            if(code=="1234")
            {
                uid = accessToken;
                  return new ResultMessage() {  IsSuccess=true, Message = "驗證成功" };
               // return new ResultMessage() { IsSuccess = true};
            }
            else
            //    return new ResultMessage() { IsSuccess = false };
           return new ResultMessage() { IsSuccess = false, Message = "驗證失敗" };


        }

        [HttpGet]
        //UpdateGroupToken?lineid=xxx&linegid=xxx&code=xxxxx
        public ResultMessage UpdateGroupToken(string code, string lineuid,string linegid) //檢查是否已註冊
        {
            if (code == "1234")
            {
                gid = linegid;
                return new ResultMessage() { IsSuccess = true,Message = "驗證成功" };
            }
            else
                return new ResultMessage() { IsSuccess = false, Message = "驗證失敗" };


        }

        [HttpGet]
        //TaskFinishReport?lineuid=xx&taskid=xxx
        public ResultMessage TaskFinishReport(string lineuid,string taskid)
        {
            return new ResultMessage() {IsSuccess=true,Message=$"{taskid} 已完成" };
        }

        [HttpGet]
        public TaskInfo[] GetUnfinishTaskList(string lineuid)
        {
            return new TaskInfo[] { new TaskInfo() { taskid="123", msg="測試任務1" },
            new TaskInfo() { taskid="124", msg="測試任務2" }
            };
        }

    }
}
