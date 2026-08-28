import {
  CarFlagsRulesValue,
  EnergyTypesRulesValue,
  LaneRulesValue,
  RuleState,
  RuleValue,
  SizeClassRulesValue,
} from 'types'

export function mergeRuleValues(a: RuleValue, b: RuleValue): RuleValue {
  if (
    a.rule === b.rule &&
    a.state === b.state &&
    a.state === RuleState.Applied
  ) {
    return { state: a.state, rule: a.rule }
  } else {
    const mergedRule = Math.max(a.rule, b.rule)
    return { state: RuleState.PartiallyApplied, rule: mergedRule }
  }
}

export function mergeCarFlagsRules(
  a: CarFlagsRulesValue,
  b: CarFlagsRulesValue,
): CarFlagsRulesValue {
  return { emergency: mergeRuleValues(a.emergency, b.emergency) }
}

export function mergeSizeClassRules(
  a: SizeClassRulesValue,
  b: SizeClassRulesValue,
): SizeClassRulesValue {
  return {
    small: mergeRuleValues(a.small, b.small),
    medium: mergeRuleValues(a.medium, b.medium),
    large: mergeRuleValues(a.large, b.large),
    undefined: mergeRuleValues(a.undefined, b.undefined),
  }
}

export function mergeEnergyTypesRules(
  a: EnergyTypesRulesValue,
  b: EnergyTypesRulesValue,
): EnergyTypesRulesValue {
  return {
    fuel: mergeRuleValues(a.fuel, b.fuel),
    electricity: mergeRuleValues(a.electricity, b.electricity),
    fuelAndElectricity: mergeRuleValues(
      a.fuelAndElectricity,
      b.fuelAndElectricity,
    ),
    none: mergeRuleValues(a.none, b.none),
  }
}

export function mergeLaneRules(
  a: LaneRulesValue,
  b: LaneRulesValue,
): LaneRulesValue {
  return {
    carFlagsRules: mergeCarFlagsRules(a.carFlagsRules, b.carFlagsRules),
    sizeClassRules: mergeSizeClassRules(a.sizeClassRules, b.sizeClassRules),
    energyTypesRules: mergeEnergyTypesRules(
      a.energyTypesRules,
      b.energyTypesRules,
    ),
  }
}
