using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace _4charm.Models
{
    class ThreadCache
    {
        private static ThreadCache _current;
        public static ThreadCache Current
        {
            get
            {
                if (_current == null) _current = new ThreadCache();
                return _current;
            }
        }

        public Dictionary<string, Board> Boards { get; set; }

        private ThreadCache()
        {
            Boards = new Dictionary<string, Board>();
        }

        public Board EnforceBoard(string name)
        {
            if (!Boards.ContainsKey(name))
            {
                Boards[name] = new Board(BoardList.Boards[name].Name, BoardList.Boards[name].Description, BoardList.Boards[name].IsNSFW);
            }
            return Boards[name];
        }
    }
}
