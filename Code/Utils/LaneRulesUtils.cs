using Game.Prefabs;
using Game.Vehicles;
using RoadRule.Components;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace RoadRule.Utils
{
    public static class LaneRulesUtils
    {
        public static bool IsPrefer(LaneRules laneRules, CarFlags carFlags, SizeClass sizeClass, EnergyTypes energyTypes)
        {
            if (IsForbidden(laneRules, carFlags, sizeClass, energyTypes))
            {
                return false;
            }

            bool isPreferCarFlags = IsPrefer((int)CarFlags.Emergency, laneRules.m_CarFlagsRules.m_Emergency, (int)carFlags);
            bool isPreferSizeClass =
                IsPrefer(laneRules.m_SizeClassRules.m_Small, sizeClass == SizeClass.Small)
                || IsPrefer(laneRules.m_SizeClassRules.m_Medium, sizeClass == SizeClass.Medium)
                || IsPrefer(laneRules.m_SizeClassRules.m_Large, sizeClass == SizeClass.Large)
                || IsPrefer(laneRules.m_SizeClassRules.m_Undefined, sizeClass == SizeClass.Undefined);

            bool isPreferEnergyTypes =
                IsPrefer(laneRules.m_EnergyTypesRules.m_Fuel, energyTypes == EnergyTypes.Fuel)
                || IsPrefer(laneRules.m_EnergyTypesRules.m_Electricity, energyTypes == EnergyTypes.Electricity)
                || IsPrefer(laneRules.m_EnergyTypesRules.m_FuelAndElectricity, energyTypes == EnergyTypes.FuelAndElectricity)
                || IsPrefer(laneRules.m_EnergyTypesRules.m_None, energyTypes == EnergyTypes.None);

            return isPreferCarFlags || isPreferSizeClass || isPreferEnergyTypes;
        }

        public static bool IsForbidden(LaneRules laneRules, CarFlags carFlags, SizeClass sizeClass, EnergyTypes energyTypes)
        {
            bool isForbiddenCarFlags = IsForbidden((int)CarFlags.Emergency, laneRules.m_CarFlagsRules.m_Emergency, (int)carFlags);
            bool isForbiddenSizeClass =
                IsForbidden(laneRules.m_SizeClassRules.m_Small, sizeClass == SizeClass.Small)
                || IsForbidden(laneRules.m_SizeClassRules.m_Medium, sizeClass == SizeClass.Medium)
                || IsForbidden(laneRules.m_SizeClassRules.m_Large, sizeClass == SizeClass.Large)
                || IsForbidden(laneRules.m_SizeClassRules.m_Undefined, sizeClass == SizeClass.Undefined);

            bool isForbiddenEnergyTypes =
                IsForbidden(laneRules.m_EnergyTypesRules.m_Fuel, energyTypes == EnergyTypes.Fuel)
                || IsForbidden(laneRules.m_EnergyTypesRules.m_Electricity, energyTypes == EnergyTypes.Electricity)
                || IsForbidden(laneRules.m_EnergyTypesRules.m_FuelAndElectricity, energyTypes == EnergyTypes.FuelAndElectricity)
                || IsForbidden(laneRules.m_EnergyTypesRules.m_None, energyTypes == EnergyTypes.None);

            return isForbiddenCarFlags || isForbiddenSizeClass || isForbiddenEnergyTypes;
        }

        public static bool IsPrefer(int flag, LaneRules.Rule rule, int flags)
        {
            var value = (flags & flag) != 0;
            return IsPrefer(rule, value);
        }

        public static bool IsPrefer(LaneRules.Rule rule, bool value)
        {
            if (IsForbidden(rule, value))
            {
                return false;
            }
            switch (rule)
            {
                case LaneRules.Rule.None:
                    return false;
                case LaneRules.Rule.PreferOrNone:
                    return value == false;
                case LaneRules.Rule.NoneOrPrefer:
                    return value == true;
                case LaneRules.Rule.ForbiddenOrNone:
                    return false;
                case LaneRules.Rule.NoneOrForbidden:
                    return false;
                case LaneRules.Rule.ForbiddenOrPrefer:
                    return value == true;
                case LaneRules.Rule.PreferOrForbidden:
                    return value == false;
                default:
                    return false;
            }
        }

        public static bool IsForbidden(int flag, LaneRules.Rule rule, int flags)
        {
            var value = (flags & flag) != 0;
            return IsForbidden(rule, value);
        }

        public static bool IsForbidden(LaneRules.Rule rule, bool value)
        {
            switch (rule)
            {
                case LaneRules.Rule.None:
                    return false;
                case LaneRules.Rule.PreferOrNone:
                    return false;
                case LaneRules.Rule.NoneOrPrefer:
                    return false;
                case LaneRules.Rule.ForbiddenOrNone:
                    return value == false;
                case LaneRules.Rule.NoneOrForbidden:
                    return value == true;
                case LaneRules.Rule.ForbiddenOrPrefer:
                    return value == false;
                case LaneRules.Rule.PreferOrForbidden:
                    return value == true;
                default:
                    return false;
            }
        }
    }
}
