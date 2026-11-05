using ReplantedModUi;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppReloaded.Input;
using Il2CppReloaded.Services;
using Il2CppReloaded.DataModels;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Binders;
using Il2CppTekly.DataModels.Binders;
using Il2CppTekly.DataModels.Models;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PlantLibrary;

internal static class LibraryAlmanac
{
    internal static readonly List<ModCatalog> Catalogs = new();
    private static readonly Dictionary<(int Root, string Mod), View> Views = new();
    internal static bool GermanGameLanguage() => PlantLocalization.German;

    internal static void Attach(AlmanacPlantBinder binder)
    {
        var root = binder.transform;
        while (root && !root.name.StartsWith("P_Almanac_Plants", StringComparison.Ordinal)) root = root.parent;
        if (!root) return;
        foreach (var owner in Catalogs) {
            var key = (root.GetInstanceID(), owner.Id);
            if (Views.ContainsKey(key)) continue;
            View? view = null;
            try { view = new View(root, GermanGameLanguage(), owner); view.Build(); Views.Add(key, view); }
            catch (Exception error) { view?.Dispose(); LibraryMod.Instance.LoggerInstance.Error($"Almanac {owner.Id}: {error}"); }
        }
    }
    internal static void Update() {
        if (Views.Count == 0) return;
        bool german = GermanGameLanguage();
        foreach (var (key, view) in Views.ToArray()) {
            if (!view.Root) { view.Dispose(); Views.Remove(key); }
            else if (!view.Root.gameObject.activeInHierarchy) view.Close(false);
            else { view.RefreshAvailability(); view.RefreshLanguage(german); view.RefreshNativePortraits(); }
        }
    }
    internal static void Dispose() { foreach (var view in Views.Values.Reverse()) view.Dispose(); Views.Clear(); }
    internal static bool RightClick(Button button) {
        foreach (var view in Views.Values) {
            if (!view.Root || !view.Root.gameObject.activeInHierarchy || !view.IsNativeCard(button)) continue;
            try { if (view.OpenOutcomes(button)) return true; }
            catch (Exception error) { view.Close(true); LibraryMod.Instance.LoggerInstance.Error(error); return true; }
        }
        return false;
    }
    private sealed class View : IDisposable
    {
        private const int Columns = 8;
        private const int PageSize = 40;
        internal readonly Transform Root;
        private readonly List<GameObject> owned = new();
        private readonly List<(GameObject Object, bool Active)> hidden = new();
        private readonly Dictionary<int, (Sprite Sprite, Texture2D Texture)> portraits = new();
        private readonly List<(Button Button, Image Image, Image Background)> cards = new();
        private readonly ModCatalog owner;
        private Entry[] entries;
        private int availabilityRevision = -1;
        private readonly Dictionary<PlantIdentity, Entry> catalog;
        private Dictionary<PlantIdentity, Ingredient> IngredientOnly => owner.Ingredients;
        private PlantIdentity CatalogPlant(Entry entry) => entry.Plant;
        private Stream Resource(string file) => owner.Resource(file);
        private readonly Dictionary<SeedType, AlmanacEntryModel> nativePlants = new();
        private (PlantIdentity Plant, PlantIdentity[] Partners)[] outcomes = Array.Empty<(PlantIdentity, PlantIdentity[])>();
        private PlantIdentity? ingredient;
        private bool showingOutcomes;
        private Transform nativeGrid = null!;
        private GameObject staging = null!, page = null!;
        private Transform panel = null!, detail = null!;
        private Button tab = null!, outcomeTab = null!, previous = null!, next = null!;
        private TMP_Text title = null!, description = null!, count = null!, header = null!;
        private Image portrait = null!;
        private ScrollRect scroll = null!;
        private BasicNavigationLayer? navigation;
        private Selectable? originalTarget;
        private Navigation originalBackNavigation = null!, originalCloseNavigation = null!;
        private int pageIndex;
        private int selectedIndex;
        private bool german;

