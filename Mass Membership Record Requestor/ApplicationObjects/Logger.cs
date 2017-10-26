using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mass_Membership_Record_Requestor.ApplicationObjects
{
    public class Logger
    {
        public void LogRequestError(Exception e, MembershipRecordInfo failedMemberRequestInfo)
        {
            using (var fileStream = File.AppendText("recordFailLog.txt"))
            {
                fileStream.WriteLine(String.Format("{0}\t{1}\t{2}", failedMemberRequestInfo.Name, e.Message, e.StackTrace));
            }
        }

    }
}
