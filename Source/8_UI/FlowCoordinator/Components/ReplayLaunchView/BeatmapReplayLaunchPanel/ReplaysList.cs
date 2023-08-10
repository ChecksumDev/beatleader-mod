﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeatLeader.Models;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using IPA.Utilities;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using static BeatLeader.Models.LevelEndType;

namespace BeatLeader.Components {
    internal class ReplaysList : ReeUIComponentV3<ReplaysList>, TableView.IDataSource {
        #region Cells

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        private abstract class AbstractDataCell : TableCell {
            #region Config

            public const int CellHeight = 8;

            private const string MarkupPath = Plugin.ResourcesPath +
                ".BSML.FlowCoordinator.Components.ReplayLaunchView.BeatmapReplayLaunchPanel.ReplaysList.ReplaysListCell.bsml";

            #endregion

            #region Resources

            private static readonly string markup = Utilities
                .GetResourceContent(typeof(AbstractDataCell).Assembly, MarkupPath);

            private static readonly Material noGlowMaterial = Resources.FindObjectsOfTypeAll
                <Material>().FirstOrDefault(x => x.name == "UINoGlowAdditive")!;

            private static readonly Sprite backgroundSprite = Resources.FindObjectsOfTypeAll
                <Sprite>().FirstOrDefault(x => x.name == "RoundRect10Thin")!;

            private static readonly Signal clickSignal = Resources.FindObjectsOfTypeAll
                <Signal>().FirstOrDefault(x => x.name == "TableCellWasPressed")!;

            #endregion

            #region Data

            public IReplayHeader? ReplayHeader { get; private set; }

            #endregion

            #region Construction

            private bool _isInitialized;

            public static T Create<T>(IReplayHeader header) where T : AbstractDataCell {
                if (header is null) throw new ArgumentNullException(nameof(header));
                var instance = new GameObject(nameof(AbstractDataCell)).AddComponent<T>();
                instance.Init(header);
                //when you finish a map it invokes the finish event and executes score sending and replay saving.
                //cell generation also happens there (on the game scene). as we know unity destroys objects after the scene transition
                //so we need to use DontDestroyOnLoad to keep this cell alive
                DontDestroyOnLoad(instance);
                return instance;
            }

            public AbstractDataCell Init(IReplayHeader header) {
                ReplayHeader = header;
                if (!_isInitialized) {
                    PersistentSingleton<BSMLParser>.instance.Parse(markup, gameObject, this);
                    ((SelectableCell)this).SetField("_wasPressedSignal", clickSignal);
                    gameObject.AddComponent<Touchable>();
                    name = nameof(AbstractDataCell);
                    reuseIdentifier = name;
                    _isInitialized = true;
                }
                OnConstruct();
                return this;
            }

            protected virtual void OnConstruct() { }

            #endregion

            #region Colors

            protected abstract Color HighlightColor { get; }
            protected abstract Color HighlightSelectedColor { get; }
            protected abstract Color SelectColor { get; }
            protected abstract Color IdlingColor { get; }

            #endregion

            #region UI

            public string? TopLeftText {
                get => _topLeftTextComponent.text;
                set => _topLeftTextComponent.text = value;
            }

            protected string? BottomLeftText {
                get => _bottomLeftTextComponent.text;
                set => _bottomLeftTextComponent.text = value;
            }

            protected string? TopRightText {
                get => _topRightTextComponent.text;
                set => _topRightTextComponent.text = value;
            }

            protected string? BottomRightText {
                get => _bottomRightTextComponent.text;
                set => _bottomRightTextComponent.text = value;
            }

            [UIComponent("top-left")]
            protected readonly TMP_Text _topLeftTextComponent = null!;

            [UIComponent("bottom-left")]
            protected readonly TMP_Text _bottomLeftTextComponent = null!;

            [UIComponent("top-right")]
            protected readonly TMP_Text _topRightTextComponent = null!;

            [UIComponent("bottom-right")]
            protected readonly TMP_Text _bottomRightTextComponent = null!;

            [UIComponent("background")]
            protected readonly ImageView background = null!;

            protected override void HighlightDidChange(TransitionType transitionType) {
                RefreshVisuals();
            }

            protected override void SelectionDidChange(TransitionType transitionType) {
                RefreshVisuals();
            }

            protected void RefreshVisuals() {
                background.color1 = selected switch {
                    false => highlighted ? HighlightColor : IdlingColor,
                    true => highlighted ? HighlightSelectedColor : SelectColor
                };
            }

            [UIAction("#post-parse")]
            protected void HandlePostParse() {
                background.sprite = backgroundSprite;
                background.gradient = true;
                background.material = noGlowMaterial;
                background.SetField("_skew", 0.18f);
                background.__Refresh();
            }

            #endregion
        }

