using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emailer
{
    public class EmailResponse
    {
        bool successfullySent = true;
        string message = "";

        public bool SuccessfullySent
        {
            get { return successfullySent; }
            set { successfullySent = value; }
        }

        public string Message
        {
            get { return message; }
            set { message = value; }
        }
    }
}
