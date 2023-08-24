﻿using System;
using System.Collections.Generic;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Tags;
using HMUI;
using IPA.Utilities;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace BeatLeader.Components {
    /// <summary>
    /// Abstraction for lists
    /// </summary>
    internal interface IListComponent {
        void Refresh(bool clearSelection = true);

        void ScrollTo(int idx, bool animated = true);

        /// <summary>
        /// Clears selected cell with specified index
        /// </summary>
        /// <param name="idx">Index of the cell. -1 for all cells</param>
        void ClearSelection(int idx = -1);
    }

    /// <summary>
    /// Abstraction for lists with value
    /// </summary>
    /// <typeparam name="TItem">Data type</typeparam>
    internal interface IListComponent<in TItem> : IListComponent {
        void ScrollTo(TItem item, bool animated = true);

        void ClearSelection(TItem item);
    }

    /// <summary>
    /// Modifiable abstraction for lists with value
    /// </summary>
    /// <typeparam name="TItem">Data type</typeparam>
    internal interface IModifiableListComponent<TItem> : IListComponent<TItem> {
        IList<TItem> Items { get; }
    }

    /// <summary>
    /// Universal ReeUIComponentV3 base for lists
    /// </summary>
    /// <typeparam name="T">Inherited component</typeparam>
    /// <typeparam name="TItem">Data type</typeparam>
    internal abstract class ListComponentBase<T, TItem> : LayoutComponentBase<T>, TableView.IDataSource, IModifiableListComponent<TItem>
        where T : ReeUIComponentV3<T> {

        #region Events

        [ExternalProperty, UsedImplicitly]
        public event Action<ICollection<int>>? ItemsWithIndexesSelectedEvent;

        #endregion

        #region DataSource

        float TableView.IDataSource.CellSize() => CellSize;

        int TableView.IDataSource.NumberOfCells() => items.Count;

        TableCell TableView.IDataSource.CellForIdx(TableView tableView, int idx) => ConstructCell(items[idx]);

        #endregion

        #region ModifiableListComponent

        IList<TItem> IModifiableListComponent<TItem>.Items => items;

        public void ScrollTo(TItem item, bool animated = true) {
            var idx = FindItemIndex(item);
            if (idx is -1) return;
            ScrollTo(idx, animated);
        }

        public void ClearSelection(TItem item) {
            var idx = -1;
            if (CellSelectionType is TableViewSelectionType.Multiple) {
                idx = FindItemIndex(item);
                if (idx is -1) return;
            }
            ClearSelection(idx);
        }

        private int FindItemIndex(TItem item) {
            return items.FindIndex(x => x!.Equals(item));
        }

        #endregion

        #region UI Components

        private TableView _tableView = null!;
        private ScrollView _scrollView = null!;

        private GameObject _listObject = null!;
        private GameObject _emptyTextObject = null!;

        #endregion

        #region TableView

        protected TableViewSelectionType CellSelectionType {
            get => _tableView.selectionType;
            set => _tableView.selectionType = value;
        }

        public readonly List<TItem> items = new();

        public void ScrollTo(int idx, bool animated = true) {
            _tableView.ScrollToCellWithIdx(idx, TableView.ScrollPositionType.Center, animated);
        }

        public void Refresh(bool clearSelection = true) {
            if (clearSelection) {
                ClearSelectionInternal(-1);
                _tableView.ReloadData();
            } else _tableView.ReloadDataKeepingPosition();
            ShowEmptyScreen(items.Count is 0);
            if (clearSelection) ItemsWithIndexesSelectedEvent?.Invoke(Array.Empty<int>());
        }

        public void ClearSelection(int idx = -1) {
            ClearSelectionInternal(idx);
            Refresh(false);
        }

        protected TableCell? DequeueReusableCell(string identifier) {
            return _tableView.DequeueReusableCellForIdentifier(identifier);
        }

        private void ClearSelectionInternal(int idx) {
            if (CellSelectionType is TableViewSelectionType.None) return;
            if (idx is not -1 && CellSelectionType is TableViewSelectionType.Multiple) _selectedCellIndexes.Remove(idx);
            else _tableView.ClearSelection();
        }

        private void ShowEmptyScreen(bool show) {
            _listObject.SetActive(!show);
            _emptyTextObject.SetActive(show);
        }

        #endregion

        #region Abstraction

        protected virtual ScrollView.ScrollViewDirection ScrollDirection => ScrollView.ScrollViewDirection.Vertical;

        protected virtual string? EmptyText => "The monkey left you on your own!";

        protected abstract float CellSize { get; }

        protected abstract TableCell ConstructCell(TItem data);

        #endregion

        #region Scrollbar

        [ExternalProperty, UsedImplicitly]
        public IScrollbar? Scrollbar {
            get => _scrollbar;
            set {
                if (_scrollbar is not null) _scrollbar.ScrollEvent -= HandleScrollbarScroll;
                _scrollbar = value;
                if (_scrollbar is not null) _scrollbar.ScrollEvent += HandleScrollbarScroll;
            }
        }

        private IScrollbar? _scrollbar;

        private void UpdateScrollbar(float pos) {
            if (_scrollbar is null) return;
            //TODO: asm pub; pub ver:
            //var contentRect = _scrollView._contentRectTransform.rect;
            //var contentRect = _scrollView._contentRectTransform.rect;
            //var viewportRect = _scrollView._viewport.rect;
            //_scrollbar.PageHeight = ScrollDirection is ScrollView.ScrollViewDirection.Vertical ?
            //    viewportRect.height / contentRect.height :
            //    viewportRect.width / contentRect.width;
            //var progress = pos / (_scrollView.contentSize - _scrollView.scrollPageSize);
            //_scrollbar.Progress = progress;
            //_scrollbar.CanScrollUp = _scrollView._destinationPos > 1f / 1000;
            //_scrollbar.CanScrollDown = _scrollView._destinationPos < _scrollView.contentSize - _scrollView.scrollPageSize - 1f / 1000;
            
            var contentRect = _scrollView.GetField<RectTransform, ScrollView>("_contentRectTransform").rect;
            var viewportRect = _scrollView.GetField<RectTransform, ScrollView>("_viewport").rect;
            _scrollbar.PageHeight = ScrollDirection is ScrollView.ScrollViewDirection.Vertical ?
                viewportRect.height / contentRect.height :
                viewportRect.width / contentRect.width;
            var progress = pos / (_scrollView.GetProperty<float, ScrollView>("contentSize") - 
                _scrollView.GetProperty<float, ScrollView>("scrollPageSize"));
            _scrollbar.Progress = progress;
            _scrollbar.CanScrollUp = _scrollView.GetField<float, ScrollView>("_destinationPos") > 1f / 1000;
            _scrollbar.CanScrollDown = _scrollView.GetField<float, ScrollView>("_destinationPos")
                < _scrollView.GetProperty<float, ScrollView>("contentSize")
                - _scrollView.GetProperty<float, ScrollView>("scrollPageSize") - 1f / 1000;
        }

        private void HandleScrollbarScroll(ScrollView.ScrollDirection scrollDirection) {
            switch (scrollDirection) {
                case ScrollView.ScrollDirection.Up:
                    _scrollView.PageUpButtonPressed();
                    break;
                case ScrollView.ScrollDirection.Down:
                    _scrollView.PageDownButtonPressed();
                    break;
            }
        }

        #endregion

        #region Setup

        // ReSharper disable once StaticMemberInGenericType
        private static readonly CustomListTag customListTag = new();

        private HashSet<int> _selectedCellIndexes = null!;

        protected override void OnInitialize() {
            _listObject = customListTag.CreateObject(ContentTransform);
            var tableData = _listObject.GetComponent<CustomCellListTableData>();

            var textGo = new GameObject("EmptyText");
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 3.2f;
            text.text = EmptyText;
            _emptyTextObject = textGo;

            _tableView = tableData.tableView;
            _tableView.SetDataSource(this, true);
            Destroy(tableData);
            
            //TODO: asm pub; pub ver:
            //_scrollView = _tableView._scrollView;
            //_scrollView.SetField("_platformHelper", BeatSaberUI.PlatformHelper);
            //_scrollView._scrollViewDirection = ScrollDirection;
            //_scrollView.scrollPositionChangedEvent += UpdateScrollbar;
            //_tableView._tableType = ScrollDirection switch {
            //    ScrollView.ScrollViewDirection.Horizontal => TableView.TableType.Horizontal,
            //    _ => TableView.TableType.Vertical
            //};
            //_selectedCellIndexes = _tableView._selectedCellIdxs;
            //_tableView.didSelectCellWithIdxEvent += HandleCellWithIndexSelected;
            //_tableView.gameObject.SetActive(true);
            //ShowEmptyScreen(false);
            
            _scrollView = _tableView.GetField<ScrollView, TableView>("_scrollView");
            _scrollView.SetField("_platformHelper", BeatSaberUI.PlatformHelper);
            _scrollView.SetField("_scrollViewDirection", ScrollDirection);
            _scrollView.scrollPositionChangedEvent += UpdateScrollbar;

            _tableView.SetField("_tableType", ScrollDirection switch {
                ScrollView.ScrollViewDirection.Horizontal => TableView.TableType.Horizontal,
                _ => TableView.TableType.Vertical
            });
            _selectedCellIndexes = _tableView.GetField<HashSet<int>, TableView>("_selectedCellIdxs");
            _tableView.didSelectCellWithIdxEvent += HandleCellWithIndexSelected;
            _tableView.gameObject.SetActive(true);
            ShowEmptyScreen(false);
        }

        #endregion

        #region Callbacks

        private void HandleCellWithIndexSelected(TableView view, int idx) {
            ItemsWithIndexesSelectedEvent?.Invoke(_selectedCellIndexes);
        }

        #endregion
    }
}