        internal View(Transform root, bool german, ModCatalog owner) {
            Root = root; this.german = german; this.owner = owner;
            catalog = owner.Entries.ToDictionary(CatalogPlant);
            entries = owner.Entries.Where(entry => DebugCatalog.CanView(owner, entry.Plant)).ToArray();
        }

        internal void RefreshAvailability()
        {
            if (availabilityRevision == DebugCatalog.Revision) return;
            availabilityRevision = DebugCatalog.Revision;
            entries = owner.Entries.Where(entry => DebugCatalog.CanView(owner, entry.Plant)).ToArray();
            if (showingOutcomes && ingredient is { } source && owner.Related != null)
                outcomes = owner.Related(source).Where(pair => DebugCatalog.CanView(owner, pair.Result)).GroupBy(pair => pair.Result)
                    .Select(group => (Plant: group.Key, Partners: group.Select(pair => pair.Partner).Distinct().ToArray()))
                    .OrderBy(pair => PlantName(pair.Plant), StringComparer.CurrentCultureIgnoreCase).ToArray();
            if (page != null && page.activeSelf) ShowPage(pageIndex);
        }

        private EntryText Text(Entry entry) => german ? entry.German : entry.English;

        private int DisplayCount => showingOutcomes ? outcomes.Length : entries!.Length;
        private Selectable FirstCard => DisplayCount > 0 ? cards[0].Button : next;

        internal bool IsNativeCard(Button button) => nativeGrid && button.transform.parent == nativeGrid
            && button.gameObject.activeInHierarchy && button.IsInteractable();

        private static AlmanacEntryModel? NativeModel(Button button)
        {
            var container = button.GetComponent<BinderContainer>();
            if (container == null || !container.m_hasBound) return null;
            // The non-generic interop overload is the private raw lookup. It
            // does not expand the wildcard used by binder paths. Resolve the
            // full key first, as native binder consumers do.
            string key = container.ResolveFullKey("*");
            return !string.IsNullOrEmpty(key) && container.TryGet(key, out IModel model)
                ? model?.TryCast<AlmanacEntryModel>() : null;
        }

        internal bool OpenOutcomes(Button button)
        {
            if (owner.Related == null) return false;
            var model = NativeModel(button);
            if (model?.Value == null || model.Imitater || model.Locked) return false;
            nativePlants.Clear();
            // Native cards are virtualized. The full entries model owns plants
            // outside the currently instantiated grid, including locked entries.
            var models = model.m_entriesModel.m_entriesModel.m_models;
            for (int i = 0; i < models.Count; ++i)
            {
                var item = models[i].Model.TryCast<AlmanacEntryModel>();
                if (item?.Value != null && !item.Imitater)
                    nativePlants[item.Value.SeedType] = item;
            }
            ingredient = new PlantIdentity(model.Value.SeedType);
            outcomes = owner.Related(ingredient.Value).Where(pair => DebugCatalog.CanView(owner, pair.Result)).GroupBy(pair => pair.Result)
                .Select(group => (Plant: group.Key, Partners: group.Select(pair => pair.Partner).Distinct().ToArray()))
                .OrderBy(pair => PlantName(pair.Plant), StringComparer.CurrentCultureIgnoreCase).ToArray();
            outcomeTab.gameObject.SetActive(true);
            Open(true);
            return true;
        }

        private string PlantName(PlantIdentity plant)
        {
            if (plant.Marker == SeedType.None && nativePlants.TryGetValue(plant.NativeType, out var native))
                return native.m_nameModel.OnToDisplayString();
            if (catalog.TryGetValue(plant, out var entry)) return Text(entry).Name;
            if (IngredientOnly.TryGetValue(plant, out var item)) return german ? item.German : item.English;
            throw new InvalidDataException($"Missing almanac plant: {plant}");
        }

