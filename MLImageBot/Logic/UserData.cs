using System.Collections.Generic;

namespace MLImageBot.Logic
{
    public class UserData
    {
        public Dictionary<EAction, string> PreData;
        public Stack<EAction> Actions;

        public UserData()
        {
            PreData = new Dictionary<EAction, string>();
            Actions = new Stack<EAction>();
        }
    }
}