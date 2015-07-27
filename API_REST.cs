using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;
using System.Net;
using System.Web;
using System.Threading;
using System.Data.SqlClient;
using System.ComponentModel;
using ZOOM_REST_Web.funcClass;
using System.Transactions;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace ZOOM_REST_Web
{
    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    //支援JSONP
    [JavascriptCallbackBehavior(UrlParameterName = "callback")]

    public class API_REST
    {

        private const string timezone = "GMT+8:00";
        private const int duration = 60;

        public API_REST()
        {
            IncomingWebRequestContext req = WebOperationContext.Current.IncomingRequest;
            if (!Global.allows.Contains(req.Headers.Get("Referer"))) return;

            string reqsurl = req.UriTemplateMatch.RequestUri.AbsolutePath.Split(new char[] { '/' })[2];
            switch (reqsurl)
            {
                case "xxxddd":
                    Users.testStatus();
                    //return;
                    break;
                default:
                    break;
            }

        [WebInvoke(UriTemplate = "testStatus", Method = "*", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public string testStatus()
        {
            IncomingWebRequestContext req = WebOperationContext.Current.IncomingRequest;

            string sss = req.UriTemplateMatch.QueryParameters["fc"];
            switch (sss)
            {
                case "xxxddd":
                    Users.testStatus();
                    return "";
                //break;
                default:
                    break;
            }
            return "-";
        }


        //--- get functions -------------
        private String getFunc(string eid = "nobody", string i = "clientIP")
        {
            StringBuilder er = new StringBuilder();
            const string funcName = "getFunc";
            string ps = String.Format("eid:{0},IP:{1}", eid, i);
            string res = "Success";
            string msg = "";

            try
            {
                Global.init(1);
                using (Entities db = new Entities(Global.connStr))
                {
                    var eus = from e in db.EndUsers
                              where e.EneUserID == eid
                              select e;

                    if (eus.Count() != 1)
                    {
                        res = "Wrong";
                        msg = "EID 有誤";
                        er.AppendLine(msg);
                        Global.funcLog(funcName, ps, er.ToString(), res);
                        return Global.jss.Serialize(new { res, msg });
                    }
                    var eu = eus.First();
                    if (eu.IdentityTypeID == 999)
                    {
                        var fs = from f in db.Functions
                                 select f;
                        msg = "最大權限999，抓取完成";
                        er.AppendLine(msg);
                        Global.funcLog(funcName, ps, er.ToString(), res);
                        return Global.jss.Serialize(fs);
                    }
                    else
                    {
                        var fs = from f in db.Functions
                                 where f.UserType >= 1
                                 select new { f.FuncID, f.FuncName, f.UserType };
                        msg = "抓取完成";
                        er.AppendLine(msg);
                        Global.funcLog(funcName, ps, er.ToString(), res);
                        return Global.jss.Serialize(fs);
                    }
                }

            }
            catch (Exception ex)
            {
                res = "Error";
                msg = ex.Message;
                er.AppendLine(msg);
                Global.funcLog(funcName, ps, er.ToString(), res);
                return Global.jss.Serialize(new { res, msg });
            }
        }
    
        [Description("取得功能清單，成功的話回傳：{FuncID,FuncName,UserType,CreateDate}")]
        [OperationContract, WebInvoke(UriTemplate = "func?eid={eid}&i={i}", Method = "GET", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public String getFuncG(string eid = "nobody", string i = "clientIP")
        {
            return getFunc(eid, i);
        }
     
       
   
      
        private String addNewUser(string id, string pw, string nn, int tp, string misid = "misid", string phone = "phone", string mail = "mail", string gid = "", string eid = "nobody", string i = "clientIP")
        {
            StringBuilder er = new StringBuilder();
            const string funcName = "addNewUser";
            string ps = String.Format("id:{0},pw:{1},nn:{2},tp:{3},misid:{4},phone:{5},mail:{6},eid:{7},IP:{8}", id, pw, nn, tp, misid, phone, mail, eid, i);

            try
            {
                Global.init(1);
                using (Entities db = new Entities(Global.connStr))
                {
                    er.AppendLine("初始化！");
                    string apiParam, apiRes;
                    var oeus = from e in db.EndUsers
                               where e.euAccount == id
                               select e;
                    if (oeus.Count() > 0)
                    {
                        er.AppendLine("此ID已有人使用");
                        Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                        return Global.jss.Serialize("Error code:此ID已有人使用");
                    }
                       var type= Regex.IsMatch(mail,
              @"^(?("")("".+?""@)|(([0-9a-zA-Z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-zA-Z])@))" +
              @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,6}))$");
                       er.AppendLine(""+type);
                       Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                       
                    if(type==false)
                      {
                            er.AppendLine("錯誤信箱格式");
                            Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                            return Global.jss.Serialize("Error code:錯誤信箱格式");
                      }
                    if (db.Connection.State != System.Data.ConnectionState.Open) db.Connection.Open();
                    using (var tran = db.Connection.BeginTransaction())
                    {
                        try
                        {
                            EndUsers eu = new EndUsers()
                            {
                                EneUserID = Guid.NewGuid().ToString(),
                                euAccount = id,
                                euPassword = pw,
                                euNickname = nn,
                                CreateDate = DateTime.Now,
                                UpdateDate = DateTime.Now,
                                IdentityTypeID = tp,
                                MISID = misid,
                                phone = phone,
                                mail = mail
                            };
                            er.AppendLine("add new user data");
                            if (tp == 1)
                            {    //--- 一般會員，僅能參與會議（很少用到）
                                er.AppendLine("type=1");
                                var ect = from e in db.EndUsers
                                          select new { e.EneUserID };
                                //--- 先呼叫API新增ZOOM CUST USER 然後取得其ZOOMID後再新增EndUser資料庫
                                string css = String.Format("CS{0:yyyyMMdd}", DateTime.Now);
                                string mm = String.Format("{0:00000}@cablesoft.com.tw", ect.Count());
                                string csmail = css + mm;
                                
                                apiParam = String.Format("em={0}&fn={1}&ln={2}&tp={3}", csmail, nn, "Cablesoft",tp);
                                er.AppendLine(String.Format("apiParam={0}", apiParam));
                                using (WebClient zoomws = new WebClient() { Encoding = System.Text.Encoding.UTF8 })
                                {
                                    apiRes = zoomws.DownloadString(new Uri(Global.strWS + Global.createCustUser + apiParam));
                                }
                                apiRes = Global.getAPIJson(apiRes);
                                er.AppendLine(String.Format("apiRes={0}", apiRes));
                                //newUser nu = JsonConvert.DeserializeObject<newUser>(apiRes);
                                newUser nu = Global.jss.Deserialize<newUser>(apiRes);
                                eu.ZOOMID = nu.id;
                                er.AppendLine(String.Format("ZOOM ID={0}", nu.id));
                            }
                            else if (tp == 2)
                            {   //--- 可開會會員，一般都是用這個
                                er.AppendLine("type=1");
                                var ect = from e in db.EndUsers
                                          select new { e.EneUserID };
                                //--- 先呼叫API新增ZOOM CUST USER 然後取得其ZOOMID後再新增EndUser資料庫
                                string css = String.Format("CS{0:yyyyMMdd}", DateTime.Now);
                                string mm = String.Format("{0:00000}@cablesoft.com.tw", ect.Count());
                                string csmail = css + mm;

                                apiParam = String.Format("em={0}&fn={1}&ln={2}&tp={3}", csmail, nn, "Cablesoft", tp);
                                er.AppendLine(String.Format("apiParam={0}", apiParam));
                                using (WebClient zoomws = new WebClient() { Encoding = System.Text.Encoding.UTF8 })
                                {
                                    apiRes = zoomws.DownloadString(new Uri(Global.strWS + Global.createCustUser + apiParam));
                                }
                                apiRes = Global.getAPIJson(apiRes);
                                er.AppendLine(String.Format("apiRes={0}", apiRes));
                                //newUser nu = JsonConvert.DeserializeObject<newUser>(apiRes);
                                newUser nu = Global.jss.Deserialize<newUser>(apiRes);
                                eu.ZOOMID = nu.id;
                                er.AppendLine(String.Format("ZOOM ID={0}", nu.id));
                            }
                            else 
                            {
                                er.AppendLine("type=3");
                                var ect = from e in db.EndUsers
                                          select new { e.EneUserID };
                                //--- 先呼叫API新增ZOOM CUST USER 然後取得其ZOOMID後再新增EndUser資料庫
                                string css = String.Format("CS{0:yyyyMMdd}", DateTime.Now);
                                string mm = String.Format("{0:00000}@cablesoft.com.tw", ect.Count());
                                string csmail = css + mm;

                                apiParam = String.Format("em={0}&fn={1}&ln={2}&tp={3}", csmail, nn, "Cablesoft", tp);
                                er.AppendLine(String.Format("apiParam={0}", apiParam));
                                using (WebClient zoomws = new WebClient() { Encoding = System.Text.Encoding.UTF8 })
                                {
                                    apiRes = zoomws.DownloadString(new Uri(Global.strWS + Global.createCustUser + apiParam));
                                }
                                apiRes = Global.getAPIJson(apiRes);
                                er.AppendLine(String.Format("apiRes={0}", apiRes));
                                //newUser nu = JsonConvert.DeserializeObject<newUser>(apiRes);
                                newUser nu = Global.jss.Deserialize<newUser>(apiRes);
                                eu.ZOOMID = nu.id;
                                er.AppendLine(String.Format("ZOOM ID={0}", nu.id));
                            }
                            db.EndUsers.AddObject(eu);
                         
                            if (gid != null && gid.Length > 10)
                            {
                                er.AppendLine("準備建立清單");
                                HMsGMapping hm = new HMsGMapping()
                                {
                                    HMsGMappingID = Guid.NewGuid().ToString(),
                                    HMsGID = gid,
                                    MEID = eu.EneUserID,
                                    CreateDate = DateTime.Now
                                };
                                db.AddToHMsGMapping(hm);
                                er.AppendLine("對應清單建立成功");
                            }
                            db.SaveChanges();
                            tran.Commit();
                            er.AppendLine("SaveChanges\r\nEID=" + eu.EneUserID);
                            string title = "系統測試--0630 NBD System Testing";
                            string smail;
                            smail = "您好：您的帳號為:"+id+" 密碼為:"+pw+" 請確認帳號與密碼是否與您申請時無異 感謝您的註冊 NBD小組敬上同意";
                            cs.sendSMTP(mail,title,smail,ref er);
                            //var kk = new { eu.EneUserID, eu.euAccount, eu.euNickname, eu.IdentityTypeID, eu.ZOOMID, };
                            Global.funcLog(funcName, ps, er.ToString(), "Success:");
                            return Global.jss.Serialize(eu.EneUserID);
                        }
                        catch (Exception tex)
                        {
                            tran.Rollback();
                            er.AppendLine("交易失敗！" + tex.Message);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.funcLog(funcName, ps, er.AppendLine(ex.Message).ToString(), "Error");
                return Global.jss.Serialize(String.Format("Error code : 0x84177282005\r\n\r\n{0}", ex.Message));
            }
        }
    
        [Description("新增會員，id：登入帳號，pw：登入密碼，nn：暱稱，tp：身份權限(1=Basic,2=Pro,3=Corp)ID，misid：只有付費帳號才需要（可不給），gid：要特別指定群組時才給就好（可不給），成功的話回傳：EneUserID")]
        [OperationContract, WebInvoke(UriTemplate = "addNewUser?id={id}&pw={pw}&nn={nn}&tp={tp}&misid={misid}&phone={phone}&mail={mail}&gid={gid}&eid={eid}&i={i}", Method = "GET", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public String addNewUserG(string id, string pw, string nn, int tp, string misid = "misid", string phone = "phone", string mail = "mail", string gid = "", string eid = "nobody", string i = "clientIP")
        {
            return addNewUser(id, pw, nn, tp, misid, phone, mail, gid, eid, i);
        }
        //----------------------------
       
        private String listEndUser(string eid = "nobody", string i = "clientIP")
        {
            StringBuilder er = new StringBuilder();
            const string funcName = "listEndUser";
            string ps = String.Format("eid:{0},IP:{1}", eid, i);
            try
            {
                Global.init(1);
                using (Entities db = new Entities(Global.connStr))
                {
                    er.AppendLine("初始化！");
                    if (eid != "nobody")
                    {
                        var es = from e in db.EndUsers
                                 select new { e.EneUserID, e.euAccount, e.euNickname, e.IdentityTypeID, e.ZOOMID, e.mail, e.phone, e.MISID };
                        er.AppendLine("取得全部會員資料！");
                        Global.funcLog(funcName, ps, er.ToString(), "Success");
                        return Global.jss.Serialize(es);
                    }
                    else
                    {
                        var es = from e in db.EndUsers
                                 where e.EneUserID == eid
                                 select new { e.EneUserID, e.euAccount, e.euNickname, e.IdentityTypeID, e.ZOOMID, e.mail, e.phone, e.MISID };
                        if (es.Count() == 1)
                        {
                            er.AppendLine("取得特定會員資料！");
                            Global.funcLog(funcName, ps, er.ToString(), "Success");
                            return Global.jss.Serialize(es);
                        }
                        else
                        {
                            Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                            return Global.jss.Serialize("Wrong : 查無資料");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.funcLog(funcName, ps, er.AppendLine(ex.Message).ToString(), "Error");
                return Global.jss.Serialize(String.Format("Error code : 0x84177282005\r\n\r\n{0}", ex.Message));
            }
        }
    
        [Description("取得平台所有會員")]
        [OperationContract, WebInvoke(UriTemplate = "listEndUser?eid={eid}&i={i}", Method = "GET", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public String listEndUserG(string eid = "nobody", string i = "clientIP")
        {
            return listEndUser(eid, i);
        }
    
        private String updatePassword(string pw, string meid,int r = 0 , string eid = "nobody", string i = "clientIP")
        {
            StringBuilder er = new StringBuilder();
            const string funcName = "updatePassword";
            string ps = String.Format("pw:{0},meid:{1},r:{2},eid:{3},IP:{4}", pw, meid, r, eid, i);
            try
            {
                Global.init(1);
                using (Entities db = new Entities(Global.connStr))
                {
                    er.AppendLine("初始化！");
                    var meus = from e in db.EndUsers
                              where e.EneUserID == meid
                              select e;
                    if (meus.Count() != 1)
                    {
                        er.AppendLine("群組成員ID錯誤！");
                        Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                        return Global.jss.Serialize("Wrong : 群組成員ID錯誤");
                    }
                    var eus = from e in db.EndUsers
                              where e.EneUserID == eid
                              select e;
                    if (eus.Count() != 1)
                    {
                        er.AppendLine("修改資料的修改者ID錯誤！");
                        Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                        return Global.jss.Serialize("Wrong : 修改資料的修改者ID錯誤");
                    }
                    if (eus.Count() == 1)
                    {
                        er.AppendLine("取得使用者資料！");
                        EndUsers meu = meus.First();
                       
                        if (r == 0)
                        {
                            meu.euPassword = pw;
                        }
                        else if (r == 1)
                        {
                            string npw = Global.base46encode(DateTime.Now.Ticks + new Random().Next(1, 999));
                            meu.euPassword = npw;
                            cs.sendSMTP(meu.mail, "密碼重置成功", "您的新密碼為：" + npw, ref er);
                        }
                        db.SaveChanges();
                        Global.funcLog(funcName, ps, er.ToString(), "Success");
                        return Global.jss.Serialize("Success");
                    }
                    er.AppendLine("無法取得使用者資料！");
                    Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                    return Global.jss.Serialize("Error:update password fail!");
                }
            }
            catch (Exception ex)
            {
                Global.funcLog(funcName, ps, er.AppendLine(ex.Message).ToString(), "Error");
                return Global.jss.Serialize(String.Format("Error code : 0x84177282105\r\n\r\n{0}", ex.Message));
            }
        }
      
        [Description("自動替換新密碼，並且發送mail供確認---目前還是缺少（再認證，的動作，所以需要小討論一下")]
        [OperationContract, WebInvoke(UriTemplate = "updatePassword?pw={pw}&meid={meid}&r={r}&eid={eid}&i={i}", Method = "GET", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public String updatePasswordG(string pw, string meid,  int r = 0, string eid="nobody" , string i = "clientIP")
        {
            return updatePassword(pw, meid, r, eid, i);
        }
        //----------------------------
        private String updateUserData(string mail, string conttel, string nickname, string meid,string eid = "nobody", string i = "clientIP")
        {
            StringBuilder er = new StringBuilder();
            const string funcName = "updatePasswordMailContTel";
            string ps = String.Format("mail:{0},conttel:{1},nickname:{2},meid:{3},eid:{4},IP:{5}", mail, conttel, nickname, meid,eid, i);

            try
            {
                Global.init(1);
                using (Entities db = new Entities(Global.connStr))
                {
                    er.AppendLine("初始化！");
                    var seus = from se in db.EndUsers
                               where se.EneUserID == eid
                               select se;
                    if (seus.Count() != 1)
                    {
                        er.AppendLine("修改資料的修改者ID錯誤！");
                        Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                        return Global.jss.Serialize("Wrong : 修改資料的修改者ID錯誤");
                    }

                    var meus = from e in db.EndUsers
                              where e.EneUserID == meid
                              select e;
                    if (meus.Count() == 1)
                    {
                        EndUsers meu = meus.First();
                        er.AppendLine("取得使用者資料！");

                        if (meu.mail == mail) er.AppendLine("mail相同！");
                        if (meu.phone == conttel) er.AppendLine("電話相同！");
                        if (meu.euNickname == nickname) er.AppendLine("名稱相同！");

                        if (conttel.Length > 0) meu.phone = conttel;
                        if (mail.Length > 0) meu.mail = mail;
                        if (nickname.Length > 0) meu.euNickname = nickname;

                        db.SaveChanges();
                        er.AppendLine("使用者資料更新完成！");
                        Global.funcLog(funcName, ps, er.ToString(), "Success");
                        return Global.jss.Serialize("Success");
                    }
                    er.AppendLine("無法取得使用者資料！");
                    Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                    return Global.jss.Serialize("Wrong :update user data fail!");
                }
            }
            catch (Exception ex)
            {
                er.AppendLine(ex.Message);
                er.AppendLine(ex.InnerException.TargetSite.Name);
                er.AppendLine(ex.Source);
                er.AppendLine(ex.HelpLink);
                Global.funcLog(funcName, ps, er.ToString(), "Error");
                return Global.jss.Serialize(String.Format("Error code : 0x84177282305\r\n\r\n{0}", ex.Message));
            }
        }
       
        [Description("更新會員資料")]
        [OperationContract, WebInvoke(UriTemplate = "updateUserData?mail={mail}&conttel={conttel}&nickname={nickname}&meid={meid}&eid={eid}&i={i}", Method = "GET", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public String updateUserDataG(string mail, string conttel, string nickname, string meid,string eid = "nobody", string i = "clientIP")
        {
            return updateUserData(mail, conttel, nickname,meid, eid, i);
        }
      
        private String getUser(string id,string i = "clientIP")
        {
            StringBuilder er = new StringBuilder();
            const string funcName = "getUser";
            string ps = String.Format("id:{0}IP:{1}",id, i);
            try
            {
                Global.init(1);
                using (Entities db = new Entities(Global.connStr))
                {
                    er.AppendLine("初始化！");
                    string apiParam, apiRes;
                 
                    apiParam = String.Format("uid={0}", id);
                    er.AppendLine(String.Format("apiParam={0}", apiParam));
                    using (WebClient zoomws = new WebClient() { Encoding = System.Text.Encoding.UTF8 })
                    {
                        apiRes = zoomws.DownloadString(new Uri(Global.strWS + Global.userGet + apiParam));
                    }
                    apiRes = Global.getAPIJson(apiRes);
                    er.AppendLine(String.Format("apiRes={0}", apiRes));
                    er.AppendLine("成功！");
                    Global.funcLog(funcName, ps, er.ToString(), "Success");
                    return Global.jss.Serialize(apiRes);

                }
            }
            catch (Exception ex)
            {
                Global.funcLog(funcName, ps, er.AppendLine(ex.Message).ToString(), "Error");
                return Global.jss.Serialize(String.Format("Error code : 0x84177282908\r\n\r\n{0}", ex.Message));
            }
        }
      
        [Description("取得USER在ZOOM的資料")]
        [OperationContract, WebInvoke(UriTemplate = "getUser?id={id}&i={i}", Method = "GET", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public String getUserG(string id, string i = "clientIP")
        {
            return getUser(id, i);
        }
        //---------------------
        private String upUser(string eid,string meids,int tp, string i = "clientIP")
        {
            StringBuilder er = new StringBuilder();
            const string funcName = "upUser";
            string ps = String.Format("eid:{0}meid:{1}tp:{2}IP:{3}", eid, meids, tp, i);
            try
            {
                Global.init(1);
                using (Entities db = new Entities(Global.connStr))
                {
                    er.AppendLine("初始化！");
                    
                    string[] ms = meids.Split(new char[]{','});
                    var seus = from se in db.EndUsers
                               where se.EneUserID == eid
                               select se;
                    if (seus.Count() != 1)
                    {
                        er.AppendLine("更新群組的管理員ID錯誤！");
                        Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                        return Global.jss.Serialize("Wrong : 更新群組的群組管理員ID錯誤");
                    }
                    else if (seus.First().IdentityTypeID < 978)
                    {
                        er.AppendLine("更新群組的管理員的權限不足！");
                        Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                        return Global.jss.Serialize("Wrong : 更新群組的管理員的權限不足");
                    }
                    er.AppendLine(">>>");
                    foreach (string m in ms) er.Append(m + "_");
                    er.AppendLine("\r\n<<<");

                    var hs = from h in db.EndUsers
                             where ms.Contains(h.EneUserID)
                             select h;
                    
                    if (hs.Count() == 0)
                    {
                        er.AppendLine("群組成員ID錯誤！" + hs.Count());
                        Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                        return Global.jss.Serialize("Wrong : 群組成員ID錯誤");
                    }
                    er.AppendLine("群組成員資料取得成功！");
                    if (db.Connection.State != System.Data.ConnectionState.Open) db.Connection.Open();
                    using (var tran = db.Connection.BeginTransaction())
                    {
                        StringBuilder apiParam = new StringBuilder();
                        StringBuilder apiRes = new StringBuilder();
                        foreach (var eu in hs)
                        {
                            if (eu.ZOOMID == null) continue;
                            apiParam.Append(String.Format("uid={0}&tp={1}", eu.ZOOMID,tp));
                            using (WebClient zoomws = new WebClient() { Encoding = System.Text.Encoding.UTF8 })
                            {
                                apiRes.Append(zoomws.DownloadString(new Uri(Global.strWS + Global.userUpdate + apiParam)));
                                
                            }
                            //er.AppendLine("取得API回傳資料（ZOOM" + eu.ZOOMID + "會員資料）！" + apiRes);
                            apiParam.Clear();
                            apiRes.Clear();
                        }
                        foreach (var m in hs)
                            m.IdentityTypeID = tp;
                        db.SaveChanges();
                        tran.Commit();
                        Global.funcLog(funcName, ps, er.ToString(), "Success");
                        return Global.jss.Serialize("Success");
                    }
                }
                //Global.funcLog(funcName, ps, er.ToString(), "Success");
                
            }
            catch (Exception ex)
            {
                Global.funcLog(funcName, ps, er.AppendLine(ex.Message).ToString(), "Error");
                return Global.jss.Serialize(String.Format("Error code : 0x84177282908\r\n\r\n{0}", ex.Message));
            }
        }

        [Description(@"更新指定成員（們），eids：要被更新的EID（S），用『,』分隔")]
        [OperationContract, WebInvoke(UriTemplate = "upUser?eid={eid}&meids={meids}&tp={tp}&i={i}", Method = "GET", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public String upUserG(string eid,string meids, int tp,string i = "clientIP")
        {
            return upUser(eid, meids,tp,i);
        }
        //---------------------
        private String getAccountReport(int ps,int  pn,string  fd,string  td,string i = "clientIP")
        {
            StringBuilder er = new StringBuilder();
            const string funcName = "getAccountReport";
            string pss = String.Format("ps:{0}pn:{1}fd:{2}td:{3}i:{4}", ps, pn, fd, td, i);
            try
            {
                Global.init(1);
                using (Entities db = new Entities(Global.connStr))
                {
                    er.AppendLine("初始化！");
                    string apiParam, apiRes;

                    apiParam = String.Format("ps={0}&pn={1}&fd={2}&td={3}", ps, pn, fd, td);
                    er.AppendLine(String.Format("apiParam={0}", apiParam));
                    using (WebClient zoomws = new WebClient() { Encoding = System.Text.Encoding.UTF8 })
                    {
                        apiRes = zoomws.DownloadString(new Uri(Global.strWS + Global.getaccountreport + apiParam));
                    }
                    apiRes = Global.getAPIJson(apiRes);
                    er.AppendLine(String.Format("apiRes={0}", apiRes));
                    er.AppendLine("成功！");
                    Global.funcLog(funcName, pss, er.ToString(), "Success");
                    return Global.jss.Serialize(apiRes);

                }
            }
            catch (Exception ex)
            {
                Global.funcLog(funcName, pss, er.AppendLine(ex.Message).ToString(), "Error");
                return Global.jss.Serialize(String.Format("Error code : 0x84177282908\r\n\r\n{0}", ex.Message));
            }
        }
        //---          
        //---
        [Description("取得acco在ZOOM的total資料")]
        [OperationContract, WebInvoke(UriTemplate = "getAccountReport?ps={ps}&pn={pn}&fd={fd}&td={td}&i={i}", Method = "GET", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public String getAccountReportG(int ps,int  pn, string fd, string td, string i = "clientIP")
        {
            return getAccountReport(ps, pn, fd, td, i);
        }
        //---------------------
        private String getuserreport(int ps, int pn, string fd, string td, string zid, string i = "clientIP")
        {
            StringBuilder er = new StringBuilder();
            const string funcName = "getuserreport";
            string pss = String.Format("ps:{0}pn:{1}fd:{2}td:{3}zid:{4}i:{5}", ps, pn, fd, td, zid, i);
            try
            {
                Global.init(1);
                using (Entities db = new Entities(Global.connStr))
                {
                    er.AppendLine("初始化！");
                    string apiParam, apiRes;

                    apiParam = String.Format("ps={0}&pn={1}&fd={2}&td={3}&zid={4}", ps, pn, fd, td, zid);
                    er.AppendLine(String.Format("apiParam={0}", apiParam));
                    using (WebClient zoomws = new WebClient() { Encoding = System.Text.Encoding.UTF8 })
                    {
                        apiRes = zoomws.DownloadString(new Uri(Global.strWS + Global.getuserreport + apiParam));
                    }
                    apiRes = Global.getAPIJson(apiRes);
                    er.AppendLine(String.Format("apiRes={0}", apiRes));
                    er.AppendLine("成功！");
                    Global.funcLog(funcName, pss, er.ToString(), "Success");
                    return Global.jss.Serialize(apiRes);
                }
            }
            catch (Exception ex)
            {
                Global.funcLog(funcName, pss, er.AppendLine(ex.Message).ToString(), "Error");
                return Global.jss.Serialize(String.Format("Error code : 0x84177282908\r\n\r\n{0}", ex.Message));
            }
        }
        //---          
        //---
        [Description("取得user在ZOOM的total資料")]
        [OperationContract, WebInvoke(UriTemplate = "getuserreport?ps={ps}&pn={pn}&fd={fd}&td={td}&zid={zid}&i={i}", Method = "GET", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public String getuserreportG(int ps, int pn, string fd, string td, string zid, string i = "clientIP")
        {
            return getuserreport(ps, pn, fd, td, zid, i);
        }
        //---------------------
        //---------------------
        //---------------------
        //--------------------- 
        [Description("檢查信箱/電話/帳號是否已存在，參數必須只能輸入一種 ，若無資料回傳：Success ， 重複回傳:Error")]
        [OperationContract, WebInvoke(UriTemplate = "checkAccount?gid={gid}&mail={mail}&phone={phone}&account={account}&i={i}", Method = "GET", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public String checkAccountG(string gid, string mail = "mail", string phone = "phone", string account = "account", string i = "clientIP")
        {
            return checkAccount( gid, mail, phone, account, i);
        }
        //----------------------------
        private String checkAccount(string gid, string mail = "mail", string phone = "phone", string account = "account", string i = "clientIP")
        {
            StringBuilder er = new StringBuilder();
            const string funcName = "checkAccount";
            string ps = String.Format("gid:{0}mail:{1},phone:{2},account:{3},IP:{4}", gid, mail, phone, account, i);

            Global.init(1);
            try
            {
                using (Entities db = new Entities(Global.connStr))
                {
                    if (((mail == null || mail == "") && (phone == null || phone == "") && (account == null || account == ""))||
                   (!(mail == null || mail == "") && !(phone == null || phone == ""))||
                   (!(mail == null || mail == "") && !(account == null || account == ""))||
                   (!(phone == null || phone == "") && !(account == null || account == ""))||
                   (!(phone == null || phone == "") && !(account == null || account == "") && !(mail == null || mail == "")))
                    {
                        er.AppendLine("欄位必須輸入唯一參數或是不可為空白");
                        Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                        return Global.jss.Serialize("Error1");
                    }
                    else
                    {
                        er.AppendLine("初始化！");
                        if (gid == null || gid == "" || gid== "{GID}")
                        {
                            if (!(mail == null || mail == ""))
                            {
                                var meus = from e in db.EndUsers
                                           where e.mail == mail
                                           select e;
                                if (meus.Count() > 0)
                                {
                                    er.AppendLine("此信箱已有人使用");
                                    Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                                    return Global.jss.Serialize("Error");
                                }
                                er.AppendLine("此信箱沒有人使用");
                                Global.funcLog(funcName, ps, er.ToString(), "Success");
                                return Global.jss.Serialize("Success");
                            }
                            if (!(phone == null || phone == ""))
                            {


                                var peus = from e in db.EndUsers
                                           where e.phone == phone
                                           select e;
                                if (peus.Count() > 0)
                                {
                                    er.AppendLine("此電話已有人使用");
                                    Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                                    return Global.jss.Serialize("Error");
                                }
                                er.AppendLine("此電話沒有人使用");
                                Global.funcLog(funcName, ps, er.ToString(), "Success");
                                return Global.jss.Serialize("Success");
                            }
                            if (!(account == null || account == ""))
                            {
                                var aeus = from e in db.EndUsers
                                           where e.euAccount == account
                                           select e;
                                if (aeus.Count() > 0)
                                {
                                    er.AppendLine("此帳號已有人使用");
                                    Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                                    return Global.jss.Serialize("Error");
                                }
                                er.AppendLine("此帳號沒有人使用");
                                Global.funcLog(funcName, ps, er.ToString(), "Success");
                                return Global.jss.Serialize("Success");
                            }
                        }
                        else
                        {
                            if (!(mail == null || mail == ""))
                            {
                                var meus = from e in db.EndUsers
                                           join g in db.HMsGMapping
                                           on e.EneUserID equals g.MEID
                                           where g.HMsGID == gid && e.mail == mail
                                           select e;
                                if (meus.Count() > 0)
                                {
                                    er.AppendLine("此信箱已有人使用");
                                    Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                                    return Global.jss.Serialize("Error");
                                }
                                er.AppendLine("此信箱沒有人使用");
                                Global.funcLog(funcName, ps, er.ToString(), "Success");
                                return Global.jss.Serialize("Success");
                            }
                            if (!(phone == null || phone == ""))
                            {


                                var peus = from e in db.EndUsers
                                           join g in db.HMsGMapping
                                           on e.EneUserID equals g.MEID
                                           where g.HMsGID == gid && e.phone == phone
                                           select e;
                                if (peus.Count() > 0)
                                {
                                    er.AppendLine("此電話已有人使用");
                                    Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                                    return Global.jss.Serialize("Error");
                                }
                                er.AppendLine("此電話沒有人使用");
                                Global.funcLog(funcName, ps, er.ToString(), "Success");
                                return Global.jss.Serialize("Success");
                            }
                            if (!(account == null || account == ""))
                            {
                                var aeus = from e in db.EndUsers
                                           where e.euAccount == account
                                           select e;
                                if (aeus.Count() > 0)
                                {
                                    er.AppendLine("此帳號已有人使用");
                                    Global.funcLog(funcName, ps, er.ToString(), "Wrong");
                                    return Global.jss.Serialize("Error");
                                }
                                er.AppendLine("此帳號沒有人使用");
                                Global.funcLog(funcName, ps, er.ToString(), "Success");
                                return Global.jss.Serialize("Success");
                            }
                        }
                    }
                    return Global.jss.Serialize("Error");
                }
            }
            catch (Exception ex)
            {
                Global.funcLog(funcName, ps, er.AppendLine(ex.Message).ToString(), "Error");
                return Global.jss.Serialize(String.Format("Error code : 0x84177282908\r\n\r\n{0}", ex.Message));
            }          
        }
        //--------------------- 
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
          private String forgpass(string id, string mail, string i = "clientIP")
        {
            StringBuilder er = new StringBuilder();
            const string funcName = "forgpass";
            string pss = String.Format("id:{0}mail:{1}", id, mail, i);
            try
            {
                Global.init(1);
                using (Entities db = new Entities(Global.connStr))
                {
                    er.AppendLine("初始化！");
                    var eus = from e in db.EndUsers
                              where e.euAccount == id & e.mail == mail
                              select e;
                    var eu = eus.First();
                    string pass = eu.euPassword;
                    if (eus.Count() != 1)
                    {
                        er.AppendLine("帳號或信箱錯誤！");
                        Global.funcLog(funcName, pss, er.ToString(), "Wrong");
                        return Global.jss.Serialize("Wrong:帳號或信箱錯誤");
                        
                    }
                    string title = "系統測試--NBD System Testing For Password";
                    string smail;
                    smail = "您好：您的帳號為:" + id + " 密碼為:" + pass + " 請確認帳號與密碼是否與您申請時無異 感謝您的註冊 NBD小組敬上同意";
                    cs.sendSMTP(mail, title, smail, ref er);
                        
                    er.AppendLine("成功！");
                    Global.funcLog(funcName, pss, er.ToString(), "Success");
                    return Global.jss.Serialize("Success");
                    
                }
            }
            catch (Exception ex)
            {
                Global.funcLog(funcName, pss, er.AppendLine(ex.Message).ToString(), "Error");
                return Global.jss.Serialize(String.Format("Error code : 0x84177282908\r\n\r\n{0}", ex.Message));
            }
        }
        //---          
        //---
        [Description("查詢並傳送user的密碼")]
        [OperationContract, WebInvoke(UriTemplate = "forgpass?id={id}&mail={mail}&i={i}", Method = "GET", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public String forgpassG( string id, string mail, string i = "clientIP")
        {
            return forgpass( id, mail, i);
        }
        //---------------------
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private String listlogsp(string fid , string i = "clientIP")
        {
            StringBuilder er = new StringBuilder();
            const string funcName = "listlogsp";
            string ps = String.Format("fid:{0},ip:{1}", fid, i);
            try
            {
                Global.init(1);
                using (Entities db = new Entities(Global.connStr))
                {
                    er.AppendLine("初始化！");

                    //var sd = from e in db.CallLogs
                    //         where
                    //         e.functionName == "startClassesS" ||
                    //         e.functionName == "signin"
                    //         group e by new
                    //         {
                    //             e.functionName
                    //         } into g
                    //         select new
                    //         {
                    //             g.Key.functionName,
                    //             num = (Int64?)g.Count(p => p.functionName != null)
                    //         };

                    var es = from a in
                                 (
                                      (from calllogs in db.CallLogs
                                       where
                                         calllogs.functionName == fid
                                       select new
                                       {
                                           calllogs.functionName,
                                           calllogs.logType
                                       }))
                             where
                               (new string[] { "Success", "Error", "Wrong" }).Contains(a.logType)
                             group a by new
                             {
                                 a.functionName,
                                 a.logType
                             } into g
                             select new
                             {
                                 g.Key.functionName,
                                 g.Key.logType,
                                 num = (Int64?)g.Count(p => p.logType != null)
                             };

                    //var page1 = es.Skip(0).Take(100).ToList();
                    //var sss = sd;
                    /*string sdu="{\"cols\": [";
                    foreach(var gosh in page1)
                    {
                        sdu+=string.Format("{{{0}:{1}{2}}},",gosh.functionName,gosh.LogID,"\n");
                    };
                    sdu = sdu + "]}";*/
                    er.AppendLine("成功！A");
                    Global.funcLog(funcName, ps, er.ToString(), "Success");
                    return Global.jss.Serialize(es);

                    ////return listlogG(next,name,i);


                    //e.functionName, e.pamarts, e.returnData, e.logType,e.createDate,                     
                    /*foreach (var a in es.Skip(2 * pageSize).Take(pageSize))
                    {
                        System.Console.WriteLine(a.functionName,"測試1");
                    }*/


                }
            }
            catch (Exception ex)
            {
                Global.funcLog(funcName, ps, er.AppendLine(ex.Message).ToString(), "Error");
                return Global.jss.Serialize(String.Format("Error code : 0x84177282005\r\n\r\n{0}", ex.Message));
            }
        }

        [Description("輸入FUNC成功取出 Success Error Wrong 數量")]
        [OperationContract, WebInvoke(UriTemplate = "listlogsp?fid={fid}&i={i}", Method = "GET", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public String listlogspG(string fid , string i = "clientIP")
        {
            return listlogsp(fid, i);
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private String listlogsw(string fid, string ltp, string i = "clientIP")
        {
            StringBuilder er = new StringBuilder();
            const string funcName = "listlogsw";
            string ps = String.Format("fid:{0},ltp:{1},ip:{2}", fid, ltp, i);
            try
            {
                Global.init(1);
                using (Entities db = new Entities(Global.connStr))
                {
                    er.AppendLine("初始化！");

                    var es = from e in db.CallLogs
                             where e.functionName == fid & e.logType == ltp
                             select e;
                    er.AppendLine("成功！A");
                    Global.funcLog(funcName, ps, er.ToString(), "Success");
                    return Global.jss.Serialize(es);

                }
            }
            catch (Exception ex)
            {
                Global.funcLog(funcName, ps, er.AppendLine(ex.Message).ToString(), "Error");
                return Global.jss.Serialize(String.Format("Error code : 0x84177282005\r\n\r\n{0}", ex.Message));
            }
        }

        [Description("輸入FUNC & ltp 成功取出記錄")]
        [OperationContract, WebInvoke(UriTemplate = "listlogsw?fid={fid}&ltp={ltp}&i={i}", Method = "GET", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public String listlogswG(string fid, string ltp, string i = "clientIP")
        {
            return listlogsw( fid, ltp, i);
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private String listlogf(string fid , string i = "clientIP")
        {
            StringBuilder er = new StringBuilder();
            const string funcName = "listlogf";
            string ps = String.Format("fid:{0},IP:{1}", fid, i);
            try
            {
                Global.init(1);
                using (Entities db = new Entities(Global.connStr))
                {
                    er.AppendLine("初始化！");

                    //Newtonsoft.Json.Converters.IsoDateTimeConverter iso = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
                            //iso.DateTimeFormat = "yyyy-MM-dd hh:mm";
                    var es = from e in db.CallLogs
                             orderby e.LogID
                             where e.functionName == fid
                             select new { e.LogID, e.functionName, e.returnData, e.logType, e.createDate };
                    var count = es.Count();   //e.functionName, e.pamarts, e.returnData, e.logType,e.createDate,                     
                    /*foreach (var a in es.Skip(2 * pageSize).Take(pageSize))
                    {
                        System.Console.WriteLine(a.functionName,"測試1");
                    }*/


                    er.AppendLine("成功！");
                    Global.funcLog(funcName, ps, er.ToString(), "Success");
                    return Global.jss.Serialize(es);
                    //return Global.jss.Serialize(JsonConvert.SerializeObject(es, iso));
                    //return listlogG(next,name,i);

                }
            }
            catch (Exception ex)
            {
                Global.funcLog(funcName, ps, er.AppendLine(ex.Message).ToString(), "Error");
                return Global.jss.Serialize(String.Format("Error code : 0x84177282005\r\n\r\n{0}", ex.Message));
            }
        }

        [Description("輸入FUNC取出記錄")]
        [OperationContract, WebInvoke(UriTemplate = "listlogf?fid={fid}&i={i}", Method = "GET", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public String listlogfG(string fid, string i = "clientIP")
        {
            return listlogf(fid, i);
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}