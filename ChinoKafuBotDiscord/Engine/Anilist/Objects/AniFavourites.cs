using System;
using System.Collections.Generic;
using System.Text;

namespace AnimeListBot.Handler.Anilist
{
    public class AniFavourites
    {
        public AniMediaConnection anime { get; set; }
        public AniMediaConnection manga { get; set; }

        public AniCharacterConnection characters { get; set; }
    }
}
