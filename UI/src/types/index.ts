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

export enum RuleValue {
  None = 0,
  Prefer = 1,
  Forbidden = 2,
}

export interface RuleOptionsValue {
  noFlag: RuleValue
  hasFlag: RuleValue
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
  emergency: FieldValue<RuleOptionsValue>
}

export interface SizeClassRulesValue {
  small: FieldValue<RuleOptionsValue>
  medium: FieldValue<RuleOptionsValue>
  large: FieldValue<RuleOptionsValue>
  undefined: FieldValue<RuleOptionsValue>
}

export interface EnergyTypesRulesValue {
  fuel: FieldValue<RuleOptionsValue>
  electricity: FieldValue<RuleOptionsValue>
  fuelAndElectricity: FieldValue<RuleOptionsValue>
  none: FieldValue<RuleOptionsValue>
}

export interface VehicleTypeRulesValue {
  ambulance: FieldValue<RuleOptionsValue>
  deliveryTruck: FieldValue<RuleOptionsValue>
  fireEngine: FieldValue<RuleOptionsValue>
  garbageTruck: FieldValue<RuleOptionsValue>
  hearse: FieldValue<RuleOptionsValue>
  maintenanceVehicle: FieldValue<RuleOptionsValue>
  personalCar: FieldValue<RuleOptionsValue>
  policeCar: FieldValue<RuleOptionsValue>
  postVan: FieldValue<RuleOptionsValue>
  publicTransport: FieldValue<RuleOptionsValue>
  taxi: FieldValue<RuleOptionsValue>
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
