import {
  CarFlagsRulesValue,
  EnergyTypesRulesValue,
  LaneRulesValue,
  FieldState,
  FieldValue,
  SizeClassRulesValue,
  VehicleTypeRulesValue,
  Rule,
  CarLaneValue,
} from 'types'

export function mergeRuleValues(
  a: FieldValue<Rule>,
  b: FieldValue<Rule>,
): FieldValue<Rule> {
  if (
    a.value === b.value &&
    a.state === b.state &&
    a.state === FieldState.Applied
  ) {
    return Object.assign({}, a)
  } else {
    const mergedRule = Math.max(a.value, b.value)
    return { state: FieldState.PartiallyApplied, value: mergedRule }
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
    carFlagsRules: mergeCarFlagsRules(a.carFlagsRules, b.carFlagsRules),
    sizeClassRules: mergeSizeClassRules(a.sizeClassRules, b.sizeClassRules),
    energyTypesRules: mergeEnergyTypesRules(
      a.energyTypesRules,
      b.energyTypesRules,
    ),
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
    speedLimit: mergeSpeedLimitValues(a.speedLimit, b.speedLimit),
    defaultSpeedLimit: mergeSpeedLimitValues(
      a.defaultSpeedLimit,
      b.defaultSpeedLimit,
    ),
  }
}