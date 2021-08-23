using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui;

namespace DelvUI.Interface {
    public class UnitFrameOnlyHudWindow : HudWindow {
        public override uint JobId => 23;

        private int BarHeight => 20;
        private int SmallBarHeight => 10;
        private int BarWidth => 250;
        private new int XOffset => 127;
        private new int YOffset => 440;
        
        public UnitFrameOnlyHudWindow(
            ClientState clientState, 
            GameGui gameGui,
            JobGauges jobGauges,
            ObjectTable objectTable, 
            PluginConfiguration pluginConfiguration, 
            TargetManager targetManager
        ) : base(
            clientState,
            gameGui,
            jobGauges,
            objectTable,
            pluginConfiguration,
            targetManager
        ) { }

        protected override void Draw(bool _) {
            DrawHealthBar();
            DrawPrimaryResourceBar();
            DrawTargetBar();
            DrawFocusBar();
        }
    }
}