using System.Collections.Generic;

namespace _4charm.Models
{
    /// <summary>
    /// This is the global listing of all boards that can be linked to,
    /// added, removed, etc. On first launch the all listing is populated
    /// out of the SFW boards from this list.
    /// </summary>
    class BoardList
    {
        /// <summary>
        /// Board listing. Additions and deletions should add/remove corresponding
        /// fanart and icon from the assets.
        /// </summary>
        public static Dictionary<string, BoardID> Boards = new Dictionary<string, BoardID>
        {
            {"a", new BoardID("a", "Anime & Manga", false)},
            {"b", new BoardID("b", "Random", true)},
            {"c", new BoardID("c", "Anime/Cute", false)},
            {"d", new BoardID("d", "Hentai/Alternative", true)},
            {"e", new BoardID("e", "Ecchi", true)},
            {"g", new BoardID("g", "Technology", false)},
            {"gif", new BoardID("gif", "Adult GIF", true)},
            {"h", new BoardID("h", "Hentai", true)},
            {"hr", new BoardID("hr", "High Resolution", true)},
            {"k", new BoardID("k", "Weapons", true)},
            {"m", new BoardID("m", "Mecha", false)},
            {"o", new BoardID("o", "Auto", false)},
            {"p", new BoardID("p", "Photography", false)},
            {"r", new BoardID("r", "Request", true)},
            {"s", new BoardID("s", "Sexy Beautiful Women", true)},
            {"t", new BoardID("t", "Torrents", true)},
            {"u", new BoardID("u", "Yuri", true)},
            {"v", new BoardID("v", "Video Games", false)},
            {"vg", new BoardID("vg", "Video Game Generals", false)},
            {"vr", new BoardID("vr", "Retro Games", false)},
            {"w", new BoardID("w", "Anime/Wallpapers", false)},
            {"wg", new BoardID("wg", "Wallpapers/General", false)},

            {"i", new BoardID("i", "Oekaki", false)},
            {"ic", new BoardID("ic", "Artwork/Critique", false)},

            {"r9k", new BoardID("r9k", "ROBOT9001", true)},

            {"s4s", new BoardID("s4s", "Shit 4chan Says", true)},

            {"cm", new BoardID("cm", "Cute/Male", false)},
            {"hm", new BoardID("hm", "Handsome Men", true)},
            {"lgbt", new BoardID("lgbt", "Lesbian, Gay, Bisexual, & Transgender", false)},
            {"y", new BoardID("y", "Yaoi", true)},

            {"3", new BoardID("3", "3DCG", false)},
            {"adv", new BoardID("adv", "Advice", false)},
            {"an", new BoardID("an", "Animals & Nature", false)},
            {"asp", new BoardID("asp", "Alternative Sports", false)},
            {"biz", new BoardID("biz", "Business & Finanace", false)},
            {"cgl", new BoardID("cgl", "Cosplay & EGL", false)},
            {"ck", new BoardID("ck", "Food & Cooking", false)},
            {"co", new BoardID("co", "Comics & Cartoons", false)},
            {"diy", new BoardID("diy", "Do-It-Yourself", false)},
            {"fa", new BoardID("fa", "Fashion", false)},
            {"fit", new BoardID("fit", "Fitness", false)},
            {"gd", new BoardID("gd", "Graphic Design", false)},
            {"hc", new BoardID("hc", "Hardcore", true)},
            {"int", new BoardID("int", "International", false)},
            {"jp", new BoardID("jp", "Otaku Culture", false)},
            {"lit", new BoardID("lit", "Literature", false)},
            {"mlp", new BoardID("mlp", "Pony", false)},
            {"mu", new BoardID("mu", "Music", false)},
            {"n", new BoardID("n", "Transportation", false)},
            {"out", new BoardID("out", "Outdoors", false)},
            {"po", new BoardID("po", "Papercraft & Origami", false)},
            {"pol", new BoardID("pol", "Politically Incorrect", true)},
            {"sci", new BoardID("sci", "Science & Math", false)},
            {"soc", new BoardID("soc", "Cams & Meetups", true)},
            {"sp", new BoardID("sp", "Sports", false)},
            {"tg", new BoardID("tg", "Traditional Games", false)},
            {"toy", new BoardID("toy", "Toys", false)},
            {"trv", new BoardID("trv", "Travel", false)},
            {"tv", new BoardID("tv", "Television & Film", false)},
            {"vp", new BoardID("vp", "Pokémon", false)},
            {"wsg", new BoardID("wsg", "Worksafe GIF", false)},
            {"x", new BoardID("x", "Paranormal", false)}
        };
    }
}
