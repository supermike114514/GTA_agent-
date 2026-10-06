using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTA5Buddy
{
    /// <summary>
    /// Vehicle spawner with a runtime-enumerated database.
    ///
    /// The database is NOT a hardcoded list: it is built from Vehicle.GetAllModels(), so it
    /// always matches exactly what this installation actually contains - story-mode
    /// vehicles, DLC vehicles and the Online-only models that ship inside the Enhanced
    /// archives alike. Names come from Vehicle.GetModelDisplayName / GetModelMakeName.
    ///
    /// A model that exists in the vehicle enum can still be missing from the loaded content;
    /// those fail to load and are reported instead of silently doing nothing.
    /// </summary>
    internal sealed class VehiclePanel
    {
        /// <summary>
        /// One catalogued model. Public so the LemonUI panel can enumerate the same database
        /// without duplicating the catalogue logic.
        /// </summary>
        internal sealed class Entry
        {
            internal VehicleHash Hash;
            internal string Display;
            internal string Make;
            internal VehicleClass Class;
            internal string ClassLabel;   // Chinese, from Loc
            internal string Search;
            internal bool Spawnable;
        }

        private readonly List<Entry> _db = new List<Entry>();
        private bool _built;
        private string _query = string.Empty;

        // category filter: index into _categoryNames; 0 means "all"
        private readonly List<string> _categoryNames = new List<string>();
        private readonly List<VehicleClass> _categoryValues = new List<VehicleClass>();
        private int _categoryIndex;

        /// <summary>Set by MenuController: opens the category browser page.</summary>
        internal Action OnRequestCategoryPage;

        private readonly MenuPanel _menu = new MenuPanel(
            "载具生成器",
            "上下选择 · 回车生成 · 分类可进入分类页 · 退格删搜索字 · F5 关闭");

        // pending model request, so we never spawn a half-streamed model
        private Model _pendingModel;
        private int _pendingSince;

        internal MenuPanel Menu { get { return _menu; } }
        internal string Query { get { return _query; } }
        internal bool Built { get { return _built; } }

        /// <summary>Total models catalogued (0 until the catalogue finishes).</summary>
        internal int TotalCount { get { return _db.Count; } }

        /// <summary>Number of class buckets, including the leading "all" entry.</summary>
        internal int CategoryCount { get { return _categoryNames.Count; } }

        /// <summary>Chinese name of the active category filter.</summary>
        internal string CategoryLabel
        {
            get { return _categoryNames.Count == 0 ? "全部" : _categoryNames[_categoryIndex]; }
        }

        /// <summary>How many models the active category (plus search) currently matches.</summary>
        internal int CountInCategory()
        {
            int n = 0;
            foreach (Entry e in _db)
            {
                if (_categoryIndex > 0 && e.Class != _categoryValues[_categoryIndex - 1]) continue;
                if (_query.Length > 0 && e.Search.IndexOf(_query, StringComparison.Ordinal) < 0) continue;
                n++;
            }
            return n;
        }

        /// <summary>Fills a panel with one row per vehicle class, for the category browser.</summary>
        internal void BuildCategoryMenu(MenuPanel target)
        {
            target.Clear();

            for (int i = 0; i < _categoryNames.Count; i++)
            {
                int captured = i;

                int count = 0;
                foreach (Entry e in _db)
                {
                    if (captured == 0 || e.Class == _categoryValues[captured - 1]) count++;
                }

                if (count == 0) continue;   // never offer an empty bucket

                target.Add(_categoryNames[i], delegate { SetCategory(captured); }, count + " 辆");
            }

            target.Select(_categoryIndex);
            target.Normalize();
            target.TitleOverride = "载具分类 · 共 " + target.Count + " 类";
        }

        /// <summary>Applies a category by index and refreshes the list.</summary>
        internal void SetCategory(int index)
        {
            if (_categoryNames.Count == 0) return;
            if (index < 0 || index >= _categoryNames.Count) return;
            _categoryIndex = index;
            Rebuild();
        }

        // ------------------------------------------------------------------
        // data access for the LemonUI panel
        // ------------------------------------------------------------------

        /// <summary>Chinese name of the category at an index.</summary>
        internal string CategoryNameAt(int index)
        {
            if (index < 0 || index >= _categoryNames.Count) return string.Empty;
            return _categoryNames[index];
        }

        /// <summary>How many models the given category contains (ignoring the search text).</summary>
        internal int CountInCategory(int index)
        {
            int n = 0;
            foreach (Entry e in _db)
            {
                if (index > 0 && e.Class != _categoryValues[index - 1]) continue;
                n++;
            }
            return n;
        }

        /// <summary>
        /// The models matching the active category and search text, in display order. Used by
        /// the graphical panel so both UIs show the same set.
        /// </summary>
        internal IEnumerable<Entry> VisibleEntries()
        {
            foreach (Entry e in _db)
            {
                if (_categoryIndex > 0 && e.Class != _categoryValues[_categoryIndex - 1]) continue;
                if (_query.Length > 0 && e.Search.IndexOf(_query, StringComparison.Ordinal) < 0) continue;
                yield return e;
            }
        }

        /// <summary>Requests a spawn of the given model (the same path the list uses).</summary>
        internal void SpawnByHash(VehicleHash hash)
        {
            foreach (Entry e in _db)
            {
                if (e.Hash != hash) continue;
                RequestSpawn(e);
                return;
            }
        }

        // ------------------------------------------------------------------
        // database
        // ------------------------------------------------------------------

        private VehicleHash[] _allModels;
        private int _buildCursor;
        private int _buildStartedAt;

        /// <summary>How far the catalogue has progressed, for the progress hint.</summary>
        internal int IndexedSoFar { get { return _db.Count; } }

        /// <summary>
        /// Starts cataloguing the vehicle models straight from the game's own enum, so the
        /// list is exhaustive for this install rather than a hand-maintained subset.
        ///
        /// The work is spread across frames by <see cref="StepBuild"/>: several hundred model
        /// lookups in one tick would visibly stall the game.
        /// </summary>
        internal void Build()
        {
            if (_built || _allModels != null) return;

            try
            {
                _allModels = Vehicle.GetAllModels();
                _buildCursor = 0;
                _buildStartedAt = Game.GameTime;
                BuddyLog.Info("vehicle catalogue: " + _allModels.Length + " model hashes to index");
            }
            catch (Exception ex)
            {
                _built = true;
                BuddyLog.Error("vehicle catalogue could not be enumerated", ex);
            }
        }

        /// <summary>
        /// Catalogues models for at most <paramref name="budgetMs"/> milliseconds, then
        /// returns. Call every tick until <see cref="Built"/> is true.
        /// </summary>
        internal void StepBuild(int budgetMs)
        {
            if (_built || _allModels == null) return;

            int started = Environment.TickCount;

            try
            {
                while (_buildCursor < _allModels.Length)
                {
                    try
                    {
                        VehicleHash hash = _allModels[_buildCursor];
                        var entry = new Entry();
                        entry.Hash = hash;

                        Model m = new Model(hash);
                        entry.Spawnable = m.IsValid;

                        string display = Vehicle.GetModelDisplayName(m);
                        string make = Vehicle.GetModelMakeName(m);

                        // display name can be empty for a few stripped/debug models
                        entry.Display = string.IsNullOrEmpty(display) ? hash.ToString() : display;
                        entry.Make = make ?? string.Empty;

                        entry.Class = Vehicle.GetModelClass(m);
                        entry.ClassLabel = Loc.VehicleClass(entry.Class);

                        // the search index carries the Chinese class too, so typing 跑车 finds them
                        entry.Search = (entry.Display + " " + entry.Make + " " + hash + " " +
                                        entry.Class + " " + entry.ClassLabel).ToLowerInvariant();

                        _db.Add(entry);
                    }
                    catch (Exception)
                    {
                        // a single bad enum entry must not abort the whole catalogue
                    }

                    _buildCursor++;

                    if (Environment.TickCount - started >= budgetMs) return;
                }
            }
            finally
            {
                // If the frame budget was hit we will be called again; otherwise finish up.
                if (_buildCursor >= _allModels.Length)
                {
                    _db.Sort(delegate (Entry a, Entry b)
                    {
                        return string.Compare(a.Display, b.Display, StringComparison.OrdinalIgnoreCase);
                    });

                    BuildCategoryList();

                    _built = true;
                    _allModels = null;
                    BuddyLog.Info("vehicle catalogue built: " + _db.Count + " models in " +
                                  (Game.GameTime - _buildStartedAt) + " ms");
                    Rebuild();
                }
            }
        }

        internal void SetQuery(string q)
        {
            _query = (q ?? string.Empty).Trim().ToLowerInvariant();
            Rebuild();
        }

        internal void ClearQuery()
        {
            SetQuery(string.Empty);
        }

        internal void AppendQuery(char c)
        {
            if (c == '\b')
            {
                if (_query.Length > 0) SetQuery(_query.Substring(0, _query.Length - 1));
                return;
            }
            if (char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-')
            {
                SetQuery(_query + c);
            }
        }

        /// <summary>
        /// Rebuilds the visible rows from the database plus the current filter. Does nothing
        /// until the catalogue has finished indexing.
        /// </summary>
        internal void Rebuild()
        {
            if (!_built)
            {
                _menu.Clear();
                _menu.Add("正在建立载具清单... " + _db.Count + " 个已索引", null, string.Empty);
                _menu.Normalize();
                return;
            }

            int keep = _menu.SelectedIndex;
            _menu.Clear();

            // Row 0 opens the category browser: a real list of every class with its vehicle
            // count, instead of stepping through classes blind. The count is folded in once the
            // filtered loop below knows the real total.
            _menu.Add("分类:  " + CategoryLabel + "    > 进入分类页",
                      delegate { if (OnRequestCategoryPage != null) OnRequestCategoryPage(); },
                      "分类");

            int shown = 0;
            const int cap = 400;   // plenty to scroll, keeps the rebuild cheap

            foreach (Entry e in _db)
            {
                if (_categoryIndex > 0 && e.Class != _categoryValues[_categoryIndex - 1]) continue;
                if (_query.Length > 0 && e.Search.IndexOf(_query, StringComparison.Ordinal) < 0) continue;

                Entry captured = e;
                _menu.Add(
                    e.Display + (string.IsNullOrEmpty(e.Make) ? string.Empty : "  (" + e.Make + ")"),
                    delegate { RequestSpawn(captured); },
                    e.ClassLabel);

                shown++;
                if (shown >= cap) break;
            }

            _visibleCount = shown;
            _menu.SetRow(0, shown == 0
                ? "分类:  " + CategoryLabel + "   (无匹配)   > 进入分类页"
                : "分类:  " + CategoryLabel + "   (" + shown + " 辆)   > 进入分类页");

            if (shown == 0)
            {
                _menu.Add("(此分类/搜索下没有匹配载具)", null, string.Empty);
            }

            if (_query.Length > 0)
            {
                _menu.Add("<< 清空搜索 (" + _query + ") >>", delegate { ClearQuery(); }, string.Empty);
            }

            _menu.Select(keep);
            _menu.Normalize();
        }

        /// <summary>
        /// Builds the category list straight from the classes actually present in the
        /// database, so an empty class never appears as a dead option.
        /// </summary>
        private void BuildCategoryList()
        {
            _categoryNames.Clear();
            _categoryValues.Clear();
            _categoryNames.Add("全部");

            var seen = new List<VehicleClass>();
            foreach (Entry e in _db)
            {
                if (!seen.Contains(e.Class)) seen.Add(e.Class);
            }

            // group by Chinese label so the order reads naturally
            var pairs = new List<KeyValuePair<string, VehicleClass>>();
            foreach (VehicleClass c in seen)
            {
                pairs.Add(new KeyValuePair<string, VehicleClass>(Loc.VehicleClass(c), c));
            }
            pairs.Sort(delegate (KeyValuePair<string, VehicleClass> a, KeyValuePair<string, VehicleClass> b)
            {
                return string.Compare(a.Key, b.Key, StringComparison.Ordinal);
            });

            foreach (KeyValuePair<string, VehicleClass> p in pairs)
            {
                _categoryNames.Add(p.Key);
                _categoryValues.Add(p.Value);
            }

            BuddyLog.Info("vehicle categories: " + _categoryNames.Count + " buckets (" +
                          string.Join(", ", _categoryNames.ToArray()) + ")");
        }

        /// <summary>Steps the class filter and refreshes the list.</summary>
        internal void NextCategory()
        {
            if (_categoryNames.Count == 0) return;
            _categoryIndex = (_categoryIndex + 1) % _categoryNames.Count;
            Rebuild();
        }

        /// <summary>Steps the class filter backwards.</summary>
        internal void PreviousCategory()
        {
            if (_categoryNames.Count == 0) return;
            _categoryIndex = (_categoryIndex - 1 + _categoryNames.Count) % _categoryNames.Count;
            Rebuild();
        }

        private int _visibleCount;

        // ------------------------------------------------------------------
        // spawning
        // ------------------------------------------------------------------

        private void RequestSpawn(Entry entry)
        {
            try
            {
                Model m = new Model(entry.Hash);
                if (!m.IsValid)
                {
                    Notify("~r~模型无效：" + entry.Display + "（本安装中不存在或不可用）");
                    return;
                }

                if (!m.IsInCdImage)
                {
                    Notify("~r~模型不在内容档案中：" + entry.Display);
                    return;
                }

                m.Request(1000);
                _pendingModel = m;
                _pendingSince = Game.GameTime;

                _menu.Hint = "正在加载模型 " + entry.Display + " ...";
                Notify("~y~正在加载 " + entry.Display);
            }
            catch (Exception ex)
            {
                BuddyLog.Error("vehicle spawn request failed", ex);
                Notify("~r~生成失败：" + ex.Message);
            }
        }

        /// <summary>
        /// Drives the pending request and advances the catalogue. Call every tick.
        /// </summary>
        internal void Update()
        {
            // keep the catalogue moving even when the panel is closed, so opening the page
            // never stutters
            StepBuild(4);

            if (_pendingSince == 0) return;

            try
            {
                // stream for at most ~4 seconds; a model that never loads is reported, not waited on forever
                if (_pendingModel.IsLoaded || Game.GameTime - _pendingSince > 4000)
                {
                    bool loaded = _pendingModel.IsLoaded;
                    if (loaded) DoSpawn(_pendingModel);
                    else Notify("~r~模型加载超时，可能该载具在本安装中缺失");

                    _pendingSince = 0;
                }
            }
            catch (Exception ex)
            {
                _pendingSince = 0;
                BuddyLog.Error("vehicle spawn update failed", ex);
            }
        }

        private void DoSpawn(Model model)
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists()) return;

                // in front of the player, on the ground, heading the same way
                Vector3 pos = player.Position + (player.ForwardVector * 5f);
                float groundZ;
                if (World.GetGroundHeight(pos, out groundZ)) pos = new Vector3(pos.X, pos.Y, groundZ + 0.4f);

                Vehicle veh = World.CreateVehicle(model, pos, player.Heading);
                if (veh == null || !veh.Exists())
                {
                    Notify("~r~生成失败：世界拒绝了该载具");
                    return;
                }

                veh.IsPersistent = true;
                veh.PlaceOnGround();
                veh.IsEngineRunning = true;

                Function.Call(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, veh);

                player.Task.WarpIntoVehicle(veh, VehicleSeat.Driver);

                BuddyLog.Info("spawned vehicle " + model.Hash + " class=" +
                              Vehicle.GetModelClass(model) + " at " + pos.ToString());
                Notify("~g~已生成 " + Vehicle.GetModelDisplayName(model));
            }
            catch (Exception ex)
            {
                BuddyLog.Error("vehicle spawn failed", ex);
                Notify("~r~生成异常：" + ex.Message);
            }
        }

        // ------------------------------------------------------------------

        internal static void Notify(string message)
        {
            try { GTA.UI.Notification.PostTicker(message, false, true); }
            catch (Exception) { }
        }
    }
}
