﻿using System;
using BeatLeader.Models;
using BeatLeader.Utils;

namespace BeatLeader.Replayer
{
    public class NoteControllerEmulator : NoteController
    {
        #region Create NoteData

        private static readonly NoteData emptyNoteData = NoteData.CreateBasicNoteData(0, 0, NoteLineLayer.Base, ColorType.ColorA, NoteCutDirection.Any);
        private static NoteData CreateNoteData(NoteEvent noteEvent)
        {
            ReplayDataHelper.DecodeNoteId(
                noteEvent.noteID,
                out var scoringType,
                out var lineIndex,
                out var noteLineLayer,
                out var colorType,
                out var cutDirection
            );

            var gameplayType = scoringType switch
            {
                NoteData.ScoringType.Ignore => NoteData.GameplayType.Normal,
                NoteData.ScoringType.NoScore => NoteData.GameplayType.Bomb,
                NoteData.ScoringType.Normal => NoteData.GameplayType.Normal,
                NoteData.ScoringType.SliderHead => NoteData.GameplayType.Normal,
                NoteData.ScoringType.SliderTail => NoteData.GameplayType.Normal,
                NoteData.ScoringType.BurstSliderHead => NoteData.GameplayType.BurstSliderHead,
                NoteData.ScoringType.BurstSliderElement => NoteData.GameplayType.BurstSliderElement,
                _ => throw new ArgumentOutOfRangeException()
            };

            return emptyNoteData.CopyWith(
                time: noteEvent.spawnTime,
                lineIndex: lineIndex,
                noteLineLayer: noteLineLayer,
                gameplayType: gameplayType,
                scoringType: scoringType,
                colorType: colorType,
                cutDirection: cutDirection
            );
        }

        #endregion

        public override NoteData noteData => _noteData;
        public NoteCutInfo CutInfo { get; private set; }

        private NoteData _noteData;

        public void Setup(NoteEvent noteEvent)
        {
            _noteData = CreateNoteData(noteEvent);
            CutInfo = Models.NoteCutInfo.Convert(noteEvent.noteCutInfo, _noteData);
        }

        protected override void HiddenStateDidChange(bool _) { }
        public override void Pause(bool _) { }
    }
}