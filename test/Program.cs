using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MessageService;
namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            //var enc = aesEncryptBase64("{userid,name}","0988163835","TaiPower");
            //var dec=aesDecryptBase64(enc,"0988163835","TaiPower");



            //using (MessageServiceDBEntities db = new MessageServiceDBEntities())
            //{

            //    foreach (var rec in  db.tblMessageLog)
            //        Console.WriteLine(rec.message);

            //}
        //    for(int i=0;i<5;i++)
            SendSMS("0988163835", $"中文輸入");
               

            Console.ReadLine();
        }

        static bool SendSMS(string phoneno,string message)
        {


            string comport = "Com7";

            System.IO.Ports.SerialPort com = new System.IO.Ports.SerialPort(comport, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
            try
            {
                try
                {
                    com.Close();
                }
                catch {; }
                com.Open();


                System.IO.StreamReader rd = new StreamReader(com.BaseStream);
                rd.BaseStream.ReadTimeout = 1000;
                System.IO.StreamWriter wr = new StreamWriter(com.BaseStream);
                wr.WriteLine("at+cmgf=0");
                wr.Flush();
                if (!ReadATCmdResult(rd))
                    return false;
                SmsEncode smsenc = new SmsEncode(phoneno, message);
                string pdu = smsenc.finalSmsCode;

                wr.WriteLine(smsenc.cmgsLength);
                wr.Flush();

                ReadWaitChar(rd, '>');
                wr.Write(pdu);
                wr.Flush();
                if (!ReadATCmdResult(rd))
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {

                com.Close();
            }




            return true; 
        }

        static bool ReadWaitChar(StreamReader rd,char ch)
        {
            try
            {
                char c = ' ';
                do
                {
                     c = (char)rd.Read();
                } while (ch!=c);
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }

           

        }
       static  bool ReadATCmdResult(StreamReader rd)
        {
            string res = "";
            try { 
                do
                {

                    res = rd.ReadLine();
                    Console.WriteLine($"{res}");
                } while (!(res == "OK" || res == "ERROR"));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
               return false ;
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
    }
}
