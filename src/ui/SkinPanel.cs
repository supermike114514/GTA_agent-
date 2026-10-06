using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTA5Buddy
{
    /// <summary>
    /// Character skin browser.
    ///
    /// Like the vehicle catalogue this is runtime-enumerated - Ped.GetAllModels() - rather
    /// than a hardcoded list, so it covers exactly the ped models this install contains:
    /// ambient citizens, police, gangs, animals, story characters and DLC ped models alike.
    ///
    /// Grouping comes from Loc.ClassifyPed, which uses the reliable Model.* flags for the
    /// broad strokes (human/animal, male/female, gang) and infers the finer roles from the
    /// model name. Unknown models fall back to the citizen buckets rather than being misfiled.
    ///
    /// Spawned models are persistent and placed beside the player, so a skin can be inspected
    /// or used as a companion stand-in without it wandering off.
    /// </summary>
    internal sealed class SkinPanel
    {
        /// <summary>
        /// One catalogued ped model. Public so the LemonUI panel can enumerate the same
        /// database without duplicating the catalogue logic.
        /// </summary>
        internal sealed class Entry
        {
            internal PedHash Hash;
            internal string Label;
            internal string CategoryName;
            internal Loc.PedCategory Category;
            internal string Search;
            internal bool Spawnable;
        }

        private readonly List<Entry> _db = new List<Entry>();
        private bool _built;

        private readonly List<string> _categoryNames = new List<string>();
        private readonly List<Loc.PedCategory> _categoryValues = new List<Loc.PedCategory>();
        private int _categoryIndex;

        private string _query = string.Empty;

        private readonly MenuPanel _menu = new MenuPanel(
            "角色皮肤",
            "上下选择 · 回车生成 · ◀▶ 切换分类 · 退格删搜索字 · F5 关闭");

        // chunked catalogue build
        private PedHash[] _allModels;
        private int _buildCursor;
        private int _buildStartedAt;

        // pending model request
        private Model _pendingModel;
        private int _pendingSince;
        private int _visibleCount;

        internal MenuPanel Menu { get { return _menu; } }
        internal string Query { get { return _query; } }
        internal bool Built { get { return _built; } }

        /// <summary>Total ped models catalogued (0 until the catalogue finishes).</summary>
        internal int TotalCount { get { return _db.Count; } }

        /// <summary>Number of category buckets, including the leading "all" entry.</summary>
        internal int CategoryCount { get { return _categoryNames.Count; } }

        internal string CategoryLabel
        {
            get { return _categoryNames.Count == 0 ? "全部" : _categoryNames[_categoryIndex]; }
        }

        // ------------------------------------------------------------------
        // catalogue
        // ------------------------------------------------------------------

        internal void Build()
        {
            if (_built || _allModels != null) return;

            try
            {
                _allModels = Ped.GetAllModels();
                _buildCursor = 0;
                _buildStartedAt = Game.GameTime;
                BuddyLog.Info("skin catalogue: " + _allModels.Length + " ped model hashes to index");
            }
            catch (Exception ex)
            {
                _built = true;
                BuddyLog.Error("skin catalogue could not be enumerated", ex);
            }
        }

        /// <summary>
        /// Indexes models for at most the given budget, then returns. The caller repeats this
        /// every tick so several hundred model lookups never stall a single frame.
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
                        PedHash hash = _allModels[_buildCursor];
                        Model m = new Model(hash);

                        var entry = new Entry();
                        entry.Hash = hash;
                        entry.Spawnable = m.IsValid;
                        entry.Category = Loc.ClassifyPed(m, hash.ToString());
                        entry.CategoryName = Loc.PedCategoryName(entry.Category);
                        entry.Label = Loc.PedLabel(m.Hash, entry.Category);
                        entry.Search = (hash.ToString() + " " + entry.Category + " " +
                                        entry.CategoryName).ToLowerInvariant();

                        _db.Add(entry);
                    }
                    catch (Exception)
                    {
                        // one bad enum entry must not abort the catalogue
                    }

                    _buildCursor++;
                    if (Environment.TickCount - started >= budgetMs) return;
                }
            }
            finally
            {
                if (_buildCursor >= _allModels.Length)
                {
                    _db.Sort(delegate (Entry a, Entry b)
                    {
                        int c = a.Category.CompareTo(b.Category);
                        return c != 0 ? c : a.Hash.CompareTo(b.Hash);
                    });

                    BuildCategoryList();

                    _built = true;
                    _allModels = null;
                    BuddyLog.Info("skin catalogue built: " + _db.Count + " ped models in " +
                                  (Game.GameTime - _buildStartedAt) + " ms");
                    Rebuild();
                }
            }
        }

        private void BuildCategoryList()
        {
            _categoryNames.Clear();
            _categoryValues.Clear();
            _categoryNames.Add("全部");

            var seen = new List<Loc.PedCategory>();
            foreach (Entry e in _db)
            {
                if (!seen.Contains(e.Category)) seen.Add(e.Category);
            }

            var pairs = new List<KeyValuePair<string, Loc.PedCategory>>();
            foreach (Loc.PedCategory c in seen)
            {
                pairs.Add(new KeyValuePair<string, Loc.PedCategory>(Loc.PedCategoryName(c), c));
            }
            pairs.Sort(delegate (KeyValuePair<string, Loc.PedCategory> a, KeyValuePair<string, Loc.PedCategory> b)
            {
                return string.Compare(a.Key, b.Key, StringComparison.Ordinal);
            });

            foreach (KeyValuePair<string, Loc.PedCategory> p in pairs)
            {
                _categoryNames.Add(p.Key);
                _categoryValues.Add(p.Value);
            }

            BuddyLog.Info("skin categories: " + string.Join(", ", _categoryNames.ToArray()));
        }

        // ------------------------------------------------------------------
        // list
        // ------------------------------------------------------------------

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

        internal void NextCategory()
        {
            if (_categoryNames.Count == 0) return;
            _categoryIndex = (_categoryIndex + 1) % _categoryNames.Count;
            Rebuild();
        }

        internal void PreviousCategory()
        {
            if (_categoryNames.Count == 0) return;
            _categoryIndex = (_categoryIndex - 1 + _categoryNames.Count) % _categoryNames.Count;
            Rebuild();
        }

        // ------------------------------------------------------------------
        // data access for the LemonUI panel
        // ------------------------------------------------------------------

        /// <summary>The models matching the active category and search text, in display order.</summary>
        internal IEnumerable<Entry> VisibleEntries()
        {
            foreach (Entry e in _db)
            {
                if (_categoryIndex > 0 && e.Category != _categoryValues[_categoryIndex - 1]) continue;
                if (_query.Length > 0 && e.Search.IndexOf(_query, StringComparison.Ordinal) < 0) continue;
                yield return e;
            }
        }

        /// <summary>Requests a spawn of the given model (the same path the list uses).</summary>
        internal void SpawnByHash(PedHash hash)
        {
            foreach (Entry e in _db)
            {
                if (e.Hash != hash) continue;
                RequestSpawn(e);
                return;
            }
        }

        // Category filter access mirroring VehiclePanel, so the LemonUI panel can group the
        // model list and browse categories the same way the vehicle list does.
        // (CategoryCount / CategoryLabel already exist above.)

        internal string CategoryNameAt(int index)
        {
            if (index < 0 || index >= _categoryNames.Count) return string.Empty;
            return _categoryNames[index];
        }

        internal int CountInCategory(int index)
        {
            int n = 0;
            foreach (Entry e in _db)
            {
                if (index > 0 && e.Category != _categoryValues[index - 1]) continue;
                n++;
            }
            return n;
        }

        internal void SetCategory(int index)
        {
            if (index < 0 || index >= _categoryNames.Count) return;
            _categoryIndex = index;
            Rebuild();
        }

        /// <summary>Every catalogue entry, used when no category filter should apply.</summary>
        internal IEnumerable<Entry> AllEntries()
        {
            return _db;
        }

        internal void Rebuild()
        {
            if (!_built)
            {
                _menu.Clear();
                _menu.Add("正在建立皮肤清单... " + _db.Count + " 个已索引", null, string.Empty);
                _menu.Normalize();
                return;
            }

            int keep = _menu.SelectedIndex;
            _menu.Clear();

            _menu.Add("分类:  " + CategoryLabel, delegate { NextCategory(); }, "◀ ▶");

            int shown = 0;
            const int cap = 400;

            foreach (Entry e in _db)
            {
                if (_categoryIndex > 0 && e.Category != _categoryValues[_categoryIndex - 1]) continue;
                if (_query.Length > 0 && e.Search.IndexOf(_query, StringComparison.Ordinal) < 0) continue;

                Entry captured = e;
                _menu.Add(e.Label, delegate { RequestSpawn(captured); }, string.Empty);

                shown++;
                if (shown >= cap) break;
            }

            _visibleCount = shown;
            _menu.SetRow(0, shown == 0
                ? "分类:  " + CategoryLabel + "   (无匹配)"
                : "分类:  " + CategoryLabel + "   (" + shown + " 个)");

            if (shown == 0)
            {
                _menu.Add("(此分类/搜索下没有匹配皮肤)", null, string.Empty);
            }

            if (_query.Length > 0)
            {
                _menu.Add("<< 清空搜索 (" + _query + ") >>", delegate { ClearQuery(); }, string.Empty);
            }

            _menu.Select(keep);
            _menu.Normalize();
        }

        // ------------------------------------------------------------------
        // spawning
        // ------------------------------------------------------------------

        private void RequestSpawn(Entry entry)
        {
            try
            {
                Model m = new Model(entry.Hash);
                if (!m.IsValid || !m.IsInCdImage)
                {
                    Notify("~r~模型不可用：" + entry.Label);
                    return;
                }

                m.Request(1000);
                m.RequestCollision(1000);
                _pendingModel = m;
                _pendingSince = Game.GameTime;
                _menu.Hint = "正在加载皮肤 " + entry.Label + " ...";
            }
            catch (Exception ex)
            {
                BuddyLog.Error("skin spawn request failed", ex);
                Notify("~r~生成失败：" + ex.Message);
            }
        }

        internal void Update()
        {
            StepBuild(4);

            if (_pendingSince == 0) return;

            try
            {
                // ped models need their collision too, or the new ped drops through the world
                bool ready = _pendingModel.IsLoaded && _pendingModel.IsCollisionLoaded;

                if (ready || Game.GameTime - _pendingSince > 4000)
                {
                    if (ready) DoSpawn(_pendingModel);
                    else Notify("~r~模型加载超时，该皮肤可能在本安装中缺失");

                    _pendingSince = 0;
                }
            }
            catch (Exception ex)
            {
                _pendingSince = 0;
                BuddyLog.Error("skin spawn update failed", ex);
            }
        }

        private void DoSpawn(Model model)
        {
            try
            {
                Ped player = Game.Player.Character;
                if (player == null || !player.Exists()) return;

                // beside the player, facing them, on the ground
                Vector3 side = player.RightVector * 1.6f;
                Vector3 pos = player.Position + side;

                float groundZ;
                if (World.GetGroundHeight(pos, out groundZ))
                {
                    pos = new Vector3(pos.X, pos.Y, groundZ + 0.5f);
                }

                float heading = (player.Heading + 180f) % 360f;
                Ped ped = World.CreatePed(model, pos, heading);

                if (ped == null || !ped.Exists())
                {
                    Notify("~r~生成失败：世界拒绝了该模型");
                    return;
                }

                ped.IsPersistent = true;      // stands there for inspection instead of despawning
                Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, ped, true, true);
                Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, ped, Function.Call<int>(Hash.GET_PED_GROUP_INDEX, player));

                BuddyLog.Info("spawned skin model 0x" + model.Hash.ToString("X8") + " at " + pos.ToString());
                Notify("~g~已生成皮肤模型 0x" + model.Hash.ToString("X8"));
            }
            catch (Exception ex)
            {
                BuddyLog.Error("skin spawn failed", ex);
                Notify("~r~生成异常：" + ex.Message);
            }
        }

        private static void Notify(string message)
        {
            try { GTA.UI.Notification.PostTicker(message, false, true); }
            catch (Exception) { }
        }
    }
}
