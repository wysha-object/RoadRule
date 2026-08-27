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
            bool isPreferSizeClass = false;
            switch (sizeClass)
            {
                case SizeClass.Small:
                    isPreferSizeClass = IsPrefer(laneRules.m_SizeClassRules.m_Small, true);
                    break;
                case SizeClass.Medium:
                    isPreferSizeClass = IsPrefer(laneRules.m_SizeClassRules.m_Medium, true);
                    break;
                case SizeClass.Large:
                    isPreferSizeClass = IsPrefer(laneRules.m_SizeClassRules.m_Large, true);
                    break;
                case SizeClass.Undefined:
                    isPreferSizeClass = IsPrefer(laneRules.m_SizeClassRules.m_Undefined, true);
                    break;
            }

            bool isPreferEnergyTypes = false;
            switch (energyTypes)
            {
                case EnergyTypes.Fuel:
                    isPreferEnergyTypes = IsPrefer(laneRules.m_EnergyTypesRules.m_Fuel, true);
                    break;
                case EnergyTypes.Electricity:
                    isPreferEnergyTypes = IsPrefer(laneRules.m_EnergyTypesRules.m_Electricity, true);
                    break;
                case EnergyTypes.FuelAndElectricity:
                    isPreferEnergyTypes = IsPrefer(laneRules.m_EnergyTypesRules.m_FuelAndElectricity, true);
                    break;
                case EnergyTypes.None:
                    isPreferEnergyTypes = IsPrefer(laneRules.m_EnergyTypesRules.m_None, true);
                    break;
            }

            return isPreferCarFlags || isPreferSizeClass || isPreferEnergyTypes;
        }

        public static bool IsForbidden(LaneRules laneRules, CarFlags carFlags, SizeClass sizeClass, EnergyTypes energyTypes)
        {
            bool isForbiddenCarFlags = IsForbidden((int)CarFlags.Emergency, laneRules.m_CarFlagsRules.m_Emergency, (int)carFlags);
            bool isForbiddenSizeClass = false;
            switch (sizeClass)
            {
                case SizeClass.Small:
                    isForbiddenSizeClass = IsForbidden(laneRules.m_SizeClassRules.m_Small, true);
                    break;
                case SizeClass.Medium:
                    isForbiddenSizeClass = IsForbidden(laneRules.m_SizeClassRules.m_Medium, true);
                    break;
                case SizeClass.Large:
                    isForbiddenSizeClass = IsForbidden(laneRules.m_SizeClassRules.m_Large, true);
                    break;
                case SizeClass.Undefined:
                    isForbiddenSizeClass = IsForbidden(laneRules.m_SizeClassRules.m_Undefined, true);
                    break;
            }

            bool isForbiddenEnergyTypes = false;
            switch (energyTypes)
            {
                case EnergyTypes.Fuel:
                    isForbiddenEnergyTypes = IsForbidden(laneRules.m_EnergyTypesRules.m_Fuel, true);
                    break;
                case EnergyTypes.Electricity:
                    isForbiddenEnergyTypes = IsForbidden(laneRules.m_EnergyTypesRules.m_Electricity, true);
                    break;
                case EnergyTypes.FuelAndElectricity:
                    isForbiddenEnergyTypes = IsForbidden(laneRules.m_EnergyTypesRules.m_FuelAndElectricity, true);
                    break;
                case EnergyTypes.None:
                    isForbiddenEnergyTypes = IsForbidden(laneRules.m_EnergyTypesRules.m_None, true);
                    break;
            }

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
