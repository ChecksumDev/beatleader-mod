﻿using System.Reflection;
using System.Threading.Tasks;
using BeatLeader.Models;
using BeatLeader.Utils;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using JetBrains.Annotations;
using TMPro;
using UnityEngine.UI;

namespace BeatLeader.Components {
    internal class BattleRoyaleOpponentsList : ReeListComponentBase<BattleRoyaleOpponentsList, IReplayHeader, BattleRoyaleOpponentsList.Cell> {
        #region Cells

        public class Cell : ReeTableCell<Cell, IReplayHeader> {
            #region UI Components

            [UIValue("player-avatar"), UsedImplicitly]
            private PlayerAvatar _playerAvatar = null!;

            [UIComponent("player-name"), UsedImplicitly]
            private TMP_Text _playerNameText = null!;

            [UIComponent("date"), UsedImplicitly]
            private TMP_Text _dateText = null!;

            #endregion

            #region Setup

            protected override string Markup { get; } = BSMLUtility.ReadMarkupOrFallback(
                "BattleRoyaleOpponentsListCell", Assembly.GetExecutingAssembly()
            );

            private IListComponent<IReplayHeader>? _replaysList;
            private IModifiableListComponent<IReplayHeader> _opponentsList = null!;
            private IReplayHeader _header = null!;

            protected override void Init(IReplayHeader item) {
                _header = item;
                _ = SetPlayerAsync(_header.ReplayInfo!.PlayerID);
            }

            public void Init(
                IModifiableListComponent<IReplayHeader> opponentsList,
                IListComponent<IReplayHeader>? replaysList
            ) {
                _opponentsList = opponentsList;
                _replaysList = replaysList;
            }

            protected override void OnInstantiate() {
                _playerAvatar = ReeUIComponentV2.Instantiate<PlayerAvatar>(transform);
                var layoutElement = _playerAvatar.GetRootTransform().gameObject.GetOrAddComponent<LayoutElement>();
                layoutElement.preferredHeight = layoutElement.preferredWidth = 6;
            }

            #endregion

            #region SetPlayer

            private const string PlayerEndpoint = BeatLeaderConstants.BEATLEADER_API_URL + "/player/";

            private async Task SetPlayerAsync(string playerId) {
                //TODO: move to new web requests
                var player = await WebUtils.SendAndDeserializeAsync<Player>(PlayerEndpoint + playerId);
                _playerAvatar.SetPlayer(player!);
                _playerNameText.text = player!.name;
            }

            #endregion

            #region Callbacks

            [UIAction("navigate-button-click"), UsedImplicitly]
            private void HandleNavigateButtonClicked() {
                _replaysList?.ScrollTo(_header);
            }

            [UIAction("remove-button-click"), UsedImplicitly]
            private void HandleRemoveButtonClicked() {
                var idx = _opponentsList.Items.IndexOf(_header);
                _opponentsList.Items.RemoveAt(idx);
                _opponentsList.Refresh(false);
                _opponentsList.ScrollTo(idx - 1);
                _replaysList?.ClearSelection(_header);
            }

            #endregion
        }

        #endregion

        #region Setup

        protected override float CellSize => 8;

        private IListComponent<IReplayHeader>? _list;

        public void Setup(IListComponent<IReplayHeader> list) {
            _list = list;
        }

        protected override void OnCellConstruct(Cell cell) {
            cell.Init(this, _list);
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            CellSelectionType = TableViewSelectionType.None;
        }

        #endregion
    }
}