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
    
    public partial class tblMessageLog
    {
        public long seq { get; set; }
        public System.DateTime timestamp { get; set; }
        public string type { get; set; }
        public string message { get; set; }
        public string userid_tel { get; set; }
        public string name { get; set; }
        public string token { get; set; }
        public bool is_success { get; set; }
    }
}