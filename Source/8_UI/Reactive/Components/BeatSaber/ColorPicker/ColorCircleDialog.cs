namespace BeatLeader.UI.Reactive.Components {
    internal class ColorCircleDialog : DialogComponentBase {
        public ColorCircle ColorCircle { get; } = new();
        
        protected override ILayoutItem ConstructContent() {
            return ColorCircle.AsFlexItem(size: 54f);
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            this.WithRectSize(67f, 54f);
            Title = "Select Color";
            ShowCancelButton = false;
        }
    }
}