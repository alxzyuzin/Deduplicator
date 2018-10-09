using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deduplicator.Common
{
    public enum ErrorType
    {
        UnknownError,
        FileNotFound
    }

    

    public class Error
    {
        public Error() { }

        public Error(ErrorType type, string message)
        {
            Type = type;
            Message = message;
        } 

        public ErrorType Type;
        public string Message; 
    }
}
