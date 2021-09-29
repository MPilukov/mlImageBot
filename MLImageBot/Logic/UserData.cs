using System.Collections.Generic;

namespace MLImageBot.Logic
{
    public class UserData
    {
        public readonly Dictionary<EAction, string> PreData;
        public readonly Stack<EAction> Actions;

        public UserData()
        {
            PreData = new Dictionary<EAction, string>();
            Actions = new Stack<EAction>();
        }
    }
}