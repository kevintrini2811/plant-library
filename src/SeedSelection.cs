using ReplantedModUi;
using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.DataModels;
using Il2CppSource.DataModels;
using Il2CppTekly.DataModels.Models;
using Il2CppTekly.DataModels.Binders;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using SeedList = Il2CppSystem.Collections.Generic.List<Il2CppReloaded.Gameplay.ChosenSeed>;

namespace PlantLibrary;

// Native models, packets, input and bank allocation remain the presentation path.
internal static class SeedSelection {
    private sealed class Session {
        internal SeedChooserScreen Screen = null!;
        internal readonly Dictionary<IntPtr, PlantDefinition> Choices = new();
        internal readonly List<ChosenSeed> Seeds = new();
        internal readonly List<GameObject> Buttons = new();
        internal readonly HashSet<IntPtr> HiddenNative = new();
        internal int Page;
        internal int LastPage;
        internal IntPtr SeedListPointer;
    }
    private static readonly Dictionary<IntPtr, Session> sessions = new();
    private const int PageSize = 48;
    private static bool Owns(SeedChooserScreen? screen) => screen?.mBoard != null
        && Plants.ExternalChooser?.Invoke(screen.mBoard) != true;
    private static Session? Get(SeedChooserScreen? screen) => screen == null ? null : sessions.GetValueOrDefault(screen.Pointer);
    internal static void BeforeEntries(SeedChooserDataModel model, out SeedList? original) {
        original = null;
        var screen = model.m_seedChooserScreen;
        if (!Owns(screen)) return;
        if (sessions.TryGetValue(screen.Pointer, out var old) && old.SeedListPointer != screen.mChosenSeeds.Pointer) {
            foreach (var button in old.Buttons) if (button) Object.Destroy(button);
            sessions.Remove(screen.Pointer);
        }
        if (!sessions.TryGetValue(screen.Pointer, out var session)) {
            var available = Plants.All.Where(p => p.Identity.Marker != SeedType.None && Plants.IsUnlocked(p, screen.mBoard)).ToArray();
            if (available.Length == 0) return;
            session = new Session { Screen = screen };
            foreach (var plant in available) {
                var seed = new ChosenSeed { mSeedType = plant.Identity.NativeType, mImitaterType = SeedType.None,
                    mSeedState = ChosenSeedState.SeedPacketHidden, mSeedIndexInBank = -1 };
                screen.mChosenSeeds.Add(seed); session.Seeds.Add(seed); session.Choices.Add(seed.Pointer, plant);
            }
            session.SeedListPointer = screen.mChosenSeeds.Pointer;
            sessions.Add(screen.Pointer, session);
        }
        original = screen.mChosenSeeds;
        for (int i = 0; i < (int)SeedType.NumSeedsInChooser; i++) {
            var seed = original[i];
            if (session.HiddenNative.Contains(seed.Pointer) && seed.mSeedState == ChosenSeedState.SeedPacketHidden)
                seed.mSeedState = ChosenSeedState.SeedInChooser;
        }
        session.HiddenNative.Clear();
        var native = new SeedList();
        for (int i = 0; i < (int)SeedType.NumSeedsInChooser; i++) native.Add(original[i]);
        screen.mChosenSeeds = native;
        // Native UpdateEntries replaces numeric entries but does not clear this
        // list. Remove our previous shared page before native entries are added.
        model.m_entriesUnlockedModel.Clear();
    }
    internal static void AfterEntries(SeedChooserDataModel model, SeedList? original) {
        if (original == null) return;
        var screen = model.m_seedChooserScreen; screen.mChosenSeeds = original;
        var session = Get(screen)!;
        var visible = model.m_entriesUnlockedModel;
        bool disabled = visible.DisableModifiedTrigger;
        bool allDisabled = model.m_entriesModel.DisableModifiedTrigger;
        visible.DisableModifiedTrigger = model.m_entriesModel.DisableModifiedTrigger = true;
        try {
            // Fill unused first-page slots before introducing another page.
            var allNative = new Dictionary<IntPtr, IModel>();
            for (int i = 0; i < model.m_entriesModel.m_models.Count; i++) {
                var item = model.m_entriesModel.m_models[i].Model;
                var entry = item.TryCast<SeedChooserEntryModel>();
                if (entry != null) allNative[entry.m_chosenSeed.Pointer] = item;
            }
            var nativeEntries = new List<IModel>();
            for (int i = 0; i < visible.m_models.Count; i++) {
                var entry = visible.m_models[i].Model.TryCast<SeedChooserEntryModel>();
                if (entry != null) nativeEntries.Add(allNative[entry.m_chosenSeed.Pointer]);
            }
            // The all-model owns these canonical entries. Clear may dispose the
            // separate native unlocked entries, so never reuse those instances.
            int free = Math.Max(0, PageSize - nativeEntries.Count);
            if (session.Page > 0) {
                for (int i = 0; i < (int)SeedType.NumSeedsInChooser; i++) {
                    var seed = original[i];
                    if (seed.mSeedState != ChosenSeedState.SeedInChooser) continue;
                    session.HiddenNative.Add(seed.Pointer); seed.mSeedState = ChosenSeedState.SeedPacketHidden;
                }
            }
            visible.Clear();
            if (session.Page == 0)
                for (int i = 0; i < nativeEntries.Count; i++) visible.Add("native" + i, nativeEntries[i], ReferenceType.Shared);
            int start = session.Page == 0 ? 0 : free + (session.Page - 1) * PageSize;
            int length = session.Page == 0 ? free : PageSize;
            for (int i = 0; i < session.Seeds.Count; i++) {
                var seed = session.Seeds[i]; bool show = i >= start && i < start + length;
                if (seed.mSeedState is not (ChosenSeedState.SeedInBank or ChosenSeedState.SeedFlyingToBank))
                    seed.mSeedState = show ? ChosenSeedState.SeedInChooser : ChosenSeedState.SeedPacketHidden;
                if (!show) continue;
                int position = session.Page == 0 ? nativeEntries.Count + i : i - start;
                int x = seed.mX, y = seed.mY;
                screen.GetSeedPositionInChooser(position, ref x, ref y);
                seed.mX = seed.mStartX = seed.mEndX = x; seed.mY = seed.mStartY = seed.mEndY = y;
                var definition = model.m_dataService.GetPlantDefinition(seed.mSeedType);
                var entry = new SeedChooserEntryModel(definition, seed, screen, model, false, (int)SeedType.NumSeedsInChooser + i);
                Decorate(entry);
                model.m_entriesModel.Add((int)SeedType.NumSeedsInChooser + i, entry.Cast<IModel>());
                visible.Add("custom" + i, entry.Cast<IModel>(), ReferenceType.Shared);
            }
            session.LastPage = Math.Max(0, (session.Seeds.Count - free + PageSize - 1) / PageSize);
        } finally {
            visible.DisableModifiedTrigger = disabled; model.m_entriesModel.DisableModifiedTrigger = allDisabled;
            if (!allDisabled) model.m_entriesModel.m_modified.Emit(model.m_entriesModel);
            if (!disabled) visible.m_modified.Emit(visible);
        }
    }
    private static void BuildButtons(Session session, int lastPage) {
        if (lastPage == 0 || session.Buttons.Count == 2 && session.Buttons.All(button => button != null && button)) return;
        foreach (var button in session.Buttons) if (button) Object.Destroy(button);
        session.Buttons.Clear();
        Transform? anchor = null;
        foreach (var binding in Object.FindObjectsOfType<BinderContainer>()) {
            if (!binding.m_hasBound || !binding.gameObject.activeInHierarchy) continue;
            if (!binding.TryGet(binding.ResolveFullKey(), out IModel model)) continue;
            if (model?.TryCast<SeedChooserEntryModel>()?.m_seedChooserScreen?.Pointer == session.Screen.Pointer) {
                anchor ??= binding.transform.parent;
                if (model!.TryCast<SeedChooserEntryModel>()!.m_chosenSeed.mSeedType == SeedType.Imitater) {
                    anchor = binding.transform.parent; break;
                }
            }
        }
        var source = Resources.FindObjectsOfTypeAll<Il2Cpp.TimeCycler>().FirstOrDefault(c => c.GetComponent<Button>() != null
            && c.m_cycles != null && c.m_cycles.Length > 1);
        if (anchor == null || source == null) return;
        var staging = new GameObject("PlantLibraryPageStaging"); staging.SetActive(false);
        try {
            foreach (int direction in new[] { -1, 1 }) {
                var control = NativeUi.Clone(source.gameObject, staging.transform, anchor, "PlantLibraryPage" + direction);
                session.Buttons.Add(control);
                var button = control.GetComponent<Button>(); button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener((UnityAction)(() => {
                    if (session.Screen.mSeedsInFlight != 0) return;
                    session.Page = Math.Clamp(session.Page + direction, 0, lastPage); session.Screen.mDirty = true;
                }));
                foreach (var label in control.GetComponentsInChildren<Il2CppTMPro.TMP_Text>(true)) label.gameObject.SetActive(false);
                var icon = source.m_cycles.First(c => Math.Abs(c.TimeScale - 1.5f) < .01f).CurrentSpeedIndicator;
                foreach (var cycle in source.m_cycles)
                    if (cycle.CurrentSpeedIndicator != null)
                        foreach (var child in control.GetComponentsInChildren<Transform>(true))
                            if (child.name == cycle.CurrentSpeedIndicator.name) child.gameObject.SetActive(child.name == icon.name);
                var rect = control.GetComponent<RectTransform>();
                (control.GetComponent<LayoutElement>() ?? control.AddComponent<LayoutElement>()).ignoreLayout = true;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
                rect.anchoredPosition = new Vector2(direction * 55, 45); rect.sizeDelta = new Vector2(100, 80);
                var scale = rect.localScale; scale.x = direction * Math.Abs(scale.x); rect.localScale = scale;
                control.SetActive(true);
            }
        } finally { Object.Destroy(staging); }
    }
    internal static void Update() {
        foreach (var session in sessions.Values) {
            if (session.Screen.Visible) BuildButtons(session, session.LastPage);
            foreach (var button in session.Buttons) if (button) button.SetActive(session.Screen.Visible);
        }
    }
    internal static void Decorate(SeedChooserEntryModel model) {
        if (Get(model.m_seedChooserScreen)?.Choices.TryGetValue(model.m_chosenSeed.Pointer, out var plant) != true) return;
        model.SetLocked(false); model.m_nameModel.Value = model.m_toolTipModel.Value = plant!.Name;
        model.m_sunCostModel.Value = plant.Cost(model.m_seedChooserScreen.mBoard); model.m_isImitaterModel.Value = false;
        model.m_thumbnail.Reference = null; model.m_thumbnail.Value = Portraits.Get(plant);
    }
    internal static bool Choose(SeedChooserScreen screen, ChosenSeed seed, int player) {
        var session = Get(screen);
        if (session == null || !session.Choices.TryGetValue(seed.Pointer, out var plant)) return true;
        if (seed.mSeedState == ChosenSeedState.SeedInBank) return Remove(screen, seed.mSeedIndexInBank, player);
        if (!screen.Visible || screen.mSeedsInFlight != 0 || screen.mChooseState != SeedChooserState.ChooseNormal
            || seed.mSeedState != ChosenSeedState.SeedInChooser || screen.SeedNotAllowedToPick(plant.Identity.NativeType)
            || player < 0 || player >= screen.m_seedBankInfos.Count) return false;
        var bank = screen.m_seedBankInfos[player].mSeedBank;
        if (screen.m_seedBankInfos[player].mSeedsInBank >= bank.mNumPackets) return false;
        screen.AddChosenSeedToBank(seed, player);
        seed.mFlying = seed.mFlashing = false;
        bank.mSeedPackets[seed.mSeedIndexInBank].mImitaterType = plant.Identity.Marker;
        screen.mDirty = true; return false;
    }
    internal static bool Remove(SeedChooserScreen screen, int slot, int player) {
        var session = Get(screen); if (session == null) return true;
        if (!screen.Visible || screen.mSeedsInFlight != 0 || screen.mChooseState != SeedChooserState.ChooseNormal
            || player < 0 || player >= screen.m_seedBankInfos.Count) return false;
        var selected = new List<ChosenSeed>(); ChosenSeed? removed = null;
        for (int i = 0; i < screen.mChosenSeeds.Count; i++) {
            var seed = screen.mChosenSeeds[i];
            if (seed.mPlayerIndex != player || seed.mSeedState != ChosenSeedState.SeedInBank) continue;
            if (seed.mSeedIndexInBank == slot) removed = seed; else selected.Add(seed);
        }
        if (removed == null) return true;
        if (removed.mCrazyDavePicked) return false;
        removed.mSeedState = ChosenSeedState.SeedInChooser; removed.mSeedIndexInBank = -1;
        var info = screen.m_seedBankInfos[player]; info.mSeedsInBank = 0;
        for (int i = 0; i < info.mSeedBank.mNumPackets; i++) {
            var packet = info.mSeedBank.mSeedPackets[i]; packet.mPacketType = packet.mImitaterType = SeedType.None;
            packet.mActive = packet.mRefreshing = packet.mFlying = false;
        }
        foreach (var seed in selected.OrderBy(s => s.mSeedIndexInBank)) {
            screen.AddChosenSeedToBank(seed, player); seed.mFlying = false;
            if (session.Choices.TryGetValue(seed.Pointer, out var plant)) info.mSeedBank.mSeedPackets[seed.mSeedIndexInBank].mImitaterType = plant.Identity.Marker;
        }
        info.mSeedBank.mIsDirty = true;
        screen.mDirty = true; return false;
    }
    internal static PlantDefinition? Packet(SeedPacket packet) => Plants.Find(new PlantIdentity(packet.mPacketType, packet.mImitaterType));
    internal static void Bank(SeedBankEntryModel model) {
        if (Packet(model.Packet) is not {} plant) return;
        model.m_nameModel.Value = plant.Name; model.m_sunCostModel.Value = plant.Cost(model.Packet.mBoard);
        model.m_isImitaterModel.Value = false;
        model.m_canAffordModel.Value = model.Packet.mBoard.CanTakeSunMoney(plant.Cost(model.Packet.mBoard), 0);
        model.m_activeModel.Value = model.Packet.mActive;
        model.m_showDisabled.Value = !model.m_canAffordModel.Value || !model.Packet.mActive;
        model.m_thumbnail.Reference = null; model.m_thumbnail.Value = Portraits.Get(plant);
    }
    internal static bool Place(Board board, int x, int y, int clicks, int player) {
        if (Plants.ExternalChooser?.Invoke(board) == true) return true;
        var cursor = board.CursorObjects[player];
        if (cursor.CursorType != CursorType.PlantFromBank || cursor.mSeedBankIndex < 0) return true;
        var packet = board.SeedBanks[player].mSeedPackets[cursor.mSeedBankIndex];
        if (Packet(packet) is not {} plant) return true;
        if (clicks < 0) { board.ClearCursor(true, player); return false; }
        if (board.mPaused || board.mApp.GameScene != GameScenes.Playing || !packet.mActive || packet.mRefreshing) return false;
        int col = board.PlantingPixelToGridX(x, y, plant.Identity.NativeType), row = board.PlantingPixelToGridY(x, y, plant.Identity.NativeType);
        if (col < 0 || col >= 9 || row < 0 || row >= board.GetNumRows() || !Plants.IsUnlocked(plant, board)) return false;
        int cost = plant.Cost(board);
        var reason = plant.CanPlant?.Invoke(board, col, row) ?? board.CanPlantAt(col, row, plant.Identity.NativeType);
        if (reason != PlantingReason.Ok || !board.CanTakeSunMoney(cost, player)) return false;
        var created = board.AddPlant(col, row, plant.Identity.NativeType, plant.Identity.Marker);
        if (created == null || created.mDead || !created.mIsOnBoard) return false;
        board.TakeSunMoney(cost, player); packet.WasPlanted(player); board.ClearCursor(true, player); return false;
    }
    internal static List<ChosenSeed>? Capture(SeedChooserScreen screen) {
        if (Get(screen) == null) return null;
        var result = new List<ChosenSeed>();
        for (int i = 0; i < screen.mChosenSeeds.Count; i++)
            if (screen.mChosenSeeds[i].mSeedState == ChosenSeedState.SeedInBank) result.Add(screen.mChosenSeeds[i]);
        return result;
    }
    internal static void FinalizeBank(SeedChooserScreen screen, List<ChosenSeed>? selected) {
        if (selected == null) return;
        var session = Get(screen)!;
        for (int player = 0; player < screen.m_seedBankInfos.Count; player++) {
            var info = screen.m_seedBankInfos[player]; info.mSeedsInBank = 0;
            for (int i = 0; i < info.mSeedBank.mNumPackets; i++) {
                var packet = info.mSeedBank.mSeedPackets[i];
                packet.mPacketType = packet.mImitaterType = SeedType.None;
                packet.mActive = packet.mRefreshing = packet.mFlying = false;
            }
            foreach (var seed in selected.Where(s => s.mPlayerIndex == player).OrderBy(s => s.mSeedIndexInBank)) {
                screen.AddChosenSeedToBank(seed, player); seed.mFlying = false;
                if (session.Choices.TryGetValue(seed.Pointer, out var plant))
                    info.mSeedBank.mSeedPackets[seed.mSeedIndexInBank].mImitaterType = plant.Identity.Marker;
            }
            info.mSeedBank.mIsDirty = true;
        }
    }
    internal static SeedList? NativeOnly(SeedChooserScreen? screen) {
        if (Get(screen) == null) return null;
        var original = screen!.mChosenSeeds; var native = new SeedList();
        for (int i = 0; i < (int)SeedType.NumSeedsInChooser; i++) native.Add(original[i]);
        screen.mChosenSeeds = native; return original;
    }
    internal static void Clear() {
        foreach (var session in sessions.Values)
            foreach (var button in session.Buttons) if (button) Object.Destroy(button);
        sessions.Clear();
    }
    internal static void Forget(Board board) {
        foreach (var (key, session) in sessions.ToArray()) {
            if (session.Screen.mBoard?.Pointer != board.Pointer) continue;
            foreach (var button in session.Buttons) if (button) Object.Destroy(button);
            sessions.Remove(key);
        }
    }
}

