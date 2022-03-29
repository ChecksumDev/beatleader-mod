using System;
using BeatLeader.Manager;
using BeatLeader.Models;
using BeatSaberMarkupLanguage.Attributes;
using JetBrains.Annotations;

namespace BeatLeader.Components {
    [ViewDefinition(Plugin.ResourcesPath + ".BSML.Components.ScoreDetails.bsml")]
    internal class ScoreDetails : ReeUIComponent {
        #region SetScore

        public void SetScore(Score score) {
            PlaceholderText = $"Look at this awesome {score.player.name}`s score!\n{score.baseScore}\nKEKA";
        }

        #endregion
        
        #region PlaceholderText

        private string _placeholderText = "WOW, Such details!";

        [UIValue("placeholder-text"), UsedImplicitly]
        public string PlaceholderText {
            get => _placeholderText;
            set {
                if (_placeholderText.Equals(value)) return;
                _placeholderText = value;
                NotifyPropertyChanged();
            }
        }

        #endregion
    }
}