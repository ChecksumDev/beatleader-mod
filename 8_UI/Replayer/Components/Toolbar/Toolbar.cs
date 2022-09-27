﻿using BeatSaberMarkupLanguage.Attributes;
using BeatLeader.Components.Settings;
using BeatLeader.UI.BSML_Addons.Components;
using BeatLeader.UI.BSML_Addons;
using BeatLeader.Replayer;
using UnityEngine.UI;
using UnityEngine;
using Zenject;
using HMUI;
using BeatLeader.Utils;

namespace BeatLeader.Components
{
    internal class Toolbar : EditableElement
    {
        [Inject] private readonly PlaybackController _playbackController;
        [InjectOptional] private readonly LayoutEditor _layoutEditor;

        [UIComponent("container")] private RectTransform _container;
        [UIComponent("play-button")] private BetterButton _playButton;
        [UIComponent("exit-button-background")] private RectTransform _exitButtonBackground;
        [UIComponent("exit-button-icon")] private BetterImage _exitButtonIcon;
        [UIComponent("settings-modal")] private ModalView _settingsModal;

        [UIValue("combined-song-time")] private string _CombinedSongTime
        {
            get => _combinedSongTime;
            set
            {
                _combinedSongTime = value;
                NotifyPropertyChanged(nameof(_CombinedSongTime));
            }
        }
        [UIValue("timeline")] private Timeline _timeline;
        [UIValue("settings-navigator")] private SettingsController _settingsNavigator;

        protected override RectTransform ContainerRect => _container;
        public override bool Locked => true;

        private Sprite _playSprite;
        private Sprite _pauseSprite;
        private Sprite _openedDoorSprite;
        private Sprite _closedDoorSprite;
        private string _combinedSongTime;

        protected override void OnInstantiate()
        {
            _playSprite = BSMLUtility.LoadSprite("#play-icon");
            _pauseSprite = BSMLUtility.LoadSprite("#pause-icon");
            _openedDoorSprite = BSMLUtility.LoadSprite("#opened-door-icon");
            _closedDoorSprite = BSMLUtility.LoadSprite("#closed-door-icon");
            _timeline = InstantiateInContainer<Timeline>(Container, transform);
            _settingsNavigator = InstantiateInContainer<SettingsController>(Container, transform);
            _settingsNavigator.RootMenu = MenuWithContainer.InstantiateInContainer<SettingsRootMenu>(Container);
            _playbackController.PauseStateChangedEvent += HandlePauseStateChanged;
        }
        protected override void OnInitialize()
        {
            var button = _exitButtonBackground.gameObject.AddComponent<NoTransitionsButton>();
            button.selectionStateDidChangeEvent += HandleExitButtonSelectionStateChanged;
            button.onClick.AddListener(_playbackController.EscapeToMenu);
            button.navigation = new Navigation() { mode = Navigation.Mode.None };
            _settingsModal.blockerClickedEvent += _settingsNavigator.HandleSettingsWasClosed;
            _settingsNavigator.SettingsCloseRequestedEvent += x => _settingsModal.Hide(x, () => _settingsNavigator.HandleSettingsWasClosed());
        }
        private void Update()
        {
            float time = _playbackController.CurrentSongTime;
            float totalTime = _playbackController.TotalSongTime;

            float minutes = Mathf.FloorToInt(time / 60);
            float seconds = Mathf.FloorToInt(time - (minutes * 60));
            float totalMinutes = Mathf.FloorToInt(totalTime / 60);
            float totalSeconds = Mathf.FloorToInt(totalTime - (totalMinutes * 60));

            string combinedTotalTime = $"{totalMinutes}.{(totalSeconds < 10 ? $"0{totalSeconds}" : totalSeconds)}";
            _CombinedSongTime = $"{minutes}.{(seconds < 10 ? $"0{seconds}" : seconds)}/{combinedTotalTime}";
        }

        [UIAction("pause-button-clicked")]
        private void OnPauseButtonClicked()
        {
            _playbackController.Pause(!_playbackController.IsPaused);
        }
        private void HandleExitButtonSelectionStateChanged(NoTransitionsButton.SelectionState state)
        {
            if (state == NoTransitionsButton.SelectionState.Highlighted)
            {
                _exitButtonIcon.Image.sprite = _openedDoorSprite;
            }
            if (state == NoTransitionsButton.SelectionState.Normal)
            {
                _exitButtonIcon.Image.sprite = _closedDoorSprite;
            }
        }
        private void HandlePauseStateChanged(bool pause)
        {
            _playButton.TargetGraphic.sprite = pause ? _playSprite : _pauseSprite;
        }
    }
}