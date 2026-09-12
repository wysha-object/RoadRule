import {
  LaneRulesValue,
  FieldState,
  FieldValue,
  VehicleTypeRulesValue,
  CarLaneValue,
  RuleOptionsValue,
} from 'types'

export function mergeRuleValues(
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
      noFlag: Math.max(a.value.noFlag, b.value.noFlag),
      hasFlag: Math.max(a.value.hasFlag, b.value.hasFlag),
    }
    return { state: FieldState.PartiallyApplied, value: mergedRule }
  }
}

export function mergeVehicleRules(
  a: VehicleTypeRulesValue,
  b: VehicleTypeRulesValue,
): VehicleTypeRulesValue {
  return {
    ambulance: mergeRuleValues(a.ambulance, b.ambulance),
    deliveryTruck: mergeRuleValues(a.deliveryTruck, b.deliveryTruck),
    fireEngine: mergeRuleValues(a.fireEngine, b.fireEngine),
    garbageTruck: mergeRuleValues(a.garbageTruck, b.garbageTruck),
    hearse: mergeRuleValues(a.hearse, b.hearse),
    maintenanceVehicle: mergeRuleValues(
      a.maintenanceVehicle,
      b.maintenanceVehicle,
    ),
    personalCar: mergeRuleValues(a.personalCar, b.personalCar),
    policeCar: mergeRuleValues(a.policeCar, b.policeCar),
    postVan: mergeRuleValues(a.postVan, b.postVan),
    publicTransport: mergeRuleValues(a.publicTransport, b.publicTransport),
    taxi: mergeRuleValues(a.taxi, b.taxi),
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