        private Sprite? PlantPortrait(PlantIdentity plant)
        {
            if (plant.Marker == SeedType.None && nativePlants.TryGetValue(plant.NativeType, out var native))
                return native.m_thumbnailModel.Value;
            if (catalog.TryGetValue(plant, out var entry)) return Portrait(entry);
            var item = IngredientOnly[plant];
            return Portrait(item.Id, item.Portrait);
        }

        private static Transform Required(Transform parent, string path) => parent.Find(path)
            ?? throw new InvalidDataException($"Missing native almanac element: {path}");

        internal void RefreshNativePortraits()
        {
            if (!page || !page.activeSelf || !showingOutcomes) return;
            // Addressable models finish loading asynchronously. They own the
            // sprites; refresh our images without releasing native resources.
            int start = pageIndex * PageSize;
            for (int i = 0; i < cards.Count && start + i < outcomes.Length; ++i)
            {
                var plant = outcomes[start + i].Plant;
                if (plant.Marker == SeedType.None && nativePlants.ContainsKey(plant.NativeType))
                    SetCardPortrait(i, PlantPortrait(plant), true);
            }
            if (selectedIndex >= 0 && selectedIndex < outcomes.Length)
            {
                var plant = outcomes[selectedIndex].Plant;
                if (plant.Marker == SeedType.None && nativePlants.ContainsKey(plant.NativeType))
                    portrait.sprite = PlantPortrait(plant);
            }
        }

        private GameObject Own(GameObject obj) { owned.Add(obj); return obj; }

        // Clone below an inactive parent, then remove the native binding and
        // transition behaviour before the clone can receive OnEnable/Start.
        // Only Unity's presentation components survive on these owned copies.
        private GameObject Clone(GameObject source, Transform parent, string name)
        {
            return Own(NativeUi.Clone(source, staging.transform, parent, name));
        }

        private static RectTransform Rect(Transform transform) => transform.Cast<RectTransform>();

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private TMP_Text Label(GameObject template, Transform parent, string name, string text)
        {
            var clone = Clone(template, parent, name);
            var label = clone.GetComponent<TMP_Text>();
            label.text = text;
            label.raycastTarget = false;
            clone.SetActive(true);
            return label;
        }

        private Button Button(GameObject template, Transform parent, string name, string text, Action click)
        {
            var clone = Clone(template, parent, name);
            foreach (var fitter in clone.GetComponentsInChildren<ContentSizeFitter>(true)) fitter.enabled = false;
            var button = clone.GetComponent<Button>();
            button.enabled = true;
            button.interactable = true;
            button.transition = Selectable.Transition.ColorTint;
            if (button.targetGraphic != null) button.targetGraphic.raycastTarget = true;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener((UnityAction)(() =>
            {
                try { click(); }
                catch (Exception error)
                {
                    Close(true);
                    LibraryMod.Instance.LoggerInstance.Error($"Fusion almanac: {error}");
                }
            }));
            var textLabel = clone.GetComponentInChildren<TMP_Text>(true);
            if (textLabel != null)
            {
                textLabel.text = text;
                textLabel.raycastTarget = false;
            }
            clone.SetActive(true);
            return button;
        }

