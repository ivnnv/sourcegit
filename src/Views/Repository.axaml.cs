using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace SourceGit.Views
{
    public partial class Repository : UserControl
    {
        public Repository()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            // [fork:sidebar-section-reorder] Apply persisted order + sync selection + subscribe to VM changes
            ApplySidebarViewOrder();
            SyncViewSwitcherSelection();
            UpdateLeftSidebarLayout();

            if (DataContext is ViewModels.Repository repo)
                repo.PropertyChanged += OnRepositoryPropertyChanged;
        }

        private void OnToggleFilter(object _, RoutedEventArgs e)
        {
            FilterBox.Focus();
            e.Handled = true;
        }

        // [fork:sidebar-section-reorder] Unsubscribe to avoid leaks on unmount
        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);

            if (DataContext is ViewModels.Repository repo)
                repo.PropertyChanged -= OnRepositoryPropertyChanged;
        }

        // [fork:sidebar-section-reorder] Re-sync ListBox selection when VM's SelectedViewIndex changes externally
        private void OnRepositoryPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModels.Repository.SelectedViewIndex))
                SyncViewSwitcherSelection();
        }

        // [fork:sidebar-section-reorder] All drag-drop reorder logic for the Histories/WorkingCopy/Stashes ListBox
        #region View Switcher Drag-Drop Reorder

        private static readonly DataFormat<string> _dndViewSwitcherFormat =
            DataFormat.CreateStringApplicationFormat("sourcegit-dnd-view-switcher");

        private PointerPressedEventArgs _pressedViewItemEvent = null;
        private bool _startDragViewItem = false;

        private void ApplySidebarViewOrder()
        {
            var layout = ViewModels.Preferences.Instance.Layout;
            var order = layout.SidebarViewOrder;
            if (order == null || order.Count != 3)
                return;

            // Collect items by their Tag
            var itemsByTag = new Dictionary<int, ListBoxItem>();
            foreach (var obj in ViewSwitcher.Items)
            {
                if (obj is ListBoxItem item && item.Tag is string tagStr && int.TryParse(tagStr, out var tag))
                    itemsByTag[tag] = item;
            }

            if (itemsByTag.Count != 3)
                return;

            // Reorder
            ViewSwitcher.Items.Clear();
            foreach (var tag in order)
            {
                if (itemsByTag.TryGetValue(tag, out var item))
                    ViewSwitcher.Items.Add(item);
            }
        }

        private void SyncViewSwitcherSelection()
        {
            if (DataContext is not ViewModels.Repository repo)
                return;

            // Find the ListBoxItem whose Tag matches the current SelectedViewIndex
            foreach (var obj in ViewSwitcher.Items)
            {
                if (obj is ListBoxItem item && item.Tag is string tagStr &&
                    int.TryParse(tagStr, out var tag) && tag == repo.SelectedViewIndex)
                {
                    _suppressSelectionChanged = true;
                    ViewSwitcher.SelectedItem = item;
                    _suppressSelectionChanged = false;
                    break;
                }
            }
        }

        private bool _suppressSelectionChanged = false;

        private void OnViewSwitcherSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionChanged)
                return;

            if (DataContext is ViewModels.Repository repo &&
                ViewSwitcher.SelectedItem is ListBoxItem item &&
                item.Tag is string tagStr &&
                int.TryParse(tagStr, out var viewIndex))
            {
                repo.SelectedViewIndex = viewIndex;
            }
        }

        private static ListBoxItem FindParentListBoxItem(Control control)
        {
            var parent = control.Parent;
            while (parent != null)
            {
                if (parent is ListBoxItem item)
                    return item;
                parent = (parent as Control)?.Parent;
            }
            return null;
        }

        private void OnViewSwitcherItemPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (sender is Border border)
            {
                _pressedViewItemEvent = e;
                _startDragViewItem = false;
                e.Handled = true;
            }
        }

        private void OnViewSwitcherItemPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            _pressedViewItemEvent = null;
            _startDragViewItem = false;
        }

        private async void OnViewSwitcherItemPointerMoved(object sender, PointerEventArgs e)
        {
            if (_pressedViewItemEvent == null || _startDragViewItem || sender is not Border border)
                return;

            var delta = e.GetPosition(border) - _pressedViewItemEvent.GetPosition(border);
            var sizeSquared = delta.X * delta.X + delta.Y * delta.Y;
            if (sizeSquared < 64)
                return;

            _startDragViewItem = true;

            var listBoxItem = FindParentListBoxItem(border);
            if (listBoxItem == null)
                return;

            var tag = listBoxItem.Tag as string ?? "";
            var data = new DataTransfer();
            data.Add(DataTransferItem.Create(_dndViewSwitcherFormat, tag));
            await DragDrop.DoDragDropAsync(_pressedViewItemEvent, data, DragDropEffects.Move);
        }

        private void OnViewSwitcherItemDrop(object sender, DragEventArgs e)
        {
            if (e.DataTransfer.TryGetValue(_dndViewSwitcherFormat) is not { Length: > 0 } sourceTag)
                return;

            if (sender is not ListBoxItem targetItem || targetItem.Tag is not string targetTag)
                return;

            if (sourceTag == targetTag)
                return;

            // Find source and target indices in current Items list
            var items = ViewSwitcher.Items.Cast<ListBoxItem>().ToList();
            var sourceIdx = items.FindIndex(i => (i.Tag as string) == sourceTag);
            var targetIdx = items.FindIndex(i => (i.Tag as string) == targetTag);

            if (sourceIdx < 0 || targetIdx < 0)
                return;

            // Swap
            var sourceItem = items[sourceIdx];
            items.RemoveAt(sourceIdx);
            items.Insert(targetIdx, sourceItem);

            // Rebuild ListBox
            _suppressSelectionChanged = true;
            var selectedItem = ViewSwitcher.SelectedItem;
            ViewSwitcher.Items.Clear();
            foreach (var item in items)
                ViewSwitcher.Items.Add(item);
            ViewSwitcher.SelectedItem = selectedItem;
            _suppressSelectionChanged = false;

            // Persist order
            var newOrder = items.Select(i => int.Parse(i.Tag as string ?? "0")).ToList();
            var layout = ViewModels.Preferences.Instance.Layout;
            layout.SidebarViewOrder = newOrder;
            ViewModels.Preferences.Instance.Save();

            _pressedViewItemEvent = null;
            _startDragViewItem = false;
            e.Handled = true;
        }

        #endregion

        private void OnSearchCommitPanelPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == IsVisibleProperty && sender is Grid { IsVisible: true })
                TxtSearchCommitsBox.Focus();
        }

        private void OnSearchKeyDown(object _, KeyEventArgs e)
        {
            if (DataContext is not ViewModels.Repository repo)
                return;

            if (e.Key == Key.Enter)
            {
                repo.SearchCommitContext.StartSearch();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                if (repo.SearchCommitContext.Suggestions is { Count: > 0 })
                {
                    SearchSuggestionBox.Focus(NavigationMethod.Tab);
                    SearchSuggestionBox.SelectedIndex = 0;
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                repo.SearchCommitContext.ClearSuggestions();
                e.Handled = true;
            }
        }

        private void OnClearSearchCommitFilter(object _, RoutedEventArgs e)
        {
            if (DataContext is not ViewModels.Repository repo)
                return;

            repo.SearchCommitContext.ClearFilter();
            e.Handled = true;
        }

        private void OnLocalBranchTreeSelectionChanged(object _1, RoutedEventArgs _2)
        {
            RemoteBranchTree.UnselectAll();
            TagsList.UnselectAll();
        }

        private void OnRemoteBranchTreeSelectionChanged(object _1, RoutedEventArgs _2)
        {
            LocalBranchTree.UnselectAll();
            TagsList.UnselectAll();
        }

        private void OnTagsSelectionChanged(object _1, RoutedEventArgs _2)
        {
            LocalBranchTree.UnselectAll();
            RemoteBranchTree.UnselectAll();
        }

        private void OnWorktreeContextRequested(object sender, ContextRequestedEventArgs e)
        {
            if (sender is Control { DataContext: ViewModels.Worktree worktree } ctrl && DataContext is ViewModels.Repository repo)
            {
                var menu = new ContextMenu();

                var switchTo = new MenuItem();
                switchTo.Header = App.Text("Worktree.Open");
                switchTo.Icon = this.CreateMenuIcon("Icons.Folder.Open");
                switchTo.Click += (_, ev) =>
                {
                    repo.OpenWorktree(worktree);
                    ev.Handled = true;
                };
                menu.Items.Add(switchTo);
                menu.Items.Add(new MenuItem() { Header = "-" });

                if (worktree.IsLocked)
                {
                    var unlock = new MenuItem();
                    unlock.Header = App.Text("Worktree.Unlock");
                    unlock.Icon = this.CreateMenuIcon("Icons.Unlock");
                    unlock.Click += async (_, ev) =>
                    {
                        await repo.UnlockWorktreeAsync(worktree);
                        ev.Handled = true;
                    };
                    menu.Items.Add(unlock);
                }
                else
                {
                    var loc = new MenuItem();
                    loc.Header = App.Text("Worktree.Lock");
                    loc.Icon = this.CreateMenuIcon("Icons.Lock");
                    loc.IsEnabled = !worktree.IsMain;
                    loc.Click += async (_, ev) =>
                    {
                        await repo.LockWorktreeAsync(worktree);
                        ev.Handled = true;
                    };
                    menu.Items.Add(loc);
                }

                var remove = new MenuItem();
                remove.Header = App.Text("Worktree.Remove");
                remove.Icon = this.CreateMenuIcon("Icons.Clear");
                remove.IsEnabled = !worktree.IsCurrent && !worktree.IsMain;
                remove.Click += (_, ev) =>
                {
                    if (repo.CanCreatePopup())
                        repo.ShowPopup(new ViewModels.RemoveWorktree(repo, worktree));
                    ev.Handled = true;
                };
                menu.Items.Add(remove);

                var copy = new MenuItem();
                copy.Header = App.Text("Worktree.CopyPath");
                copy.Icon = this.CreateMenuIcon("Icons.Copy");
                copy.Click += async (_, ev) =>
                {
                    await this.CopyTextAsync(worktree.FullPath);
                    ev.Handled = true;
                };
                menu.Items.Add(new MenuItem() { Header = "-" });
                menu.Items.Add(copy);
                menu.Open(ctrl);
            }

            e.Handled = true;
        }

        private void OnWorktreeDoubleTapped(object sender, TappedEventArgs e)
        {
            if (sender is Control { DataContext: ViewModels.Worktree worktree } && DataContext is ViewModels.Repository repo)
                repo.OpenWorktree(worktree);

            e.Handled = true;
        }

        private void OnWorktreeListPropertyChanged(object _, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ItemsControl.ItemsSourceProperty || e.Property == IsVisibleProperty)
                UpdateLeftSidebarLayout();
        }

        private void OnLeftSidebarRowsChanged(object _, RoutedEventArgs e)
        {
            UpdateLeftSidebarLayout();
            e.Handled = true;
        }

        private void OnLeftSidebarSizeChanged(object _, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
                UpdateLeftSidebarLayout();
        }

        private void UpdateLeftSidebarLayout()
        {
            var vm = DataContext as ViewModels.Repository;
            if (vm?.Settings == null)
                return;

            if (!IsLoaded)
                return;

            var leftHeight = LeftSidebarGroups.Bounds.Height - 28.0 * 5 - 4;
            if (leftHeight <= 0)
                return;

            var localBranchRows = vm.IsLocalBranchGroupExpanded ? LocalBranchTree.Rows.Count : 0;
            var remoteBranchRows = vm.IsRemoteGroupExpanded ? RemoteBranchTree.Rows.Count : 0;
            var desiredBranches = (localBranchRows + remoteBranchRows) * 24.0;
            var desiredTag = vm.IsTagGroupExpanded ? 24.0 * TagsList.Rows : 0;
            var desiredSubmodule = vm.IsSubmoduleGroupExpanded ? 24.0 * SubmoduleList.Rows : 0;
            var desiredWorktree = vm.IsWorktreeGroupExpanded ? 24.0 * vm.Worktrees.Count : 0;
            var desiredOthers = desiredTag + desiredSubmodule + desiredWorktree;
            var hasOverflow = (desiredBranches + desiredOthers > leftHeight);

            if (vm.IsWorktreeGroupExpanded)
            {
                var height = desiredWorktree;
                if (hasOverflow)
                {
                    var test = leftHeight - desiredBranches - desiredTag - desiredSubmodule;
                    if (test < 0)
                        height = Math.Min(120, height);
                    else
                        height = Math.Max(120, test);
                }

                leftHeight -= height;
                WorktreeList.Height = height;
                hasOverflow = (desiredBranches + desiredTag + desiredSubmodule) > leftHeight;
            }

            if (vm.IsSubmoduleGroupExpanded)
            {
                var height = desiredSubmodule;
                if (hasOverflow)
                {
                    var test = leftHeight - desiredBranches - desiredTag;
                    if (test < 0)
                        height = Math.Min(120, height);
                    else
                        height = Math.Max(120, test);
                }

                leftHeight -= height;
                SubmoduleList.Height = height;
                hasOverflow = (desiredBranches + desiredTag) > leftHeight;
            }

            if (vm.IsTagGroupExpanded)
            {
                var height = desiredTag;
                if (hasOverflow)
                {
                    var test = leftHeight - desiredBranches;
                    if (test < 0)
                        height = Math.Min(120, height);
                    else
                        height = Math.Max(120, test);
                }

                leftHeight -= height;
                TagsList.Height = height;
            }

            if (leftHeight > 0 && desiredBranches > leftHeight)
            {
                var local = localBranchRows * 24.0;
                var remote = remoteBranchRows * 24.0;
                var half = leftHeight / 2;
                if (vm.IsLocalBranchGroupExpanded)
                {
                    if (vm.IsRemoteGroupExpanded)
                    {
                        if (local < half)
                        {
                            LocalBranchTree.Height = local;
                            RemoteBranchTree.Height = leftHeight - local;
                        }
                        else if (remote < half)
                        {
                            RemoteBranchTree.Height = remote;
                            LocalBranchTree.Height = leftHeight - remote;
                        }
                        else
                        {
                            LocalBranchTree.Height = half;
                            RemoteBranchTree.Height = half;
                        }
                    }
                    else
                    {
                        LocalBranchTree.Height = leftHeight;
                    }
                }
                else if (vm.IsRemoteGroupExpanded)
                {
                    RemoteBranchTree.Height = leftHeight;
                }
            }
            else
            {
                if (vm.IsLocalBranchGroupExpanded)
                {
                    var height = localBranchRows * 24;
                    LocalBranchTree.Height = height;
                }

                if (vm.IsRemoteGroupExpanded)
                {
                    var height = remoteBranchRows * 24;
                    RemoteBranchTree.Height = height;
                }
            }
        }

        private void OnSearchSuggestionBoxKeyDown(object _, KeyEventArgs e)
        {
            if (DataContext is not ViewModels.Repository repo)
                return;

            if (e.Key == Key.Escape)
            {
                repo.SearchCommitContext.ClearSuggestions();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                var selected = SearchSuggestionBox.SelectedItem;
                if (selected is string content)
                {
                    repo.SearchCommitContext.Filter = content;
                    TxtSearchCommitsBox.CaretIndex = content.Length;
                }
                else if (selected is Models.User user)
                {
                    var apply = user.ToString().EscapeForBRE();
                    repo.SearchCommitContext.Filter = apply;
                    TxtSearchCommitsBox.CaretIndex = apply.Length;
                }

                repo.SearchCommitContext.StartSearch();
                e.Handled = true;
            }
        }

        private void OnSearchSuggestionTapped(object sender, TappedEventArgs e)
        {
            if (DataContext is not ViewModels.Repository repo)
                return;

            var ctx = (sender as Control)?.DataContext;
            if (ctx is string content)
            {
                repo.SearchCommitContext.Filter = content;
                TxtSearchCommitsBox.CaretIndex = content.Length;
            }
            else if (ctx is Models.User user)
            {
                var apply = user.ToString().EscapeForBRE();
                repo.SearchCommitContext.Filter = apply;
                TxtSearchCommitsBox.CaretIndex = apply.Length;
            }

            repo.SearchCommitContext.StartSearch();
            e.Handled = true;
        }

        private void OnOpenAdvancedHistoriesOption(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && DataContext is ViewModels.Repository { Histories: { } histories } repo)
            {
                var pref = ViewModels.Preferences.Instance;

                var layout = new MenuItem();
                layout.Header = App.Text("Repository.HistoriesLayout");
                layout.IsEnabled = false;

                var isHorizontal = pref.UseTwoColumnsLayoutInHistories;
                var horizontal = new MenuItem();
                horizontal.Header = App.Text("Repository.HistoriesLayout.Horizontal");
                if (isHorizontal)
                    horizontal.Icon = this.CreateMenuIcon("Icons.Check");
                horizontal.Click += (_, ev) =>
                {
                    pref.UseTwoColumnsLayoutInHistories = true;
                    ev.Handled = true;
                };

                var vertical = new MenuItem();
                vertical.Header = App.Text("Repository.HistoriesLayout.Vertical");
                if (!isHorizontal)
                    vertical.Icon = this.CreateMenuIcon("Icons.Check");
                vertical.Click += (_, ev) =>
                {
                    pref.UseTwoColumnsLayoutInHistories = false;
                    ev.Handled = true;
                };

                var showFlags = new MenuItem();
                showFlags.Header = App.Text("Repository.ShowFlags");
                showFlags.IsEnabled = false;

                var reflog = new MenuItem();
                reflog.Header = App.Text("Repository.ShowLostCommits");
                reflog.Tag = "--reflog";
                if (repo.HistoryShowFlags.HasFlag(Models.HistoryShowFlags.Reflog))
                    reflog.Icon = this.CreateMenuIcon("Icons.Check");
                reflog.Click += (_, ev) =>
                {
                    repo.ToggleHistoryShowFlag(Models.HistoryShowFlags.Reflog);
                    ev.Handled = true;
                };

                var firstParentOnly = new MenuItem();
                firstParentOnly.Header = App.Text("Repository.ShowFirstParentOnly");
                firstParentOnly.Tag = "--first-parent";
                if (repo.HistoryShowFlags.HasFlag(Models.HistoryShowFlags.FirstParentOnly))
                    firstParentOnly.Icon = this.CreateMenuIcon("Icons.Check");
                firstParentOnly.Click += (_, ev) =>
                {
                    repo.ToggleHistoryShowFlag(Models.HistoryShowFlags.FirstParentOnly);
                    ev.Handled = true;
                };

                var simplifyByDecoration = new MenuItem();
                simplifyByDecoration.Header = App.Text("Repository.ShowDecoratedCommitsOnly");
                simplifyByDecoration.Tag = "--simplify-by-decoration";
                if (repo.HistoryShowFlags.HasFlag(Models.HistoryShowFlags.SimplifyByDecoration))
                    simplifyByDecoration.Icon = this.CreateMenuIcon("Icons.Check");
                simplifyByDecoration.Click += (_, ev) =>
                {
                    repo.ToggleHistoryShowFlag(Models.HistoryShowFlags.SimplifyByDecoration);
                    ev.Handled = true;
                };

                var order = new MenuItem();
                order.Header = App.Text("Repository.HistoriesOrder");
                order.IsEnabled = false;

                var dateOrder = new MenuItem();
                dateOrder.Header = App.Text("Repository.HistoriesOrder.ByDate");
                dateOrder.Tag = "--date-order";
                if (!repo.EnableTopoOrderInHistory)
                    dateOrder.Icon = this.CreateMenuIcon("Icons.Check");
                dateOrder.Click += (_, ev) =>
                {
                    repo.EnableTopoOrderInHistory = false;
                    ev.Handled = true;
                };

                var topoOrder = new MenuItem();
                topoOrder.Header = App.Text("Repository.HistoriesOrder.Topo");
                topoOrder.Tag = "--topo-order";
                if (repo.EnableTopoOrderInHistory)
                    topoOrder.Icon = this.CreateMenuIcon("Icons.Check");
                topoOrder.Click += (_, ev) =>
                {
                    repo.EnableTopoOrderInHistory = true;
                    ev.Handled = true;
                };

                var highlights = new MenuItem();
                highlights.Header = App.Text("Histories.HighlightsInGraph");
                highlights.IsEnabled = false;

                var all = new MenuItem();
                all.Header = App.Text("Histories.HighlightsInGraph.All");
                if (histories.GraphHighlighting == Models.CommitGraphHighlighting.All)
                    all.Icon = this.CreateMenuIcon("Icons.Check");
                all.Click += (_, ev) =>
                {
                    histories.GraphHighlighting = Models.CommitGraphHighlighting.All;
                    ev.Handled = true;
                };

                var currentBranchOnly = new MenuItem();
                currentBranchOnly.Header = App.Text("Histories.HighlightsInGraph.CurrentBranchOnly");
                if (histories.GraphHighlighting == Models.CommitGraphHighlighting.CurrentBranchOnly)
                    currentBranchOnly.Icon = this.CreateMenuIcon("Icons.Check");
                currentBranchOnly.Click += (_, ev) =>
                {
                    histories.GraphHighlighting = Models.CommitGraphHighlighting.CurrentBranchOnly;
                    ev.Handled = true;
                };

                var selectedCommitsOnly = new MenuItem();
                selectedCommitsOnly.Header = App.Text("Histories.HighlightsInGraph.SelectedCommitsOnly");
                if (histories.GraphHighlighting == Models.CommitGraphHighlighting.SelectedCommitsOnly)
                    selectedCommitsOnly.Icon = this.CreateMenuIcon("Icons.Check");
                selectedCommitsOnly.Click += (_, ev) =>
                {
                    histories.GraphHighlighting = Models.CommitGraphHighlighting.SelectedCommitsOnly;
                    ev.Handled = true;
                };

                var currentBranchAndSelectedCommits = new MenuItem();
                currentBranchAndSelectedCommits.Header = App.Text("Histories.HighlightsInGraph.CurrentBranchAndSelectedCommits");
                if (histories.GraphHighlighting == Models.CommitGraphHighlighting.CurrentBranchAndSelectedCommits)
                    currentBranchAndSelectedCommits.Icon = this.CreateMenuIcon("Icons.Check");
                currentBranchAndSelectedCommits.Click += (_, ev) =>
                {
                    histories.GraphHighlighting = Models.CommitGraphHighlighting.CurrentBranchAndSelectedCommits;
                    ev.Handled = true;
                };

                var menu = new ContextMenu();
                menu.Placement = PlacementMode.BottomEdgeAlignedLeft;
                menu.Items.Add(layout);
                menu.Items.Add(horizontal);
                menu.Items.Add(vertical);
                menu.Items.Add(new MenuItem() { Header = "-" });
                menu.Items.Add(showFlags);
                menu.Items.Add(reflog);
                menu.Items.Add(firstParentOnly);
                menu.Items.Add(simplifyByDecoration);
                menu.Items.Add(new MenuItem() { Header = "-" });
                menu.Items.Add(order);
                menu.Items.Add(dateOrder);
                menu.Items.Add(topoOrder);
                menu.Items.Add(new MenuItem() { Header = "-" });
                menu.Items.Add(highlights);
                menu.Items.Add(all);
                menu.Items.Add(currentBranchOnly);
                menu.Items.Add(selectedCommitsOnly);
                menu.Items.Add(currentBranchAndSelectedCommits);
                menu.Open(button);
            }

            e.Handled = true;
        }

        private void OnOpenSortLocalBranchMenu(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && DataContext is ViewModels.Repository repo)
            {
                var isSortByName = repo.IsSortingLocalBranchByName;
                var byNameAsc = new MenuItem();
                byNameAsc.Header = App.Text("Repository.BranchSort.ByName");
                if (isSortByName)
                    byNameAsc.Icon = this.CreateMenuIcon("Icons.Check");
                byNameAsc.Click += (_, ev) =>
                {
                    if (!isSortByName)
                        repo.IsSortingLocalBranchByName = true;
                    ev.Handled = true;
                };

                var byCommitterDate = new MenuItem();
                byCommitterDate.Header = App.Text("Repository.BranchSort.ByCommitterDate");
                if (!isSortByName)
                    byCommitterDate.Icon = this.CreateMenuIcon("Icons.Check");
                byCommitterDate.Click += (_, ev) =>
                {
                    if (isSortByName)
                        repo.IsSortingLocalBranchByName = false;
                    ev.Handled = true;
                };

                var menu = new ContextMenu();
                menu.Placement = PlacementMode.BottomEdgeAlignedLeft;
                menu.Items.Add(byNameAsc);
                menu.Items.Add(byCommitterDate);
                menu.Open(button);
            }

            e.Handled = true;
        }

        private void OnOpenSortRemoteBranchMenu(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && DataContext is ViewModels.Repository repo)
            {
                var isSortByName = repo.IsSortingRemoteBranchByName;
                var byNameAsc = new MenuItem();
                byNameAsc.Header = App.Text("Repository.BranchSort.ByName");
                if (isSortByName)
                    byNameAsc.Icon = this.CreateMenuIcon("Icons.Check");
                byNameAsc.Click += (_, ev) =>
                {
                    if (!isSortByName)
                        repo.IsSortingRemoteBranchByName = true;
                    ev.Handled = true;
                };

                var byCommitterDate = new MenuItem();
                byCommitterDate.Header = App.Text("Repository.BranchSort.ByCommitterDate");
                if (!isSortByName)
                    byCommitterDate.Icon = this.CreateMenuIcon("Icons.Check");
                byCommitterDate.Click += (_, ev) =>
                {
                    if (isSortByName)
                        repo.IsSortingRemoteBranchByName = false;
                    ev.Handled = true;
                };

                var menu = new ContextMenu();
                menu.Placement = PlacementMode.BottomEdgeAlignedLeft;
                menu.Items.Add(byNameAsc);
                menu.Items.Add(byCommitterDate);
                menu.Open(button);
            }

            e.Handled = true;
        }

        private void OnOpenSortTagMenu(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && DataContext is ViewModels.Repository repo)
            {
                var isSortByName = repo.IsSortingTagsByName;
                var byCreatorDate = new MenuItem();
                byCreatorDate.Header = App.Text("Repository.Tags.OrderByCreatorDate");
                if (!isSortByName)
                    byCreatorDate.Icon = this.CreateMenuIcon("Icons.Check");
                byCreatorDate.Click += (_, ev) =>
                {
                    if (isSortByName)
                        repo.IsSortingTagsByName = false;
                    ev.Handled = true;
                };

                var byName = new MenuItem();
                byName.Header = App.Text("Repository.Tags.OrderByName");
                if (isSortByName)
                    byName.Icon = this.CreateMenuIcon("Icons.Check");
                byName.Click += (_, ev) =>
                {
                    if (!isSortByName)
                        repo.IsSortingTagsByName = true;
                    ev.Handled = true;
                };

                var menu = new ContextMenu();
                menu.Placement = PlacementMode.BottomEdgeAlignedLeft;
                menu.Items.Add(byName);
                menu.Items.Add(byCreatorDate);
                menu.Open(button);
            }

            e.Handled = true;
        }

        private async void OnPruneWorktrees(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
                await repo.PruneWorktreesAsync();

            e.Handled = true;
        }

        private async void OnSkipInProgress(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
                await repo.SkipMergeAsync();

            e.Handled = true;
        }

        private void OnResolveInProgress(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
                repo.SelectedViewIndex = 1;

            e.Handled = true;
        }

        private async void OnAbortInProgress(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo)
                await repo.AbortMergeAsync();

            e.Handled = true;
        }

        private void OnRemoveSelectedHistoryFilter(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Repository repo && sender is Button { DataContext: Models.HistoryFilter filter })
                repo.RemoveHistoryFilter(filter);

            e.Handled = true;
        }

        private async void OnBisectCommand(object sender, RoutedEventArgs e)
        {
            if (sender is Button button &&
                DataContext is ViewModels.Repository { IsBisectCommandRunning: false } repo &&
                repo.CanCreatePopup())
                await repo.ExecBisectCommandAsync(button.Tag as string);

            e.Handled = true;
        }

        private void OnRightPagePropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Border.IsVisibleProperty && sender is Border page)
            {
                var diffViewer = page.FindDescendantOfType<DiffView>();
                diffViewer?.ToggleHotkeyBindings(page.IsVisible);
            }
        }
    }
}