        [UsedImplicitly]
        private class ReplayDataCell : AbstractDataCell {
            static ReplayDataCell() {
                ColorUtility.TryParseHtmlString("#00C0FFFF", out selectColor);
                highlightSelectedColor = selectColor.ColorWithAlpha(0.75f);
            }

            private static readonly Color selectColor;
            private static readonly Color highlightSelectedColor;
            private static readonly Color highlightColor = Color.white.ColorWithAlpha(0.2f);
            private static readonly Color idlingColor = Color.clear;

            public bool ShowBeatmapName {
                set {
                    if (ReplayHeader?.ReplayInfo is not { } info) return;
                    var diff = FormatDiff(info.SongDifficulty);
                    TopLeftText = value ? info.SongName : diff;
                    BottomLeftText = $"{(value ? diff : string.Empty)} {info.PlayerName}";
                    static string FormatDiff(string diff) => $"[<color=#89ff89>{diff}</color>]";
                }
            }

            protected override void OnConstruct() {
                if (ReplayHeader!.FileStatus is FileStatus.Corrupted
                    || ReplayHeader.ReplayInfo is not { } info) return;
                ShowBeatmapName = false;
                TopRightText = info.LevelEndType switch {
                    Clear => "Completed",
                    Quit or Restart => "Unfinished",
                    Fail => $"Failed at {FormatTime(Mathf.FloorToInt(ReplayHeader.ReplayInfo.FailTime))}",
                    _ => "Unknown"
                };
                BottomRightText = FormatUtils.GetDateTimeString(info.Timestamp);
                reuseIdentifier = nameof(ReplayDataCell);
            }

            private static string FormatTime(int seconds) {
                var minutes = seconds / 60;
                var hours = minutes / 60;
                var secDiv = seconds % 60;
                var minDiv = minutes % 60;
                return $"{(hours is not 0 ? $"{Zero(hours)}{hours}:" : "")}{Zero(minDiv)}{minDiv}:{Zero(secDiv)}{secDiv}";
                static string Zero(int number) => number > 9 ? "" : "0";
            }

            #region Colors

            protected override Color HighlightColor => highlightColor;
            protected override Color HighlightSelectedColor => highlightSelectedColor;
            protected override Color SelectColor => selectColor;
            protected override Color IdlingColor => idlingColor;

            #endregion
        }

        [UsedImplicitly]
        private class CorruptedReplayDataCell : AbstractDataCell {
            private static readonly Color selectColor = Color.red;
            private static readonly Color highlightSelectedColor = selectColor.ColorWithAlpha(0.75f);
            private static readonly Color highlightColor = selectColor.ColorWithAlpha(0.5f);
            private static readonly Color idlingColor = selectColor.ColorWithAlpha(0.3f);

            protected override void OnConstruct() {
                const string UNKNOWN = "Unknown";
                TopLeftText = "Corrupted file";
                BottomLeftText = Path.GetFileNameWithoutExtension(ReplayHeader!.FilePath);
                TopRightText = UNKNOWN;
                BottomRightText = UNKNOWN;
            }

            #region Colors

            protected override Color HighlightColor => highlightColor;
            protected override Color HighlightSelectedColor => highlightSelectedColor;
            protected override Color SelectColor => selectColor;
            protected override Color IdlingColor => idlingColor;

            #endregion
        }

        #endregion

        #region Events

        [ExternalProperty, UsedImplicitly]
        public event Action<IReplayHeader[]?>? ReplaysSelectedEvent;

        #endregion

        #region UI Components

        [UIValue("visible-cells")]
        public const int VisibleCells = 7;

        [UIComponent("list")]
        private readonly CustomCellListTableData _replaysList = null!;

        [UIObject("empty-text")]
        private readonly GameObject _emptyTextObject = null!;

