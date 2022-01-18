using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageService
{
    class SmsEncode
    {
        #region 宣告
        public string finalSmsCode { get; set; }                            //最終編碼後的字串
        public string cmgsLength { get; set; }                              //最終的簡訊長度

        string phoneNumber;                                                 //受簡訊方的電話號碼
        string dataLength;                                                  //簡訊內容的長度
        string encodeData;                                                  //編碼過的簡訊內容
        string encodeType;                                                  //簡訊內容的編碼型式

        //const string SMSC = "886932400841";                               //中華電信簡訊中心的號碼
        //const string SMSC = "886931000099";                               //遠傳電信簡訊中心的號碼
        //const string SMSC = "886935074443";                               //台哥大簡訊中心的號碼
        const string SMSC = "00";                                           //使用卡上定義的簡訊中心號碼

        const string SMSC_LENGTH_AND_TYPE_OF_ADDRESS = "0791";              //【發話端】，簡訊中心的號碼的長度
        const string PHONELENGTH = "0C";                                    //受簡訊方電話號碼的長度(不含'+' 預設都是12碼 【因為編碼都採+886】)
        const string TON_NPI_91 = "91";                                     //受簡訊方電話號碼的編碼格式，使用國際編碼
        const string TON_NPI_81 = "81";                                     //受簡訊方電話號碼的編碼格式，不使用國際編碼(+886)
        const string TON_NPI_92 = "92";
        const string SMS_ALIVE_TIME = "AA";
        //const string TP_MTI = "11000A";                                   //本地編碼格式(如0912345678)
        const string TP_MTI = "11000C";                                     //國際編碼格式(如886912345678)
        const string AT_Send_SMS_Command = "AT+CMGS";
        const char CTRL_Z = (char)26;                                       //字串最後的字尾  (即CTRL+Z）

        #endregion


        #region 建構式  SmsEncode(string _phoneNumber, string message)
        /* 1. 傳入電話號碼與簡訊
         * 2. 檢查電話的格式，看是否為國際格式或是台灣格式（都轉換為台灣格式）
         * 3. 檢查編碼的格式，是純英文(0000)或是中文(0008) 
         * 4. 計算資料的長度
         * 5. 結合後輸出 */

        public SmsEncode(string _phoneNumber, string message)
        {
            //先把非數字的字濾掉（怕出現電話號碼出現連字號 xxxx-xxx-xxx） 
            //傳回一定為數字的電話號碼
            _phoneNumber = filterPhoneNumber(_phoneNumber);

            //如果電話號碥小於10，大於12 （09xx是十個數字,886是12個）就直接返回錯誤
            if (_phoneNumber.Length < 10 || _phoneNumber.Length >12)
                return;
            else
                phoneNumber = checkPhoneNumberFormat(_phoneNumber);
			
			
            if (isCharactorAndNumber(message))
            {   /* 選定編碼的方式，是中文USC2編碼或是英文的 ASCII編碼 */
                encodeType = "0000";
                encodeData = encodeEnglish(message);
                dataLength = Convert.ToString(message.Length, 16).PadLeft(2, '0');
            }
            else
            {
                encodeType = "0008";
                encodeData = encodeUnicode(message);
                dataLength = Convert.ToString(encodeData.Length / 2, 16).PadLeft(2, '0');       //長度還是要補0，補為8個bit(即0x00)
            }

            /* 傳回最終的字串 設定SMSC用此法 */
            /* finalSmsCode = SMSC_LENGTH_AND_TYPE_OF_ADDRESS + reverseBit(SMSC) + TP_MTI + TON_NPI_92
               + phoneNumber + encodeType + SMS_ALIVE_TIME + dataLength + encodeData + CTRL_Z; */

            /* 不設定SMSC的最終傳回字串 */
            finalSmsCode =( SMSC  + TP_MTI + TON_NPI_91 +phoneNumber + encodeType + SMS_ALIVE_TIME + dataLength + encodeData + CTRL_Z).ToUpper();

            //下面為不設定SMSC的寫法專用
            //傳回AT+CMGS=xx 的長度，最終的字串為 AT+CMGS=XX\r */ 
            /*if (encodeType == "0008")
                cmgsLength = "AT+CMGS=" + (14 + (message.Length * 2)).ToString() + "\r";
            else
                //cmgsLength = "AT+CMGS=" + (14 + message.Length).ToString() + "\r";*/
            cmgsLength = "AT+CMGS=" + (finalSmsCode.Length/2-1).ToString ()+"\r";
        }
        #endregion


        #region 純英數PDU格式編碼 encodeEnglish(string message)
        /// <summary>
        /// 純英數PDU格式編碼
        /// </summary>
        /// <param name="message">傳入要編碼的字串</param>
        /// <returns>純英數PDU編碼格式的字串</returns>
        private string encodeEnglish(string message)
        {
            string encodeString = "";
            int counter = 0;
            List<string> tmpSplit = new List<string>();
            List<string> tmpList = new List<string>();
            List<String> resultList = new List<string>();                                       //存放結果的List


            /*
             * 將輸入的數字轉成Byte，
             * 再將其轉成二進後存在LIST<String>中
             * */
            byte[] tmpByteArray = System.Text.ASCIIEncoding.Default.GetBytes(message);
            for (int x = 0; x < tmpByteArray.Length; x++)
            {
                tmpList.Add(Convert.ToString(tmpByteArray[x], 2).PadLeft(7, '0'));              //一定要變成7digit，
                if (counter == 8)                                                               //不然會出錯
                    counter = 0;

                /*
                 * 編碼為PDU模式
                 * */

                ///進行PDU編碼 (原始：tmpList    要傳遞的碼：tmpSplit
                tmpSplit.Add(tmpList[x].Substring(tmpList[x].Length - counter, counter));       //取走要傳遞的bit放在tmpSplit中

                tmpList[x] = tmpList[x].Substring(0, 7 - counter);                              //並把原來的字串切掉
                counter++;
            }


            ///將要傳遞的資料，第一個部份先移掉 （未移前，是 tmpSplit[i+1]+tmpList[i]， 移走後變成tmpSplit[i]+tmpList[i]
            ///比較好處理
            ///補0，則是移掉一個後，兩邊的數量就不對稱，故再加一個回去（  加入的是空，  空+任何數還是空，所以不影響結果）
            tmpSplit.RemoveAt(0);
            tmpSplit.Add("");


            ///這邊先將字串結合
            for (int y = 0; y < tmpList.Count; y++)
            {
                resultList.Add((tmpSplit[y] + tmpList[y]).PadLeft(8, '0'));
            }

            //每8次就會有一個空欄位，這是將空欄位移除（看 http://www.dreamfabric.com/sms/hello.html hellohello的編碼）
            for (int y = 7; y < tmpList.Count; y += 7)
            {
                resultList.RemoveAt(y);
            }

            tmpList.Clear();                                                                    //清除tmpList原先的內容，後面還要使用
            tmpSplit.Clear();                                                                   //tmpSplit後面則是用不到了
            tmpSplit = null;

            ///將新產生的編碼，轉換成16進
            string upperByte, lowByte;                                                         //高位與低位，低位一定是4個bit，高位則不一定
            int tmpIndex;                                                                       //切高低位的索引  （其實不是3，就是4...）
            int tmpSum;
            for (int i = 0; i < resultList.Count; i++)
            {
                tmpIndex = Math.Abs(4 - resultList[i].Length);                                  //4-7 or 4-8 取絕對值，就可以得到高位是3bit還是4bit 
                tmpSum = Convert.ToInt32(resultList[i].Substring(tmpIndex, 4), 2);            //先轉成十進(int)
                lowByte = Convert.ToString(tmpSum, 16);                                         //再轉為16進字串

                resultList[i] = resultList[i].Substring(0, tmpIndex);
                tmpSum = Convert.ToInt32(resultList[i], 2);
                upperByte = Convert.ToString(tmpSum, 16);
                encodeString = encodeString + upperByte.ToString() + lowByte.ToString();

            }
            resultList = null;
            tmpList = null;

            return encodeString;
        }
        #endregion


        #region Unicode PDU格式的編碼 encodeUnicode(string message)
        /// <summary>
        /// 中文pdu格式編碼
        /// </summary>
        /// <param name="message">要進行編碼的字串</param>
        /// <returns>傳回編碼過的字串</returns>
        private string encodeUnicode(string message)
        {
            string encodeString = "";
            string tmpStr = "";
            byte[] tmpByteArray = System.Text.Encoding.Unicode.GetBytes(message);

            ///內迴圈：組合單一unicode的高byte與低byte
            ///外迴圈：組合編碼字串
            //
          
            for (int i = 0; i < tmpByteArray.Length; i += 2)
            {
                for (int x = 0; x < 2; x++)
                {   /* 高byte與低byte結合，但要將每個byte補0成為 "##" */
                    tmpStr = Convert.ToString(tmpByteArray[i + x], 16).PadLeft(2, '0') + tmpStr;
                }
                encodeString = encodeString + tmpStr;
                tmpStr = "";
            }
            return encodeString;
        }
        #endregion


        #region 檢查要編碼的字串是否為英文、數字與空白組成  isCharactorAndNumber(string message)
        /// <summary>
        /// 檢查字串是否為英、數組成
        /// </summary>
        /// <param name="message">要判斷的字串</param>
        /// <returns>true-> 是英數組成， false為非純英數</returns>
        private bool isCharactorAndNumber(string message)
        {
            return false;
            /* 檢查是否為正規式中，A-Z a-z 0-9 與空白(\s) 來組成 */
            System.Text.RegularExpressions.Regex reg1 = new System.Text.RegularExpressions.Regex(@"^[A-Za-z0-9\s]+$");
            return reg1.IsMatch(message);
        }
        #endregion


        #region 過濾電話號碼是否有非數字的字元出現    filterPhoneNumber(string _phoneNumber)
        private string filterPhoneNumber(string _phoneNumber)
		{
            string tmpStr="";
            char[] tmpPhoneNum=_phoneNumber .ToCharArray ();
            for (int x = 0; x < tmpPhoneNum.Length; x++)
            {
                if (IsNumber(tmpPhoneNum[x].ToString()))
                    tmpStr = tmpStr + tmpPhoneNum[x].ToString();
            }
            tmpPhoneNum = null;
            return tmpStr; 
		}
		#endregion


        #region 檢查字串組成是否為純數字  IsNumber(string strNumber)
        public static bool IsNumber(string strNumber)
        {
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(@"^\d+(\.)?\d*$");
            return r.IsMatch(strNumber);
        }
        #endregion


        #region 倒轉bit（每兩個bit間倒轉） reverseBit(string message)
        /// <summary>
        /// 倒轉bit
        /// </summary>
        /// <param name="message">傳入須倒轉的字串</param>
        /// <returns>轉回倒轉後的字串</returns>
        private string reverseBit(string message)
        {
            string returnStr = "";

            for (int i = 0; i < message.Length; i += 2)
            {
                returnStr = returnStr + message.Substring(i + 1, 1) + message.Substring(i, 1);
            }
            return returnStr;
        }
        #endregion


        #region 檢查電話碼的格式，並編碼 checkPhoneNumberFormat(string _phoneNumber)
        /// <summary>
        /// 檢查電話碼的格式
        /// </summary>
        /// <param name="_phoneNumber">傳入電話格式</param>
        /// <returns>傳回本地端格式的電話號碼</returns>
        private string checkPhoneNumberFormat(string _phoneNumber)
        {
            /* 總而言之，就是要轉換為 0912345678 的型式，倒轉成 9021436587的PDU格式傳回 
            if (_phoneNumber.Substring(0, 3) == "886")
                return reverseBit("0"+_phoneNumber.Substring(3, _phoneNumber.Length - 3));  //去886 補0
            else if (_phoneNumber.Substring(0, 1) == "+")
                return reverseBit("0"+ _phoneNumber.Substring(4, _phoneNumber.Length - 4)); //去+886 補0
            else
                return reverseBit(_phoneNumber);
             */

            /* 使用國際編碼格式，再行倒轉以PDU格式傳回 */

            if (_phoneNumber.Substring(0, 1) == "+")
                return reverseBit(_phoneNumber.Substring(1, _phoneNumber.Length - 1));
            else if (_phoneNumber.Substring(0, 1) == "0")
                return reverseBit("886" + _phoneNumber.Substring(1, _phoneNumber.Length - 1));
            else
                return reverseBit(_phoneNumber);
        }
        #endregion
    }
}
