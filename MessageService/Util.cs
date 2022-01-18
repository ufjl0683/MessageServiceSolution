using MessageService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace TaiPower
{
    public class Util
    {
       public static bool SendSMS(string phoneno, string message)
        {
            string comport = WebConfigurationManager.AppSettings["SmsComPort"];

            System.IO.Ports.SerialPort com = new System.IO.Ports.SerialPort(comport, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
            try
            {
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

        static bool ReadWaitChar(StreamReader rd, char ch)
        {
            try
            {
                char c = ' ';
                do
                {
                    c = (char)rd.Read();
                } while (ch != c);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }



        }
        static bool ReadATCmdResult(StreamReader rd)
        {
            string res = "";
            try
            {
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
                return false;
            }

        }


    }
}