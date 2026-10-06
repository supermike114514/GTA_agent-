using System;
using System.Globalization;
using GTA;

namespace GTA5Buddy
{
    /// <summary>
    /// Chinese localisation for everything the panel renders itself.
    ///
    /// Vehicle and weapon *display names* already come back localised from the game
    /// (Vehicle.GetModelDisplayName wraps GET_DISPLAY_NAME_FROM_VEHICLE_MODEL, and the weapon
    /// collection uses its own localised labels), so only the things GTA exposes as plain
    /// enums or raw model hashes need translating here:
    ///
    ///   * VehicleClass      - a 24-value enum, always English
    ///   * ped models        - GET_MODEL_NAME_FOR_DEBUG and GET_LABEL_TEXT are both absent from
    ///                         SHVDN v3, so ped names are derived from the model hash plus the
    ///                         Model.* classification flags
    ///
    /// Everything is a hand-written table rather than a computed label, because these strings
    /// are the panel's whole vocabulary and are worth getting exactly right.
    /// </summary>
    internal static class Loc
    {
        // ------------------------------------------------------------------
        // vehicle classes -> Chinese
        // ------------------------------------------------------------------

        internal static string VehicleClass(VehicleClass cls)
        {
            switch (cls)
            {
                case GTA.VehicleClass.Compacts: return "小型车";
                case GTA.VehicleClass.Sedans: return "轿车";
                case GTA.VehicleClass.SUVs: return "SUV";
                case GTA.VehicleClass.Coupes: return "双门跑车";
                case GTA.VehicleClass.Muscle: return "肌肉车";
                case GTA.VehicleClass.SportsClassics: return "经典跑车";
                case GTA.VehicleClass.Sports: return "跑车";
                case GTA.VehicleClass.Super: return "超级跑车";
                case GTA.VehicleClass.Motorcycles: return "摩托车";
                case GTA.VehicleClass.OffRoad: return "越野车";
                case GTA.VehicleClass.Industrial: return "工业车辆";
                case GTA.VehicleClass.Utility: return "工程车辆";
                case GTA.VehicleClass.Vans: return "厢式货车";
                case GTA.VehicleClass.Cycles: return "自行车";
                case GTA.VehicleClass.Boats: return "船只";
                case GTA.VehicleClass.Helicopters: return "直升机";
                case GTA.VehicleClass.Planes: return "固定翼飞机";
                case GTA.VehicleClass.Service: return "服务车辆";
                case GTA.VehicleClass.Emergency: return "紧急车辆";
                case GTA.VehicleClass.Military: return "军用车辆";
                case GTA.VehicleClass.Commercial: return "商用车辆";
                case GTA.VehicleClass.Trains: return "列车";
                case GTA.VehicleClass.OpenWheel: return "开轮赛车";
                default: return "其他";
            }
        }

        // ------------------------------------------------------------------
        // ped model categories
        // ------------------------------------------------------------------

        /// <summary>Broad bucket a ped model falls into, used to group the skin list.</summary>
        internal enum PedCategory
        {
            Animal,
            Police,
            Gang,
            Military,
            Service,
            Female,
            Male,
            Other,
        }

        internal static string PedCategoryName(PedCategory c)
        {
            switch (c)
            {
                case PedCategory.Animal: return "动物";
                case PedCategory.Police: return "警察/急救";
                case PedCategory.Gang: return "帮派";
                case PedCategory.Military: return "军警/保安";
                case PedCategory.Service: return "服务行业";
                case PedCategory.Female: return "女性市民";
                case PedCategory.Male: return "男性市民";
                default: return "其他";
            }
        }

        /// <summary>
        /// Classifies a ped model.
        ///
        /// The Model.* flags give reliable broad strokes (human/animal, male/female,
        /// gang). The finer role buckets are inferred from the model name, because GTA does
        /// not expose a ped "type" that distinguishes police from shopkeepers. An
        /// unrecognised model therefore lands in the male/female citizen bucket rather than
        /// being misfiled - a conservative default.
        /// </summary>
        internal static PedCategory ClassifyPed(Model model, string modelName)
        {
            try
            {
                if (model.IsAnimalPed) return PedCategory.Animal;
                if (!model.IsHumanPed) return PedCategory.Animal;

                string n = (modelName ?? string.Empty).ToLowerInvariant();

                if (Contains(n, "swat", "cop", "police", "sheriff", "ranger", "fib", "cia",
                             "marshal", "prison", "guard", "paramedic", "fireman", "fire",
                             "medic", "doctor", "nurse", "lifeguard"))
                    return PedCategory.Police;

                if (model.IsGangPed ||
                    Contains(n, "gang", "ballas", "families", "vagos", "lost", "mexican",
                             "aztecas", "triad", "korean", "biker", "mafia", "cartel"))
                    return PedCategory.Gang;

                if (Contains(n, "army", "marine", "military", "blackops", "security", "chemsec"))
                    return PedCategory.Military;

                if (Contains(n, "shop", "clerk", "chef", "cook", "barber", "hair",
                             "valet", "driver", "chauffeur", "pilot", "mechanic",
                             "janitor", "gardener", "waiter", "stripper", "tennis",
                             "golf", "yoga", "fitness", "trainer", "coach", "priest",
                             "realtor", "bank", "casino", "dealer", "vendor"))
                    return PedCategory.Service;

                // "s_m_..." / "a_m_..." style component peds are the ambient citizens
                return model.IsFemalePed ? PedCategory.Female : PedCategory.Male;
            }
            catch (Exception)
            {
                return PedCategory.Other;
            }
        }

        private static bool Contains(string haystack, params string[] needles)
        {
            if (string.IsNullOrEmpty(haystack)) return false;
            for (int i = 0; i < needles.Length; i++)
            {
                if (haystack.IndexOf(needles[i], StringComparison.Ordinal) >= 0) return true;
            }
            return false;
        }

        /// <summary>
        /// Human-readable label for a ped model. The game exposes no localised ped name
        /// through SHVDN (GET_MODEL_NAME_FOR_DEBUG and GET_LABEL_TEXT are both absent), so
        /// this shows the Chinese category plus the model hash - stable and searchable
        /// rather than inventing a misleading display name.
        /// </summary>
        internal static string PedLabel(int modelHash, PedCategory category)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "{0}  0x{1:X8}", PedCategoryName(category), modelHash);
        }
    }
}