        private TableView _tableView = null!;

        #endregion

        #region Init & Dispose

        protected override void OnInitialize() {
            _tableView = _replaysList.tableView;
            _selectedCellIndexes = _tableView.GetField<HashSet<int>, TableView>("_selectedCellIdxs");
            _tableView.SetDataSource(this, true);
            _tableView.didSelectCellWithIdxEvent += HandleCellSelected;
            Refresh();
        }

        protected override void OnDispose() {
            var cells = _tableView.GetField<Dictionary<string, List<TableCell>>, TableView>("_reusableCells");
            foreach (var cell in cells.Values.SelectMany(cellList => cellList)) Destroy(cell);
            foreach (var cell in _tableView.visibleCells) Destroy(cell);
        }

        #endregion

        #region TableView

        float TableView.IDataSource.CellSize() => AbstractDataCell.CellHeight;

        int TableView.IDataSource.NumberOfCells() => replays?.Count ?? 0;

        private HashSet<int> _selectedCellIndexes = null!;

        TableCell TableView.IDataSource.CellForIdx(TableView tableView, int idx) {
            if (tableView.DequeueReusableCellForIdentifier(nameof(ReplayDataCell)) is not ReplayDataCell cell) {
                cell = AbstractDataCell.Create<ReplayDataCell>(replays[idx]);
            } else cell.Init(replays[idx]);
            cell.ShowBeatmapName = showBeatmapNameIfCorrect;
            return cell;
        }

        private void ShowEmptyScreen(bool show) {
            _replaysList.gameObject.SetActive(!show);
            _emptyTextObject.SetActive(show);
        }

        #endregion

        #region Sorting

        public enum Sorter {
            Difficulty,
            Player,
            Completion,
            Date
        }

        private class HeaderComparator : IComparer<IReplayHeader> {
            public Sorter sorter;

            public int Compare(IReplayHeader x, IReplayHeader y) {
                var xi = x.ReplayInfo;
                var yi = y.ReplayInfo;
                return xi is null || yi is null ? 0 : sorter switch {
                    Sorter.Difficulty =>
                        -CompareInteger(
                            (int)StringConverter.Convert<BeatmapDifficulty>(xi.SongDifficulty),
                            (int)StringConverter.Convert<BeatmapDifficulty>(yi.SongDifficulty)),
                    Sorter.Player =>
                        string.CompareOrdinal(xi.PlayerName, yi.PlayerName),
                    Sorter.Completion =>
                        CompareInteger((int)xi.LevelEndType, (int)yi.LevelEndType),
                    Sorter.Date =>
                        -CompareInteger(int.Parse(xi.Timestamp), int.Parse(yi.Timestamp)),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            private static int CompareInteger(int x, int y) => x >= y ? x == y ? 0 : 1 : -1;
        }

        public Sorter SortBy {
            get => _headerComparator.sorter;
            set {
                _headerComparator.sorter = value;
                RefreshSorting();
                Refresh();
            }
        }

        public SortOrder SortOrder {
            get => _sortOrder;
            set {
                _sortOrder = value;
                RefreshSorting();
                Refresh();
            }
        }

        private readonly HeaderComparator _headerComparator = new();
        private SortOrder _sortOrder;

        private void RefreshSorting() {
            replays.Sort(_headerComparator);
            if (_sortOrder is SortOrder.Ascending) replays.Reverse();
        }

        #endregion

        #region Data

        public bool AllowMultiselect {
            get => _tableView.selectionType is TableViewSelectionType.Multiple;
            set => _tableView.selectionType = value ? TableViewSelectionType.Multiple : TableViewSelectionType.Single;
        }

        public readonly List<IReplayHeader> replays = new();

        public bool showBeatmapNameIfCorrect = true;

        public void Refresh() {
            _tableView.ClearSelection();
            RefreshSorting();
            _tableView.ReloadData();
            var empty = replays.Count is 0;
            ShowEmptyScreen(empty);
            if (!empty) return;
            ReplaysSelectedEvent?.Invoke(null);
        }

        #endregion

        #region Callbacks

        private void HandleCellSelected(TableView view, int cellIdx) {
            ReplaysSelectedEvent?.Invoke(_selectedCellIndexes.Select(x => replays![x]).ToArray());
        }

        #endregion
    }
}