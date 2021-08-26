using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

namespace DelvUI.Interface {
    
    public abstract class HudWindow {
        public bool IsVisible = true;
        protected readonly ClientState ClientState;
        protected readonly GameGui GameGui;
        protected readonly JobGauges JobGauges;
        protected readonly ObjectTable ObjectTable;
        protected readonly PluginConfiguration PluginConfiguration;
        protected readonly TargetManager TargetManager;
        
        private Vector2 _barSize;

        public abstract uint JobId { get; }

        protected float CenterX => ImGui.GetMainViewport().Size.X / 2f;
        protected float CenterY => ImGui.GetMainViewport().Size.Y / 2f;
        protected int XOffset => 160;
        protected int YOffset => 460;
        protected int HealthBarHeight => PluginConfiguration.HealthBarHeight;
        protected int HealthBarWidth => PluginConfiguration.HealthBarWidth;
        protected int TargetBarHeight => PluginConfiguration.TargetBarHeight;
        protected int TargetBarWidth => PluginConfiguration.TargetBarWidth;
        protected int ToTBarHeight => PluginConfiguration.ToTBarHeight;
        protected int ToTBarWidth => PluginConfiguration.ToTBarWidth;        
        protected int FocusBarHeight => PluginConfiguration.FocusBarHeight;
        protected int FocusBarWidth => PluginConfiguration.FocusBarWidth;
        protected Vector2 BarSize => _barSize;
        
        protected HudWindow(
            ClientState clientState, 
            GameGui gameGui,
            JobGauges jobGauges,
            ObjectTable objectTable, 
            PluginConfiguration pluginConfiguration, 
            TargetManager targetManager 
            ) {
            ClientState = clientState;
            GameGui = gameGui;
            JobGauges = jobGauges;
            ObjectTable = objectTable;
            PluginConfiguration = pluginConfiguration;
            TargetManager = targetManager;
        }

        protected virtual void DrawHealthBar() {
            Debug.Assert(ClientState.LocalPlayer != null, "ClientState.LocalPlayer != null");
            _barSize = new Vector2(HealthBarWidth, HealthBarHeight);
            var actor = ClientState.LocalPlayer;
            var scale = (float) actor.CurrentHp / actor.MaxHp;
            var cursorPos = new Vector2(CenterX - HealthBarWidth - XOffset, CenterY + YOffset);

            DrawOutlinedText($"{actor.Name.Abbreviate().Truncate(16)}", new Vector2(cursorPos.X + 5, cursorPos.Y -22));
            
            var hp = $"{actor.MaxHp.KiloFormat(),6} | ";
            var hpSize = ImGui.CalcTextSize(hp);
            var percentageSize = ImGui.CalcTextSize("100");
            DrawOutlinedText(hp, new Vector2(cursorPos.X + HealthBarWidth - hpSize.X - percentageSize.X - 5, cursorPos.Y -22));
            DrawOutlinedText($"{(int)(scale * 100),3}", new Vector2(cursorPos.X + HealthBarWidth - percentageSize.X - 5, cursorPos.Y -22));
            
            ImGui.SetCursorPos(cursorPos);
            
            if (ImGui.BeginChild("health_bar", BarSize)) {
                var colors = PluginConfiguration.JobColorMap[ClientState.LocalPlayer.ClassJob.Id];
                var drawList = ImGui.GetWindowDrawList();
                drawList.AddRectFilled(cursorPos, cursorPos + BarSize, colors["background"]);
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2(HealthBarWidth * scale, HealthBarHeight), 
                    colors["gradientLeft"], colors["gradientRight"], colors["gradientRight"], colors["gradientLeft"]
                );
                drawList.AddRect(cursorPos, cursorPos + BarSize, 0xFF000000);

                if (ImGui.IsItemClicked()) {
                    TargetManager.SetTarget(actor);
                }
                
            }
            
            ImGui.EndChild();
        }

