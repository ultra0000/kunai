using System.ComponentModel;
namespace Kunai.ShurikenRenderer
{
    public class CharacterMapping
    {
        private char m_Character;
        public char Character
        {
            get => m_Character;
            set
            {
                if (!string.IsNullOrEmpty(value.ToString()))
                    m_Character = value;
            }
        }

        public int Sprite { get; set; }


        public CharacterMapping(char in_C, int in_SprId)
        {
            Character = in_C;
            Sprite = in_SprId;
        }

        public CharacterMapping()
        {
            Sprite = -1;
        }
    }
}
