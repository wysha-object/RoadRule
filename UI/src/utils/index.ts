import {
  LaneRulesValue,
  FieldState,
  FieldValue,
  VehicleTypeRulesValue,
  CarLaneValue,
  RuleOptionsValue,
  RuleValue,
} from 'types'

export function mergeRuleValues(
  a: RuleValue,
  b: RuleValue,
): RuleValue {
  return Math.max(a, b)
}

export function mergeBooleanFieldValues(
  a: FieldValue<boolean>,
  b: FieldValue<boolean>,
): FieldValue<boolean> {
  if (
    a.value === b.value &&
    a.state === b.state &&
    a.state === FieldState.Applied
  ) {
    return Object.assign({}, a)
  } else {
    const mergedValue = a.value || b.value
    return { state: FieldState.PartiallyApplied, value: mergedValue }
  }
}

export function mergeRuleOptionsFieldValues(
  a: FieldValue<RuleOptionsValue>,
  b: FieldValue<RuleOptionsValue>,
): FieldValue<RuleOptionsValue> {
  if (
    a.value.hasFlag === b.value.hasFlag &&
    a.value.fixedCost === b.value.fixedCost &&
    a.state === b.state &&
    a.state === FieldState.Applied
  ) {
    return Object.assign({}, a)
  } else {
    const mergedRule = {
      hasFlag: mergeRuleValues(a.value.hasFlag, b.value.hasFlag),
      fixedCost: Math.max(a.value.fixedCost, b.value.fixedCost),
    }
    return { state: FieldState.PartiallyApplied, value: mergedRule }
  }
}

export function mergeVehicleRules(
  a: VehicleTypeRulesValue,
  b: VehicleTypeRulesValue,
): VehicleTypeRulesValue {
  return {
    ambulance: mergeRuleOptionsFieldValues(a.ambulance, b.ambulance),
    deliveryTruck: mergeRuleOptionsFieldValues(a.deliveryTruck, b.deliveryTruck),
    fireEngine: mergeRuleOptionsFieldValues(a.fireEngine, b.fireEngine),
    garbageTruck: mergeRuleOptionsFieldValues(a.garbageTruck, b.garbageTruck),
    hearse: mergeRuleOptionsFieldValues(a.hearse, b.hearse),
    maintenanceVehicle: mergeRuleOptionsFieldValues(
      a.maintenanceVehicle,
      b.maintenanceVehicle,
    ),
    personalCar: mergeRuleOptionsFieldValues(a.personalCar, b.personalCar),
    policeCar: mergeRuleOptionsFieldValues(a.policeCar, b.policeCar),
    postVan: mergeRuleOptionsFieldValues(a.postVan, b.postVan),
    publicTransport: mergeRuleOptionsFieldValues(a.publicTransport, b.publicTransport),
    taxi: mergeRuleOptionsFieldValues(a.taxi, b.taxi),
    bicycle: mergeRuleOptionsFieldValues(a.bicycle, b.bicycle),
  }
}

export function mergeLaneRules(
  a: LaneRulesValue,
  b: LaneRulesValue,
): LaneRulesValue {
  return {
    vehicleTypeRules: mergeVehicleRules(a.vehicleTypeRules, b.vehicleTypeRules),
    keepInLane: mergeBooleanFieldValues(a.keepInLane, b.keepInLane),
  }
}

export function excludeLaneOnlyRules(value: LaneRulesValue): LaneRulesValue {
  return {
    ...value,
    keepInLane: { ...value.keepInLane, state: FieldState.PartiallyApplied },
  }
}

export function excludeMasterLaneOnlyRules(value: LaneRulesValue): LaneRulesValue {
  const vehicleTypeRules = { ...value.vehicleTypeRules }
  for (const key of Object.keys(vehicleTypeRules) as (keyof VehicleTypeRulesValue)[]) {
    const rule = vehicleTypeRules[key]
    if (rule.value.hasFlag === RuleValue.FixedCost) {
      vehicleTypeRules[key] = { ...rule, state: FieldState.PartiallyApplied }
    }
  }
  return { ...value, vehicleTypeRules }
}

export function mergeSpeedLimitValues(
  a: FieldValue<number>,
  b: FieldValue<number>,
): FieldValue<number> {
  if (
    a.value === b.value &&
    a.state === b.state &&
    a.state === FieldState.Applied
  ) {
    return Object.assign({}, a)
  } else {
    const mergedSpeedLimit = Math.min(a.value, b.value)
    return { state: FieldState.PartiallyApplied, value: mergedSpeedLimit }
  }
}

export function mergeCarLaneValues(
  a: CarLaneValue,
  b: CarLaneValue,
): CarLaneValue {
  return {
    speedLimit: mergeSpeedLimitValues(a.speedLimit, b.speedLimit)
  }
}