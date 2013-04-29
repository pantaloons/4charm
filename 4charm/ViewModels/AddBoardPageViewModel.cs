using _4charm.Models;
using _4charm.Resources;
using _4charm.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace _4charm.ViewModels
{
    class AddBoardPageViewModel : ViewModelBase
    {
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

        public bool HasBoard
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }


        private Board _board;

        public void OnNavigatedTo()
        {
            if (Board != null) Board.LoadImage();
        }

        public void OnNavigatedFrom(NavigationEventArgs e)
        {
            if(e.IsNavigationInitiator) UnloadBoard();
        }

        public void Complete()
        {
            if(!HasBoard) return;

            if (SettingsManager.Current.Boards.Count(x => x.Name == _board.Name) != 1)
            {
                SettingsManager.Current.Boards.Add(new BoardViewModel(_board));
            }

            BoardsPage.SetBoard = Board;
            ClearBoard();
            GoBack();
        }

        public void TextUpdated(string text)
        {
            if(BoardList.Boards.ContainsKey(text))
            {
                _board = new Board(BoardList.Boards[text].Name, BoardList.Boards[text].Description, BoardList.Boards[text].IsNSFW);
                SetBoard(_board);
            }
            else ClearBoard();
        }

        private void SetBoard(Board b)
        {
            Board = new BoardViewModel(b);
            Board.LoadImage();
            NSFWText = b.IsNSFW ? AppResources.AddBoardPage_NSFW : "";
            HasBoard = true;
        }

        private void UnloadBoard()
        {
            if (Board != null) Board.UnloadImage();
        }

        private void ClearBoard()
        {
            UnloadBoard();
            Board = null;

            _board = null;
            NSFWText = "";
            HasBoard = false;
        }
    }
}
