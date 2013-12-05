using _4charm.Models;
using _4charm.Resources;
using _4charm.Views;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace _4charm.ViewModels
{
    class AddBoardPageViewModel : PageViewModelBase
    {
        public string Name
        {
            get { return GetProperty<string>(); }
            set
            {
                SetProperty(value);
                NameChanged();
            }
        }

        public int SelectionStart
        {
            get { return GetProperty<int>(); }
            set { SetProperty(value); }
        }

        public bool HasBoard
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public BoardViewModel Board
        {
            get { return GetProperty<BoardViewModel>(); }
            set { SetProperty(value); }
        }

        public string NSFWText
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public ICommand AddBoard
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public AddBoardPageViewModel()
        {
            AddBoard = new ModelCommand(DoAddBoard);
        }

        private void NameChanged()
        {
            int pos = Math.Min(Name.Length, SelectionStart);
            for (int i = 0; i < pos; i++)
            {
                if (!char.IsLetterOrDigit(Name[i]))
                {
                    pos--;
                }
            }
            string replace = Regex.Replace(Name.ToLower(), "[^a-z0-9]", "");
            if (Name != replace)
            {
                Name = replace;
                SelectionStart = pos;
            }

            if (BoardList.Boards.ContainsKey(Name))
            {
                Board = new BoardViewModel(ThreadCache.Current.EnforceBoard(BoardList.Boards[Name].Name));
                NSFWText = Board.IsNSFW ? AppResources.AddBoardPage_NSFW : string.Empty;
                HasBoard = true;
            }
            else
            {
                Board = null;
                NSFWText = "";
                HasBoard = false;
            }
        }

        public void DoAddBoard()
        {
            if (!HasBoard) return;

            if (CriticalSettingsManager.Current.Boards.Count(x => x.Name == Board.Name) != 1)
            {
                CriticalSettingsManager.Current.Boards.Add(ThreadCache.Current.EnforceBoard(Name));
            }

            BoardsPage.SetBoard = Board;
            GoBack();
        }
    }
}
