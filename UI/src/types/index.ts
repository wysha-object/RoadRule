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

export const enum RuleState {
  Applied = 0,
  PartiallyApplied = 1,
}

export interface RuleValue {
  state: RuleState
  rule: Rule
}

export interface CarFlagsRulesValue {
  emergency: RuleValue
}

export interface SizeClassRulesValue {
  small: RuleValue
  medium: RuleValue
  large: RuleValue
  undefined: RuleValue
}

export interface EnergyTypesRulesValue {
  fuel: RuleValue
  electricity: RuleValue
  fuelAndElectricity: RuleValue
  none: RuleValue
}

export interface LaneRulesValue {
  carFlagsRules: CarFlagsRulesValue
  sizeClassRules: SizeClassRulesValue
  energyTypesRules: EnergyTypesRulesValue
}

export interface Lane {
  laneIndex: number
  position: Position
  screenPoint: ScreenPoint
  laneRules: LaneRulesValue
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