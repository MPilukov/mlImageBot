namespace MLImageBot.Logic
{
    public static class EActionExtension
    {
        public static string GetName(this EAction action)
        {
            switch (action) 
            {
                case EAction.ChangeLabel:
                    return "Изменить описание";            
            }

            throw new System.Exception($"Не удалось определить описание действия : {action}");
        }
    }
}