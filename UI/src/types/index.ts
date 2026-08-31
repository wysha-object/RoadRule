import { Entity } from 'cs2/utils'

export enum UIToolMode {
  MasterLane,
  Lane,
}

export const enum ToolState {
  Disabled = 0,
  Choosing = 1,
  Choosed = 2,
}

export enum Rule {
  None = 0,

  PreferOrNone = 16,

  NoneOrPrefer = 32,

  ForbiddenOrNone = 48,

  NoneOrForbidden = 64,

  ForbiddenOrPrefer = 80,

  PreferOrForbidden = 96,
}

export const enum FieldState {
  Applied = 0,
  PartiallyApplied = 1,
}

export interface FieldValue<T> {
  state: FieldState
  value: T
}

export interface CarFlagsRulesValue {
  emergency: FieldValue<Rule>
}

export interface SizeClassRulesValue {
  small: FieldValue<Rule>
  medium: FieldValue<Rule>
  large: FieldValue<Rule>
  undefined: FieldValue<Rule>
}

export interface EnergyTypesRulesValue {
  fuel: FieldValue<Rule>
  electricity: FieldValue<Rule>
  fuelAndElectricity: FieldValue<Rule>
  none: FieldValue<Rule>
}

export interface VehicleTypeRulesValue {
  ambulance: FieldValue<Rule>
  deliveryTruck: FieldValue<Rule>
  fireEngine: FieldValue<Rule>
  garbageTruck: FieldValue<Rule>
  hearse: FieldValue<Rule>
  maintenanceVehicle: FieldValue<Rule>
  personalCar: FieldValue<Rule>
  policeCar: FieldValue<Rule>
  postVan: FieldValue<Rule>
  publicTransport: FieldValue<Rule>
  taxi: FieldValue<Rule>
}

export interface LaneRulesValue {
  carFlagsRules: CarFlagsRulesValue
  sizeClassRules: SizeClassRulesValue
  energyTypesRules: EnergyTypesRulesValue
  vehicleTypeRules: VehicleTypeRulesValue
}

export interface CarLaneValue {
  speedLimit: FieldValue<number>
  defaultSpeedLimit: FieldValue<number>
}

export interface Lane {
  laneIndex: number
  position: Position
  screenPoint: ScreenPoint
  laneRules: LaneRulesValue
  carLane: CarLaneValue
}

export interface Edge {
  edgeEntity: Entity
  position: Position
  screenPoint: ScreenPoint
}

export interface Position {
  x: number
  y: number
  z: number
}

export interface ScreenPoint {
  top: number
  left: number
}
