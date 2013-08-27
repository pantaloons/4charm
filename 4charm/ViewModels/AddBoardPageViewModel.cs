using _4charm.Models;
using _4charm.Resources;
using _4charm.Views;
using System.Linq;
using System.Windows.Input;
using System.Windows.Navigation;

namespace _4charm.ViewModels
{
    class AddBoardPageViewModel : ViewModelBase
    {
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

        public void DoAddBoard()
        {
            if (!HasBoard) return;

            if (CriticalSettingsManager.Current.Boards.Count(x => x.Name == Board.Name) != 1)
            {
                CriticalSettingsManager.Current.Boards.Add(Board);
            }

            BoardsPage.SetBoard = Board;
            GoBack();
        }

        public void TextChanged(string text)
        {
            if (Board != null) Board.UnloadImage();

            if (BoardList.Boards.ContainsKey(text))
            {
                Board = new BoardViewModel(new Board(BoardList.Boards[text].Name, BoardList.Boards[text].Description, BoardList.Boards[text].IsNSFW));
                Board.LoadImage();
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
    }
}
