import {
  LaneRulesValue,
  FieldState,
  FieldValue,
  VehicleTypeRulesValue,
  CarLaneValue,
  RuleOptionsValue,
  RuleValue,
} from 'types'

export const MAX_DISPLAY_DISTANCE = 2000;

export function mergeRuleValues(
  a: RuleValue,
  b: RuleValue,
): RuleValue {
  return Math.max(a, b)
}

export function mergeRuleOptionsFieldValues(
  a: FieldValue<RuleOptionsValue>,
  b: FieldValue<RuleOptionsValue>,
): FieldValue<RuleOptionsValue> {
  if (
    a.value.noFlag === b.value.noFlag &&
    a.value.hasFlag === b.value.hasFlag &&
    a.state === b.state &&
    a.state === FieldState.Applied
  ) {
    return Object.assign({}, a)
  } else {
    const mergedRule = {
      noFlag: mergeRuleValues(a.value.noFlag, b.value.noFlag),
      hasFlag: mergeRuleValues(a.value.hasFlag, b.value.hasFlag),
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
  }
}

export function mergeLaneRules(
  a: LaneRulesValue,
  b: LaneRulesValue,
): LaneRulesValue {
  return {
    vehicleTypeRules: mergeVehicleRules(a.vehicleTypeRules, b.vehicleTypeRules),
  }
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