        private Button TextButton(GameObject template, Transform parent, string name, string text, Action click)
        {
            var button = Button(template, parent, name, text, click);
            // The native Layout has serialized width zero. Its fitter normally
            // measures localized text; these fixed-size buttons own that width.
            var layout = Required(button.transform, "Layout");
            foreach (var group in layout.GetComponents<LayoutGroup>()) group.enabled = false;
            Stretch(Rect(layout), Vector2.zero);
            var background = Required(layout, "Background").GetComponent<Image>();
            Stretch(background.rectTransform, Vector2.zero);
            background.enabled = true;
            background.gameObject.SetActive(true);
            background.raycastTarget = true;
            button.targetGraphic = background;
            var label = Required(layout, "ButtonText").GetComponent<TMP_Text>();
            Stretch(label.rectTransform, new Vector2(24, 0));
            label.enableAutoSizing = true;
            label.fontSizeMin = 40;
            label.fontSizeMax = 55;
            label.fontSize = 55;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.lineSpacing = 0;
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static void Stretch(RectTransform rect, Vector2 inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = inset;
            rect.offsetMax = -inset;
        }

        internal void Build()
        {
            panel = Required(Root, "Canvas/Layout/Center/Panel");
            var back = Required(panel, "P_BasicButton03-Back").gameObject;
            originalBackNavigation = back.GetComponent<Button>().navigation;
            originalCloseNavigation = Required(panel, "P_BasicButton03-Close").GetComponent<Button>().navigation;
            var nativeDetail = Required(panel, "SelectedItem");
            nativeGrid = Required(panel, "ItemGrid");
            var nativeCard = Required(nativeGrid, "P_Almanac_PlantItem").gameObject;
            var nameTemplate = Required(nativeDetail, "SelectedItemName").gameObject;
            navigation = Root.GetComponent<BasicNavigationLayer>();
            if (navigation != null) originalTarget = navigation.m_targetSelectable;

            staging = Own(new GameObject("FusionAlmanacStaging"));
            staging.SetActive(false);
            staging.transform.SetParent(panel, false);
            page = Own(new GameObject("FusionAlmanacPage"));
            page.SetActive(false);
            var pageRect = page.AddComponent<RectTransform>();
            pageRect.SetParent(panel, false);
            pageRect.anchorMin = Vector2.zero;
            pageRect.anchorMax = Vector2.one;
            pageRect.offsetMin = pageRect.offsetMax = Vector2.zero;

            tab = TextButton(back, panel, "FusionAlmanacTab", "Fusions", Toggle);
            var backRect = Rect(back.transform);
            Place(Rect(tab.transform), backRect.anchorMin,
                backRect.anchoredPosition + new Vector2(backRect.rect.width + 40 + 440 * Catalogs.IndexOf(owner), 0),
                new Vector2(420, backRect.sizeDelta.y), backRect.pivot);
            outcomeTab = TextButton(back, panel, "FusionOutcomesTab", "", () => Open(true));
            Place(Rect(outcomeTab.transform), backRect.anchorMin,
                backRect.anchoredPosition + new Vector2(backRect.rect.width + 40, 110),
                new Vector2(760, backRect.sizeDelta.y), backRect.pivot);
            outcomeTab.gameObject.SetActive(false);

            // Keep the native eight-column card sizes. Five rows leave space
            // for paging controls. Each result occurs once, regardless of how
            // many ingredient orders or alternative recipes produce it.
            var grid = Own(new GameObject("FusionAlmanacGrid")).AddComponent<RectTransform>();
            grid.SetParent(page.transform, false);
            var nativeRect = Rect(nativeGrid);
            Place(grid, nativeRect.anchorMin, nativeRect.anchoredPosition, nativeRect.sizeDelta, nativeRect.pivot);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            var nativeLayout = nativeGrid.GetComponent<GridLayoutGroup>();
            layout.cellSize = nativeLayout.cellSize;
            layout.spacing = nativeLayout.spacing;
            layout.padding = nativeLayout.padding;
            layout.childAlignment = nativeLayout.childAlignment;
            layout.startCorner = nativeLayout.startCorner;
            layout.startAxis = nativeLayout.startAxis;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Columns;
            for (int i = 0; i < PageSize; i++)
            {
                int slot = i;
                var button = Button(nativeCard, grid, $"FusionCard{i}", "", () => Select(pageIndex * PageSize + slot));
                var over = Required(button.transform, "OverState").GetComponent<Image>();
                over.gameObject.SetActive(true);
                button.targetGraphic = over;
                // The native ColorBlock fades this overlay, never the card art.
                over.CrossFadeColor(button.colors.normalColor * button.colors.colorMultiplier, 0, true, true);
                var image = Required(button.transform, "Background").GetComponent<Image>();
                image.color = Color.white;
                image.sprite = Portraits.PacketBackground;
                var icon = Own(new GameObject("Portrait")).AddComponent<RectTransform>();
                icon.SetParent(button.transform, false);
                // Keep header, paper and frame visible at native card dimensions.
                icon.anchorMin = new Vector2(.10f, .10f);
                icon.anchorMax = new Vector2(.90f, .84f);
                icon.offsetMin = icon.offsetMax = Vector2.zero;
                var iconImage = icon.gameObject.AddComponent<Image>();
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                over.transform.SetAsLastSibling();
                cards.Add((button, iconImage, image));
            }

            detail = Clone(nativeDetail.gameObject, page.transform, "FusionAlmanacDetail").transform;
            Required(detail, "SelectedItemRenderPortrait").gameObject.SetActive(false);
            portrait = Required(detail, "SelectedItemPortrait").GetComponent<Image>();
            portrait.preserveAspect = true;
            Place(portrait.rectTransform, new Vector2(.5f, 1), new Vector2(0, -75), new Vector2(620, 620), new Vector2(.5f, 1));
            portrait.gameObject.SetActive(true);
            title = Required(detail, "SelectedItemName").GetComponent<TMP_Text>();
            scroll = Required(detail, "Scroll View").GetComponent<ScrollRect>();
            scroll.enabled = true;
            scroll.horizontal = false;
            scroll.vertical = true;
            var gamepadScroll = scroll.GetComponent<GamepadScroll>();
            if (gamepadScroll != null) gamepadScroll.enabled = true;
            var infoBox = Required(scroll.transform, "Viewport/SelectedItemInfoBox");
            Required(infoBox, "InfoBox").gameObject.SetActive(false);
            description = Required(infoBox, "SelectedItemInfoLabel").GetComponent<TMP_Text>();
            description.enableAutoSizing = false;
            description.fontSize = description.fontSizeMax;
            description.lineSpacing = 0;
            description.alignment = TextAlignmentOptions.TopLeft;
            Place(description.rectTransform, new Vector2(0, 1), Vector2.zero, new Vector2(919, 1000), new Vector2(0, 1));
            var content = Rect(infoBox);
            Place(content, new Vector2(0, 1), Vector2.zero, new Vector2(919, 1000), new Vector2(0, 1));
            scroll.content = content;
            detail.gameObject.SetActive(true);

            previous = TextButton(back, page.transform, "PreviousFusions", "", () => ShowPage(pageIndex - 1));
            next = TextButton(back, page.transform, "NextFusions", "", () => ShowPage(pageIndex + 1));
            Place(Rect(previous.transform), Vector2.zero, new Vector2(550, 280), new Vector2(430, 94), Vector2.zero);
            Place(Rect(next.transform), Vector2.zero, new Vector2(1710, 280), new Vector2(430, 94), Vector2.zero);
            count = Label(nameTemplate, page.transform, "FusionPageCount", "");
            Place(count.rectTransform, Vector2.zero, new Vector2(1000, 280), new Vector2(690, 94), Vector2.zero);
            count.fontSizeMax = 52;
            header = Label(Required(panel, "HeaderText").gameObject, page.transform, "FusionHeader", "");
            RefreshChrome();

            // Native panel actions and its close/index buttons remain in use.
            // Only the presentation objects are hidden while this page is open.
            foreach (string path in new[] { "SlotsGrid", "ItemGrid", "SelectedItem", "Imitater", "P_ControllerPrompt_Scroll", "HeaderText" })
            {
                var obj = Required(panel, path).gameObject;
                hidden.Add((obj, obj.activeSelf));
            }
            LinkNativeButtons();
        }

        private void RefreshChrome()
        {
            tab.GetComponentInChildren<TMP_Text>(true).text = page.activeSelf
                ? german ? "Pflanzen" : "Plants" : owner.Name;
            if (ingredient.HasValue)
                outcomeTab.GetComponentInChildren<TMP_Text>(true).text = (german ? "Aus: " : "From: ") + PlantName(ingredient.Value);
            previous.GetComponentInChildren<TMP_Text>(true).text = german ? "Zurück" : "Previous";
            next.GetComponentInChildren<TMP_Text>(true).text = german ? "Weiter" : "Next";
            header.text = showingOutcomes && ingredient.HasValue
                ? (german ? "Fusionsergebnisse: " : "Fusion results: ") + PlantName(ingredient.Value)
                : owner.Name;
            int pages = Math.Max(1, (DisplayCount + PageSize - 1) / PageSize);
            count.text = $"{pageIndex + 1} / {pages}  ·  {DisplayCount} "
                + (showingOutcomes ? german ? "Ergebnisse" : "Results" : owner.Name);
        }

        internal void RefreshLanguage(bool useGerman)
        {
            if (german == useGerman) return;
            german = useGerman;
            RefreshChrome();
            if (!page.activeSelf) return;
            int start = pageIndex * PageSize;
            for (int i = 0; i < cards.Count && start + i < DisplayCount; i++)
                cards[i].Button.gameObject.name = showingOutcomes ? PlantName(outcomes[start + i].Plant) : Text(entries![start + i]).Name;
            float position = scroll.verticalNormalizedPosition;
            if (DisplayCount > 0) Select(Math.Min(selectedIndex, DisplayCount - 1));
            else ShowEmptyOutcomes();
            scroll.verticalNormalizedPosition = position;
        }

        private void LinkNativeButtons()
        {
            var back = Required(panel, "P_BasicButton03-Back").GetComponent<Button>();
            var close = Required(panel, "P_BasicButton03-Close").GetComponent<Button>();
            var views = Views.Values.Where(v => v.Root && v.Root.Pointer == Root.Pointer && v.tab != null)
                .Append(this).Distinct().OrderBy(v => Catalogs.IndexOf(v.owner)).ToArray();
            var links = views.Select(v => v.tab).ToList();
            var active = views.FirstOrDefault(v => v.page != null && v.page.activeSelf);
            if (active?.outcomeTab.gameObject.activeSelf == true) links.Add(active.outcomeTab);
            var target = active?.FirstCard ?? originalTarget;
            var nav = back.navigation; nav.mode = Navigation.Mode.Explicit;
            nav.selectOnRight = links[0]; nav.selectOnUp = target; back.navigation = nav;
            for (int i = 0; i < links.Count; i++) {
                nav = links[i].navigation; nav.mode = Navigation.Mode.Explicit;
                nav.selectOnLeft = i == 0 ? back : links[i - 1];
                nav.selectOnRight = i == links.Count - 1 ? close : links[i + 1];
                nav.selectOnUp = target; links[i].navigation = nav;
            }
            nav = close.navigation; nav.mode = Navigation.Mode.Explicit;
            nav.selectOnLeft = links[^1]; nav.selectOnUp = target; close.navigation = nav;
        }

        private void Toggle()
        {
            RefreshLanguage(GermanGameLanguage());
            if (page.activeSelf) { Close(true); return; }
            Open(false);
        }

        private void Open(bool showOutcomes)
        {
            foreach (var other in Views.Values) if (!ReferenceEquals(other, this) && other.Root.Pointer == Root.Pointer) other.Close(false);
            RefreshLanguage(GermanGameLanguage());
            if (!page.activeSelf)
            {
                for (int i = 0; i < hidden.Count; i++)
                {
                    var obj = hidden[i].Object;
                    hidden[i] = (obj, obj.activeSelf);
                    obj.SetActive(false);
                }
            }
            showingOutcomes = showOutcomes;
            outcomeTab.gameObject.SetActive(showOutcomes);
            page.SetActive(true);
            RefreshChrome();
            ShowPage(0);
        }

        internal void Close(bool focus)
        {
            if (page == null || !page || !page.activeSelf) return;
            page.SetActive(false);
            outcomeTab.gameObject.SetActive(false);
            foreach (var (obj, active) in hidden) if (obj) obj.SetActive(active);
            if (navigation != null && navigation) navigation.m_targetSelectable = originalTarget;
            if (tab != null && tab) RefreshChrome();
            if (Root && panel) LinkNativeButtons();
            foreach (var card in cards) {
                if (card.Image) card.Image.sprite = null;
                if (card.Background) card.Background.sprite = null;
            }
            if (portrait != null && portrait) portrait.sprite = null;
            ReleasePortraits();
            if (focus && Root && Root.gameObject.activeInHierarchy) Focus(tab);
        }

        private static void Focus(Selectable? target)
        {
            if (target == null || !target) return;
            // Replanted owns the active EventSystem and player selection.
            var manager = InputNavigationManager.Instance;
            if (manager != null) manager.RefreshSelection(target);
        }

        private void ShowPage(int index)
        {
            int last = Math.Max(0, (DisplayCount - 1) / PageSize);
            pageIndex = Math.Clamp(index, 0, last);
            int start = pageIndex * PageSize;
            int length = Math.Min(PageSize, DisplayCount - start);
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                card.Button.gameObject.SetActive(i < length);
                if (i >= length) continue;
                bool nativePacket = showingOutcomes && outcomes[start + i].Plant.Marker == SeedType.None
                    && nativePlants.ContainsKey(outcomes[start + i].Plant.NativeType);
                SetCardPortrait(i, showingOutcomes ? PlantPortrait(outcomes[start + i].Plant) : Portrait(entries![start + i]), nativePacket);
                card.Button.gameObject.name = showingOutcomes ? PlantName(outcomes[start + i].Plant) : Text(entries![start + i]).Name;
                var nav = card.Button.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnLeft = i % Columns == 0 ? tab : cards[i - 1].Button;
                nav.selectOnRight = i % Columns == Columns - 1 || i + 1 == length ? tab : cards[i + 1].Button;
                nav.selectOnUp = i < Columns ? tab : cards[i - Columns].Button;
                nav.selectOnDown = i + Columns < length ? cards[i + Columns].Button : next;
                card.Button.navigation = nav;
            }
            // Keep paging buttons selectable at boundaries, with clamped actions.
            // Controller focus can always return to the cards or the native index.
            foreach (var button in new[] { previous, next })
            {
                var nav = button.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = length > 0 ? FirstCard : tab;
                nav.selectOnDown = tab;
                nav.selectOnLeft = previous == button ? tab : previous;
                nav.selectOnRight = next == button ? tab : next;
                button.navigation = nav;
            }
            RefreshChrome();
            if (navigation != null && navigation) navigation.m_targetSelectable = FirstCard;
            LinkNativeButtons();
            if (length > 0) Select(start);
            else ShowEmptyOutcomes();
            var visible = showingOutcomes
                ? outcomes.Skip(start).Take(length).Where(pair => pair.Plant.Marker != SeedType.None
                    || !nativePlants.ContainsKey(pair.Plant.NativeType))
                    .Select(pair => catalog.TryGetValue(pair.Plant, out var entry)
                        ? entry.SourceId : IngredientOnly[pair.Plant].Id).ToHashSet()
                : entries!.Skip(start).Take(length).Select(entry => entry.SourceId).ToHashSet();
            ReleasePortraits(visible);
            Focus(FirstCard);
        }