[HarmonyPatch(typeof(SeedChooserDataModel), nameof(SeedChooserDataModel.UpdateEntries))]
internal static class LibraryChooserEntriesPatch {
    private static void Prefix(SeedChooserDataModel __instance, out SeedList? __state) => SeedSelection.BeforeEntries(__instance, out __state);
    private static void Postfix(SeedChooserDataModel __instance, SeedList? __state) => SeedSelection.AfterEntries(__instance, __state);
    private static void Finalizer(SeedChooserDataModel __instance, SeedList? __state) { if (__state != null) __instance.m_seedChooserScreen.mChosenSeeds = __state; }
}
[HarmonyPatch(typeof(SeedChooserEntryModel), nameof(SeedChooserEntryModel.OnTick))]
internal static class LibraryChooserEntryPatch { private static void Postfix(SeedChooserEntryModel __instance) => SeedSelection.Decorate(__instance); }
[HarmonyPatch(typeof(SeedBankEntryModel), nameof(SeedBankEntryModel.OnTick))]
internal static class LibraryBankEntryPatch { private static void Postfix(SeedBankEntryModel __instance) => SeedSelection.Bank(__instance); }
[HarmonyPatch(typeof(ChosenSeed), nameof(ChosenSeed.Selected))]
internal static class LibraryChoosePatch { private static bool Prefix(ChosenSeed __instance, SeedChooserScreen seedChooserScreen, int playerIndex) => SeedSelection.Choose(seedChooserScreen, __instance, playerIndex); }
[HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.ClickedSeedInChooser))]
internal static class LibraryChooseScreenPatch { private static bool Prefix(SeedChooserScreen __instance, ChosenSeed theChosenSeed, int playerIndex) => SeedSelection.Choose(__instance, theChosenSeed, playerIndex); }
[HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.ClickedSeedInBank), new[] { typeof(SeedPacket), typeof(int) })]
internal static class LibraryRemovePacketPatch { private static bool Prefix(SeedChooserScreen __instance, SeedPacket thePacket, int playerIndex) => SeedSelection.Remove(__instance, thePacket.mIndex, playerIndex); }
[HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.ClickedSeedInBank), new[] { typeof(ChosenSeed), typeof(int) })]
internal static class LibraryRemoveChosenPatch { private static bool Prefix(SeedChooserScreen __instance, ChosenSeed seed, int playerIndex) => SeedSelection.Remove(__instance, seed.mSeedIndexInBank, playerIndex); }
[HarmonyPatch(typeof(Board), nameof(Board.MouseDownWithPlant))]
internal static class LibraryPlacePatch {
    [HarmonyPriority(Priority.First + 2000)]
    private static bool Prefix(Board __instance, int x, int y, int theClickCount, int playerIndex) => SeedSelection.Place(__instance, x, y, theClickCount, playerIndex);
}
[HarmonyPatch(typeof(SeedPacket), nameof(SeedPacket.GetCost))]
internal static class LibraryPacketCostPatch {
    private static bool Prefix(SeedPacket __instance, ref int __result) {
        if (SeedSelection.Packet(__instance) is not {} plant) return true;
        __result = plant.Cost(__instance.mBoard); return false;
    }
}

[HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.CloseSeedChooser))]
internal static class LibraryFinalizeBankPatch {
    private static void Prefix(SeedChooserScreen __instance, out List<ChosenSeed>? __state) => __state = SeedSelection.Capture(__instance);
    private static void Postfix(SeedChooserScreen __instance, List<ChosenSeed>? __state) => SeedSelection.FinalizeBank(__instance, __state);
}
[HarmonyPatch(typeof(SeedChooserDataModel), nameof(SeedChooserDataModel.UpdateImitaterEntries))]
internal static class LibraryImitaterBoundsPatch {
    private static void Prefix(SeedChooserDataModel __instance, out SeedList? __state) => __state = SeedSelection.NativeOnly(__instance.m_seedChooserScreen);
    private static void Finalizer(SeedChooserDataModel __instance, SeedList? __state) { if (__state != null) __instance.m_seedChooserScreen.mChosenSeeds = __state; }
}
