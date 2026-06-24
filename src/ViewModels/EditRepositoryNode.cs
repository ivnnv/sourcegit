using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SourceGit.ViewModels
{
    public class EditRepositoryNode : Popup
    {
        public string Target
        {
            get;
        }

        public bool IsRepository
        {
            get;
        }

        [Required(ErrorMessage = "Name is required!")]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value, true);
        }

        public int Bookmark
        {
            get => _bookmark;
            set => SetProperty(ref _bookmark, value);
        }

        // [fork:colored-tabs] Custom bookmark color (used when Bookmark == -1)
        public uint BookmarkCustom
        {
            get => _bookmarkCustom;
            set => SetProperty(ref _bookmarkCustom, value);
        }

        // [fork:colored-tabs] Tab color preset index (0 = none, 1..N preset, -1 custom)
        public int TabColor
        {
            get => _tabColor;
            set => SetProperty(ref _tabColor, value);
        }

        public uint TabColorCustom
        {
            get => _tabColorCustom;
            set => SetProperty(ref _tabColorCustom, value);
        }

        public EditRepositoryNode(RepositoryNode node)
        {
            _node = node;
            _name = node.Name;
            _bookmark = node.Bookmark;
            _bookmarkCustom = node.BookmarkCustom;
            _tabColor = node.TabColor;
            _tabColorCustom = node.TabColorCustom;

            Target = node.IsRepository ? node.Id : node.Name;
            IsRepository = node.IsRepository;
        }

        public override Task<bool> Sure()
        {
            bool needSort = _node.Name != _name;
            _node.Name = _name;
            _node.Bookmark = _bookmark;
            // [fork:colored-tabs] Persist custom color + tab color alongside bookmark
            _node.BookmarkCustom = _bookmarkCustom;
            _node.TabColor = _tabColor;
            _node.TabColorCustom = _tabColorCustom;

            if (needSort)
            {
                Preferences.Instance.SortByRenamedNode(_node);
                Welcome.Instance.Refresh();
            }

            return Task.FromResult(true);
        }

        private RepositoryNode _node = null;
        private string _name = null;
        private int _bookmark = 0;
        // [fork:colored-tabs] Backing fields for custom bookmark + tab color
        private uint _bookmarkCustom = 0;
        private int _tabColor = 0;
        private uint _tabColorCustom = 0;
    }
}
