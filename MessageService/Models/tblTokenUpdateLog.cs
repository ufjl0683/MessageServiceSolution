//------------------------------------------------------------------------------
// <auto-generated>
//     這個程式碼是由範本產生。
//
//     對這個檔案進行手動變更可能導致您的應用程式產生未預期的行為。
//     如果重新產生程式碼，將會覆寫對這個檔案的手動變更。
// </auto-generated>
//------------------------------------------------------------------------------

namespace TaiPower.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class tblTokenUpdateLog
    {
        public long seq { get; set; }
        public System.DateTime timestamp { get; set; }
        public string userid { get; set; }
        public string name { get; set; }
        public string token { get; set; }
        public string cmd { get; set; }
        public bool is_success { get; set; }
        public string error_message { get; set; }
    }
}