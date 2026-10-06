using System;
using System.Windows.Forms;
using GTA;
using GTA.Native;

namespace GTA5Buddy
{
    /// <summary>Which page of the panel is showing.</summary>
    internal enum MenuPage
    {
        Root,
        Spawner,
        Commands,
        Vehicles,
        VehicleCategory,
        Skins,
        Stats,
    }

    /// <summary>
    /// Owns the panel stack, routes input, and freezes the player while the panel is up.
    ///
    /// Input arrives through KeyDown events (edge-triggered) rather than per-frame Control
    /// reads: that avoids fighting the game's own bindings - notably the M interaction menu -
    /// and gives exact press edges for free. Character keys are routed to the vehicle page
    /// for its search filter and ignored everywhere else.
    /// </summary>
    internal sealed class MenuController
    {
        private readonly Companion _buddy;

        private readonly MenuPanel _root;
        private readonly MenuPanel _spawner;
        private readonly MenuPanel _vehicleCategory;
        private readonly CommandsPanel _commands;
        private readonly VehiclePanel _vehicles;
        private readonly SkinPanel _skins;
        private readonly StatsPanel _stats;

        private MenuPage _page = MenuPage.Root;
        private bool _built;

        internal MenuController(Companion buddy)
        {
            _buddy = buddy;

            _root = new MenuPanel("GTA5Buddy 控制台",
                "上下选择 · 回车进入 · 退格返回 · F5 关闭 · F6 诊断报告");
            _spawner = new MenuPanel("生成器",
                "上下选择 · 回车进入 · 退格返回 · F5 关闭");
            _vehicleCategory = new MenuPanel("载具分类",
                "上下选择 · 回车选定并返回 · 退格返回 · F5 关闭");
            _commands = new CommandsPanel(buddy);
            _vehicles = new VehiclePanel();
            _skins = new SkinPanel();
            _stats = new StatsPanel();

            // the vehicle page asks to open the category browser, which is a page change and
            // therefore belongs to the controller rather than the panel
            _vehicles.OnRequestCategoryPage = delegate { OpenVehicleCategories(); };
        }

        /// <summary>Opens the category browser page for the vehicle list.</summary>
        private void OpenVehicleCategories()
        {
            _vehicles.BuildCategoryMenu(_vehicleCategory);
            _vehicleCategory.ApplyStyle();
            _page = MenuPage.VehicleCategory;
        }

        internal bool IsOpen { get; private set; }
        internal MenuPage Page { get { return _page; } }

        /// <summary>Catalogue access for the diagnostic report and the LemonUI panel.</summary>
        internal VehiclePanel Vehicles { get { return _vehicles; } }
        internal SkinPanel Skins { get { return _skins; } }

        /// <summary>
        /// Called every tick by the main script. Kicks off both catalogues and advances them a
        /// little each frame.
        ///
        /// Build() only fetches the model-hash array; the per-model work happens in Update(),
        /// which is budgeted so the game does not hitch. Without this step the catalogues stay
        /// empty and the panel shows no categories at all.
        /// </summary>
        internal void WarmUpCatalogues()
        {
            try
            {
                _vehicles.Build();   // no-op once started
                _vehicles.Update();  // advances indexing within a per-frame budget

                _skins.Build();
                _skins.Update();
            }
            catch (Exception ex) { BuddyLog.Error("catalogue warm-up failed", ex); }
        }

        internal void Toggle()
        {
            if (!IsOpen && !BuddyIni.MenuEnabled)
            {
                // the panel is switched off in GTA5Buddy.ini
                return;
            }

            IsOpen = !IsOpen;
            if (IsOpen)
            {
                EnsureBuilt();
                _page = MenuPage.Root;
                BuildRoot();
                _root.ApplyStyle();
                BuddyLog.Info("menu opened");
            }
            else
            {
                BuddyLog.Info("menu closed");
            }
        }

        internal void Close()
        {
            IsOpen = false;
        }

        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            BuildRoot();
            BuildSpawner();
            _commands.Build();
            _stats.Build();
            _vehicles.Build();   // enumerates every vehicle model present in this install
            _skins.Build();      // enumerates every ped model present in this install
        }

        private void BuildRoot()
        {
            _root.Clear();
            _root.Add("队友指令", delegate { _page = MenuPage.Commands; _commands.Build(); }, "AGENT");
            _root.Add("生成器（载具 / 角色皮肤）", delegate { _page = MenuPage.Spawner; BuildSpawner(); }, "SPAWN");
            _root.Add("人物与武器", delegate
            {
                // capture the aim target the moment this page is entered: the panel freezes
                // player controls, so `TargetedEntity` cannot be re-read afterwards
                _stats.CaptureTarget();
                _stats.Build();
                _page = MenuPage.Stats;
            }, "STATS");
            _root.Normalize();
        }

        private void BuildSpawner()
        {
            _spawner.Clear();
            _spawner.Add("载具生成器（穷举全部载具）",
                delegate { _page = MenuPage.Vehicles; _vehicles.Rebuild(); }, "VEHICLE");
            _spawner.Add("角色皮肤（穷举全部人物模型）",
                delegate { _page = MenuPage.Skins; _skins.Rebuild(); }, "SKIN");
            _spawner.Normalize();
        }

        private MenuPanel Current
        {
            get
            {
                switch (_page)
                {
                    case MenuPage.Spawner: return _spawner;
                    case MenuPage.Commands: return _commands.Menu;
                    case MenuPage.Vehicles: return _vehicles.Menu;
                    case MenuPage.VehicleCategory: return _vehicleCategory;
                    case MenuPage.Skins: return _skins.Menu;
                    case MenuPage.Stats: return _stats.Menu;
                    default: return _root;
                }
            }
        }