        protected virtual void DrawPrimaryResourceBar() {
            var actor = ClientState.LocalPlayer;

            if (actor?.CurrentMp != null) {
                var scale = (float) actor.CurrentMp / actor.MaxMp;
                var barSize = new Vector2(254, 13);
                var cursorPos = new Vector2(CenterX - 127, CenterY + YOffset - 27);
            
                var drawList = ImGui.GetWindowDrawList();
                drawList.AddRectFilled(cursorPos, cursorPos + barSize, 0x88000000);
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2(barSize.X * scale, barSize.Y), 
                    0xFFE6CD00, 0xFFD8Df3C, 0xFFD8Df3C, 0xFFE6CD00
                );
                drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);
            }
        }
        
        protected virtual void DrawTargetBar() {
            var target = TargetManager.SoftTarget ?? TargetManager.Target;

            if (target is null) {
                return;
            }

            _barSize = new Vector2(TargetBarWidth, TargetBarHeight);

            var cursorPos = new Vector2(CenterX + XOffset, CenterY + YOffset);
            ImGui.SetCursorPos(cursorPos);
            var drawList = ImGui.GetWindowDrawList();

            if (target is not Character actor) {
                var friendly = PluginConfiguration.NPCColorMap["friendly"];
                drawList.AddRectFilled(cursorPos, cursorPos + BarSize, friendly["background"]);
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2(TargetBarWidth, TargetBarHeight), 
                    friendly["gradientLeft"], friendly["gradientRight"], friendly["gradientRight"], friendly["gradientLeft"]
                );
                drawList.AddRect(cursorPos, cursorPos + BarSize, 0xFF000000);
            }
            else {
                var scale = actor.MaxHp > 0f ? (float) actor.CurrentHp / actor.MaxHp : 0f;
                var colors = DetermineTargetPlateColors(actor);
                drawList.AddRectFilled(cursorPos, cursorPos + BarSize, colors["background"]);
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2(TargetBarWidth * scale, TargetBarHeight), 
                    colors["gradientLeft"], colors["gradientRight"], colors["gradientRight"], colors["gradientLeft"]
                );
                drawList.AddRect(cursorPos, cursorPos + BarSize, 0xFF000000);

                var percentage = $"{(int) (scale * 100),3}";
                var percentageSize = ImGui.CalcTextSize(percentage);
                var maxPercentageSize = ImGui.CalcTextSize("100");
                DrawOutlinedText(percentage, new Vector2(cursorPos.X + 5 + maxPercentageSize.X - percentageSize.X, cursorPos.Y - 22));
                DrawOutlinedText($" | {actor.MaxHp.KiloFormat(),-6}", new Vector2(cursorPos.X + 5 + maxPercentageSize.X, cursorPos.Y - 22));
            }

            var name = $"{target.Name.Abbreviate().Truncate(16)}";
            var nameSize = ImGui.CalcTextSize(name);
            DrawOutlinedText(name, new Vector2(cursorPos.X + TargetBarWidth - nameSize.X - 5, cursorPos.Y - 22));

            DrawTargetOfTargetBar(target.TargetObject);
        }
        protected virtual void DrawFocusBar() {
            var focus = TargetManager.FocusTarget;
            if (focus is null) {
                return;
            }
            var barSize = new Vector2(FocusBarWidth, FocusBarHeight);
            
            var cursorPos = new Vector2(CenterX - XOffset - HealthBarWidth - FocusBarWidth-2, CenterY + YOffset);
            ImGui.SetCursorPos(cursorPos);  
            var drawList = ImGui.GetWindowDrawList();
            
            if (focus is not Character actor) {
                var friendly = PluginConfiguration.NPCColorMap["friendly"];
                drawList.AddRectFilled(cursorPos, cursorPos + barSize, friendly["background"]);
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2(FocusBarWidth, FocusBarHeight), 
                    friendly["gradientLeft"], friendly["gradientRight"], friendly["gradientRight"], friendly["gradientLeft"]
                );
                drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);
            }
            else
            {
                var colors = DetermineTargetPlateColors(actor);
                drawList.AddRectFilled(cursorPos, cursorPos + barSize, colors["background"]);
                
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2((float)FocusBarWidth * actor.CurrentHp / actor.MaxHp, FocusBarHeight), 
                    colors["gradientLeft"], colors["gradientRight"], colors["gradientRight"], colors["gradientLeft"]
                );
                
                drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);
            }
            
            var name = $"{focus.Name.Abbreviate().Truncate(12)}";
            var textSize = ImGui.CalcTextSize(name);
            DrawOutlinedText(name, new Vector2(cursorPos.X + FocusBarWidth / 2f - textSize.X / 2f, cursorPos.Y - 22));

            
        }
        
        protected virtual void DrawTargetOfTargetBar(GameObject targetObject) {
            if (targetObject is not Character actor) {
                return;
            }

            var barSize = new Vector2(ToTBarWidth, ToTBarHeight);

            var name = $"{actor.Name.Abbreviate().Truncate(12)}";
            var textSize = ImGui.CalcTextSize(name);

            var cursorPos = new Vector2(CenterX + XOffset + TargetBarWidth + 2, CenterY + YOffset);
            DrawOutlinedText(name, new Vector2(cursorPos.X + ToTBarWidth / 2f - textSize.X / 2f, cursorPos.Y - 22));
            ImGui.SetCursorPos(cursorPos);    
            
            var colors = DetermineTargetPlateColors(actor);
            if (ImGui.BeginChild("target_bar", barSize)) {
                var drawList = ImGui.GetWindowDrawList();
                drawList.AddRectFilled(cursorPos, cursorPos + barSize, colors["background"]);
                
                drawList.AddRectFilledMultiColor(
                    cursorPos, cursorPos + new Vector2((float)ToTBarWidth * actor.CurrentHp / actor.MaxHp, ToTBarHeight), 
                    colors["gradientLeft"], colors["gradientRight"], colors["gradientRight"], colors["gradientLeft"]
                );
                
                drawList.AddRect(cursorPos, cursorPos + barSize, 0xFF000000);
                
                if (ImGui.IsItemClicked()) {
                    TargetManager.SetTarget(targetObject);
                }
            }

            ImGui.EndChild();
        }

        protected Dictionary<string, uint> DetermineTargetPlateColors(Character actor) {
            var colors = PluginConfiguration.NPCColorMap["neutral"];
            
            // Still need to figure out the "orange" state; aggroed but not yet attacked.
            switch (actor.ObjectKind) {
                case ObjectKind.Player:
                    colors = PluginConfiguration.JobColorMap[actor.ClassJob.Id];
                    break;

                case ObjectKind.BattleNpc when (actor.StatusFlags & StatusFlags.InCombat) == StatusFlags.InCombat:
                    colors = PluginConfiguration.NPCColorMap["hostile"];
                    break;

                case ObjectKind.BattleNpc:
                {
                    if (!IsHostileMemory((BattleNpc)actor)) {
                        colors = PluginConfiguration.NPCColorMap["friendly"];
                    }

                    break;
                }
            }

            return colors;
        }

        protected void DrawOutlinedText(string text, Vector2 pos) {
            DrawOutlinedText(text, pos, Vector4.One, new Vector4(0f, 0f, 0f, 1f));
        }
        
        protected void DrawOutlinedText(string text, Vector2 pos, Vector4 color, Vector4 outlineColor) {
            ImGui.SetCursorPos(new Vector2(pos.X - 1, pos.Y + 1));
            ImGui.TextColored(outlineColor, text);
                
            ImGui.SetCursorPos(new Vector2(pos.X, pos.Y+1));
            ImGui.TextColored(outlineColor, text);
                
            ImGui.SetCursorPos(new Vector2(pos.X+1, pos.Y+1));
            ImGui.TextColored(outlineColor, text);
                
            ImGui.SetCursorPos(new Vector2(pos.X-1, pos.Y));
            ImGui.TextColored(outlineColor, text);

            ImGui.SetCursorPos(new Vector2(pos.X+1, pos.Y));
            ImGui.TextColored(outlineColor, text);
                
            ImGui.SetCursorPos(new Vector2(pos.X-1, pos.Y-1));
            ImGui.TextColored(outlineColor, text);
                
            ImGui.SetCursorPos(new Vector2(pos.X, pos.Y-1));
            ImGui.TextColored(outlineColor, text);
                
            ImGui.SetCursorPos(new Vector2(pos.X+1, pos.Y-1));
            ImGui.TextColored(outlineColor, text);
                
            ImGui.SetCursorPos(new Vector2(pos.X, pos.Y));
            ImGui.TextColored(color, text);
        }
        
        public void Draw() {
            if (!ShouldBeVisible() || ClientState.LocalPlayer == null) {
                return;
            }

            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);
            
            var begin = ImGui.Begin(
                "DelvUI",
                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | 
                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBringToFrontOnFocus
            );

            if (!begin) {
                return;
            }

            Draw(true);
            
            ImGui.End();
        }
        
        protected abstract void Draw(bool _);

        protected virtual unsafe bool ShouldBeVisible() {

            if (PluginConfiguration.HideHud)
            {
                return false;
            }

            if (IsVisible)
            {
                return true;
            }

            var parameterWidget = (AtkUnitBase*) GameGui.GetAddonByName("_ParameterWidget", 1);
            var fadeMiddleWidget = (AtkUnitBase*) GameGui.GetAddonByName("FadeMiddle", 1);
            
            // Display HUD only if parameter widget is visible and we're not in a fade event
            return ClientState.LocalPlayer == null || parameterWidget == null || fadeMiddleWidget == null || !parameterWidget->IsVisible || fadeMiddleWidget->IsVisible;
        }
        
        unsafe bool IsHostileMemory(BattleNpc npc)
        {
            return (npc.BattleNpcKind == BattleNpcSubKind.Enemy || (int)npc.BattleNpcKind == 1) 
                   && *(byte*)(npc.Address + 0x1980) != 0 
                   && *(byte*)(npc.Address + 0x193C) != 1;
        }
    }
}