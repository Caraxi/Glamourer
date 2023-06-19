﻿using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface;
using Glamourer.Interop;
using Glamourer.Interop.Structs;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using Penumbra.GameData.Actors;

namespace Glamourer.Gui.Tabs.ActorTab;

public class ActorSelector
{
    private readonly ObjectManager _objects;
    private readonly ActorService  _actors;
    private readonly TargetManager _targets;

    private ActorIdentifier _identifier = ActorIdentifier.Invalid;

    public ActorSelector(ObjectManager objects, TargetManager targets, ActorService actors)
    {
        _objects = objects;
        _targets = targets;
        _actors  = actors;
    }

    private LowerString _actorFilter = LowerString.Empty;
    private Vector2     _defaultItemSpacing;
    private float       _width;

    public (ActorIdentifier Identifier, ActorData Data) Selection
        => _objects.TryGetValue(_identifier, out var data) ? (_identifier, data) : (_identifier, ActorData.Invalid);

    public bool HasSelection
        => _identifier.IsValid;

    public void Draw(float width)
    {
        _width = width;
        using var group = ImRaii.Group();
        _defaultItemSpacing = ImGui.GetStyle().ItemSpacing;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        ImGui.SetNextItemWidth(_width);
        LowerString.InputWithHint("##actorFilter", "Filter...", ref _actorFilter, 64);

        DrawSelector();
        DrawSelectionButtons();
    }

    private void DrawSelector()
    {
        using var child = ImRaii.Child("##actorSelector", new Vector2(_width, -ImGui.GetFrameHeight()), true);
        if (!child)
            return;

        _objects.Update();
        using var style     = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, _defaultItemSpacing);
        var       skips     = ImGuiClip.GetNecessarySkips(ImGui.GetTextLineHeight());
        var       remainder = ImGuiClip.FilteredClippedDraw(_objects, skips, CheckFilter, DrawSelectable);
        ImGuiClip.DrawEndDummy(remainder, ImGui.GetTextLineHeight());
    }

    private bool CheckFilter(KeyValuePair<ActorIdentifier, ActorData> pair)
        => _actorFilter.IsEmpty || pair.Value.Label.Contains(_actorFilter.Lower, StringComparison.OrdinalIgnoreCase);

    private void DrawSelectable(KeyValuePair<ActorIdentifier, ActorData> pair)
    {
        var equals = pair.Key.Equals(_identifier);
        if (ImGui.Selectable(pair.Value.Label, equals) && !equals)
            _identifier = pair.Key.CreatePermanent();
    }

    private void DrawSelectionButtons()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        var buttonWidth = new Vector2(_width / 2, 0);

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.UserCircle.ToIconString(), buttonWidth
                , "Select the local player character.", !_objects.Player, true))
            _identifier = _objects.Player.GetIdentifier(_actors.AwaitedService);

        ImGui.SameLine();
        Actor targetActor = _targets.Target?.Address;
        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.HandPointer.ToIconString(), buttonWidth,
                "Select the current target, if it is in the list.", _objects.IsInGPose || !targetActor, true))
            _identifier = targetActor.GetIdentifier(_actors.AwaitedService);
    }
}