        /// <summary>
        /// Advances background work that should run whether or not the panel is open: both
        /// model catalogues. Indexing them up front means the first F5 does not pay for
        /// several hundred model lookups.
        /// </summary>
        internal void WarmUp()
        {
            try
            {
                _vehicles.Build();   // no-op once started
                _vehicles.Update();  // advances the catalogue within a per-frame budget

                _skins.Build();
                _skins.Update();
            }
            catch (Exception ex) { BuddyLog.Error("menu warm-up failed", ex); }
        }

        /// <summary>Per-frame upkeep while the panel is open: freeze the player, refresh panes.</summary>
        internal void Update()
        {
            if (!IsOpen) return;

            EnsureBuilt();
            FreezePlayer();

            _commands.Update();
            _vehicles.Update();
            _skins.Update();

            if (_page == MenuPage.Stats)
            {
                _stats.Menu.Hint = "作用对象: " + _stats.TargetName + " · 回主菜单可重新选取";
            }
        }

        /// <summary>Handles one key press. Returns true when the menu consumed it.</summary>
        internal bool HandleKey(Keys key)
        {
            if (!IsOpen) return false;

            try
            {
                switch (key)
                {
                    case Keys.F5:
                    case Keys.Escape:
                        GoBack();
                        return true;

                    case Keys.Up:
                        Current.MoveSelection(-1);
                        return true;

                    case Keys.Down:
                        Current.MoveSelection(1);
                        return true;

                    case Keys.Left:
                        // on a list page the arrows also step the category filter; elsewhere
                        // they page through the rows
                        if (_page == MenuPage.Vehicles) _vehicles.PreviousCategory();
                        else if (_page == MenuPage.Skins) _skins.PreviousCategory();
                        else Current.MoveSelection(-12);
                        return true;

                    case Keys.Right:
                        if (_page == MenuPage.Vehicles) _vehicles.NextCategory();
                        else if (_page == MenuPage.Skins) _skins.NextCategory();
                        else Current.MoveSelection(12);
                        return true;

                    case Keys.Enter:
                        if (_page == MenuPage.VehicleCategory)
                        {
                            // the category rows already called SetCategory; return to the list
                            _page = MenuPage.Vehicles;
                            _vehicles.Rebuild();
                            return true;
                        }
                        Current.ActivateSelected();
                        if (_page == MenuPage.Vehicles) _vehicles.Rebuild();
                        if (_page == MenuPage.Skins) _skins.Rebuild();
                        return true;

                    case Keys.Back:
                        if (_page == MenuPage.Vehicles) _vehicles.AppendQuery('\b');
                        else if (_page == MenuPage.Skins) _skins.AppendQuery('\b');
                        else GoBack();
                        return true;
                }

                // text input matters on the two catalogue pages (search filter)
                bool typing = _page == MenuPage.Vehicles || _page == MenuPage.Skins;

                if (typing)
                {
                    char? c = null;

                    if (key >= Keys.A && key <= Keys.Z)
                        c = char.ToLowerInvariant((char)('a' + (key - Keys.A)));
                    else if (key >= Keys.D0 && key <= Keys.D9)
                        c = (char)('0' + (key - Keys.D0));
                    else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                        c = (char)('0' + (key - Keys.NumPad0));
                    else if (key == Keys.Space)
                        c = ' ';

                    if (c.HasValue)
                    {
                        if (_page == MenuPage.Vehicles) _vehicles.AppendQuery(c.Value);
                        else _skins.AppendQuery(c.Value);
                        return true;
                    }
                }
            }
            catch (Exception ex) { BuddyLog.Error("menu key handling failed", ex); }

            // while the panel is up it swallows everything, so stray keys cannot reach the game
            return true;
        }

        /// <summary>One level up the page hierarchy, closing the panel from the root.</summary>
        private void GoBack()
        {
            switch (_page)
            {
                case MenuPage.Root:
                    Close();
                    return;
                case MenuPage.VehicleCategory:
                    // leave the browser without changing the filter, back to the vehicle list
                    _page = MenuPage.Vehicles;
                    _vehicles.Rebuild();
                    return;
                case MenuPage.Vehicles:
                case MenuPage.Skins:
                    _page = MenuPage.Spawner;
                    BuildSpawner();
                    return;
                default:
                    _page = MenuPage.Root;
                    BuildRoot();
                    return;
            }
        }

        internal void Draw()
        {
            if (!IsOpen) return;

            try
            {
                if (_page == MenuPage.Vehicles)
                {
                    string q = _vehicles.Query;
                    _vehicles.Menu.TitleOverride = string.IsNullOrEmpty(q)
                        ? "载具生成器 · 共 " + _vehicles.Menu.Count + " 行"
                        : "载具生成器 · 搜索: " + q;
                }
                else if (_page == MenuPage.VehicleCategory)
                {
                    _vehicleCategory.TitleOverride = "载具分类 · " + _vehicleCategory.Count + " 类可选";
                }
                else if (_page == MenuPage.Skins)
                {
                    string q = _skins.Query;
                    _skins.Menu.TitleOverride = string.IsNullOrEmpty(q)
                        ? "角色皮肤 · 共 " + _skins.Menu.Count + " 行"
                        : "角色皮肤 · 搜索: " + q;
                }

                Current.Draw();
            }
            catch (Exception ex) { BuddyLog.Error("menu draw failed", ex); }
        }

        /// <summary>
        /// Freezes player control while the panel is open. DISABLE_ALL_CONTROL_ACTIONS is the
        /// blunt instrument that works regardless of SHVDN's per-control helpers.
        /// </summary>
        private static void FreezePlayer()
        {
            try { Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0); }
            catch (Exception) { }
        }
    }
}
