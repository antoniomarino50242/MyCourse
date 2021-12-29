using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyCourse.Models.Exceptions.Application
{
    public class InvalidVoteException : Exception
    {
        public InvalidVoteException(int vote) : base($"Il voto {vote} non è valido")
        {
        }
    }
}