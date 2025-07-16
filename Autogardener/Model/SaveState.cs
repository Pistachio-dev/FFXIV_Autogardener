namespace Autogardener.Model
{
    public class SaveState
    {

        public CharacterSaveState GetCharacterSaveState(string fullPlayerName)
        {
            if (!SaveStates.ContainsKey(fullPlayerName))
            {
                SaveStates[fullPlayerName] = new CharacterSaveState();
            }

            return SaveStates[fullPlayerName];
        }

        // The key is the character full name "John Smith@Omega"
        public Dictionary<string, CharacterSaveState> SaveStates { get; set; } = new();
    }
}
