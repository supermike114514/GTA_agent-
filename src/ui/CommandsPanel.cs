using System;
using GTA;

namespace GTA5Buddy
{
    /// <summary>
    /// Orders the player can issue to the companion from the panel.
    ///
    /// These are discrete commands, not free text: SHVDN runs inside the game process and has
    /// no messaging channel to an outside model, so the panel is the instruction surface. Any
    /// order that cannot be carried out says why instead of failing silently.
    /// </summary>
    internal sealed class CommandsPanel
    {
        private readonly Companion _buddy;
        private readonly MenuPanel _menu = new MenuPanel(
            "队友指令",
            "上下选择 · 回车下达 · 退格返回 · M 关闭");

        internal CommandsPanel(Companion buddy)
        {
            _buddy = buddy;
        }

        internal MenuPanel Menu { get { return _menu; } }

        internal void Build()
        {
            _menu.Clear();

            _menu.Add("跟随我", delegate { _buddy.OrderFollow(); }, "FOLLOW");
            _menu.Add("原地待命", delegate { _buddy.OrderStay(); }, "STAY");
            _menu.Add("前往地图路径点", delegate { _buddy.OrderMoveToWaypoint(); }, "MOVE");

            _menu.Add("上我的车", delegate { _buddy.OrderEnterPlayerVehicle(); }, "CAR");
            _menu.Add("下车", delegate { _buddy.OrderLeaveVehicle(); }, "CAR");

            _menu.Add("攻击我瞄准的目标", delegate { _buddy.OrderAttackAimedTarget(); }, "FIGHT");
            _menu.Add("停止战斗并归队", delegate { _buddy.OrderStopCombat(); }, "FIGHT");

            _menu.Add("紧急归位（瞬移到身边）", delegate { _buddy.OrderTeleportHere(); }, "FIX");

            _menu.Add("召唤队友", delegate { _buddy.Spawn(); }, "SPAWN");
            _menu.Add("收回队友", delegate { _buddy.Despawn(); }, "SPAWN");

            _menu.Normalize();
        }

        /// <summary>Refreshes the hint line with the companion's live state.</summary>
        internal void Update()
        {
            try
            {
                if (!_buddy.Exists)
                {
                    _menu.Hint = "队友未在场 — 选「召唤队友」生成";
                    return;
                }

                Ped p = _buddy.Ped;
                string state = _buddy.State.ToString();
                _menu.Hint = "队友 " + state + " · HP " + p.Health + "/" + p.MaxHealth + " · 护甲 " + p.Armor;
            }
            catch (Exception) { }
        }
    }
}