        private void SetCardPortrait(int index, Sprite? sprite, bool nativePacket)
        {
            var card = cards[index];
            // Native thumbnails already include the complete packet. Imported
            // portraits use the library's native-derived frame exactly once.
            card.Background.sprite = nativePacket ? sprite : Portraits.PacketBackground;
            card.Image.sprite = nativePacket ? null : sprite;
            card.Image.enabled = !nativePacket && sprite != null;
        }

        private Sprite Portrait(Entry entry) => Portrait(entry.SourceId, entry.Portrait);

        private Sprite Portrait(int id, string file)
        {
            if (portraits.TryGetValue(id, out var cached)) return cached.Sprite;
            using var stream = Resource(file);
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, bytes.ToArray(), true))
                    throw new InvalidDataException($"Invalid almanac portrait: {file}");
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
                portraits.Add(id, (sprite, texture));
                return sprite;
            }
            catch { Object.Destroy(texture); throw; }
        }

        private void Select(int index)
        {
            selectedIndex = index;
            if (showingOutcomes)
            {
                var outcome = outcomes[index];
                title.text = PlantName(outcome.Plant);
                portrait.sprite = PlantPortrait(outcome.Plant);
                string recipes = string.Join("\n", outcome.Partners.Select(partner =>
                    $"{PlantName(ingredient!.Value)} + {PlantName(partner)} = {PlantName(outcome.Plant)}"));
                string info = catalog.TryGetValue(outcome.Plant, out var resultEntry)
                    ? Text(resultEntry).Info + "\n\n" : "";
                SetDescription($"<color=#28325a>{info}<b>{(german ? "Fusionen" : "Recipes")}</b>\n{recipes}</color>");
                return;
            }
            var entry = entries![index];
            var text = Text(entry);
            title.text = text.Name;
            portrait.sprite = Portrait(entry);
            string notes = string.IsNullOrWhiteSpace(text.Notes) ? "" : $"\n\n<b>Replanted</b>\n{text.Notes}";
            string recipesHeading = german ? "Rezepte" : "Recipes";
            string recipesText = text.Recipes.Length == 0 ? "" : $"\n\n<b>{recipesHeading}</b>\n{string.Join("\n", text.Recipes)}";
            string costs = owner.CostNotes(german);
            string costText = string.IsNullOrWhiteSpace(costs) ? "" : "\n\n" + costs;
            SetDescription($"<color=#28325a>{text.Info}</color>\n\n"
                + $"<color=#cc241d>{text.Flavor}{notes}{recipesText}{costText}</color>");
        }

        private void ShowEmptyOutcomes()
        {
            title.text = ingredient.HasValue ? PlantName(ingredient.Value) : "";
            portrait.sprite = null;
            SetDescription(german ? "Für diese Pflanze sind keine Fusionsergebnisse implementiert."
                : "No fusion results are implemented for this plant.");
        }

        private void SetDescription(string text)
        {
            description.text = text;
            float height = description.GetPreferredValues(description.text, 919, float.PositiveInfinity).y + 35;
            description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1;
        }

        private void ReleasePortraits(HashSet<int>? keep = null)
        {
            foreach (var (id, pair) in portraits.ToArray())
            {
                if (keep != null && keep.Contains(id)) continue;
                Object.Destroy(pair.Sprite);
                Object.Destroy(pair.Texture);
                portraits.Remove(id);
            }
        }

        public void Dispose()
        {
            Close(false);
            if (panel != null && panel)
            {
                Required(panel, "P_BasicButton03-Back").GetComponent<Button>().navigation = originalBackNavigation;
                Required(panel, "P_BasicButton03-Close").GetComponent<Button>().navigation = originalCloseNavigation;
            }
            foreach (var obj in owned.AsEnumerable().Reverse()) if (obj) Object.Destroy(obj);
            owned.Clear();
            ReleasePortraits();
        }
    }
}

[HarmonyPatch(typeof(AlmanacPlantBinder), nameof(AlmanacPlantBinder.Bind))]
internal static class LibraryAlmanacBindPatch
{
    private static void Postfix(AlmanacPlantBinder __instance) => LibraryAlmanac.Attach(__instance);
}

[HarmonyPatch(typeof(Button), nameof(Button.OnPointerClick))]
internal static class LibraryAlmanacRightClickPatch
{
    // Handle the native card before other mods suppress right-button clicks.
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(Button __instance, PointerEventData eventData) =>
        eventData.button != PointerEventData.InputButton.Right || !LibraryAlmanac.RightClick(__instance);
}
