import { useValue, bindValue, call } from 'cs2/api'
import {
  Lane,
  ToolState,
  Edge,
  LaneRulesValue,
  CarLaneValue,
  Composition,
  Camera,
  Position,
} from 'types'

export enum UpdateLaneType {
  LaneRules = 0,
  CarLane = 1,
}
export type UpdateLaneInput = { laneIndex: number } & (
  | {
      key: UpdateLaneType.LaneRules
      value: LaneRulesValue
    }
  | {
      key: UpdateLaneType.CarLane
      value: CarLaneValue
    }
)

const GetToolState = bindValue<ToolState>('RoadRule', 'GetToolState')
const GetAllLaneGroup = bindValue<{ lane: Lane; defaultSpeedLimit: number }[]>(
  'RoadRule',
  'GetAllLaneGroup',
)
const GetAllLane = bindValue<{ lane: Lane; defaultSpeedLimit: number }[]>(
  'RoadRule',
  'GetAllLane',
)
export function useGetToolStateCmd(): ToolState {
  return useValue(GetToolState)
}
export function useGetAllLaneGroupCmd(): {
  lane: Lane
  defaultSpeedLimit: number
}[] {
  return useValue(GetAllLaneGroup)
}
export function useGetAllLaneCmd(): {
  lane: Lane
  defaultSpeedLimit: number
}[] {
  return useValue(GetAllLane)
}

const GetLanes = bindValue<
  Record<
    number,
    {
      masterLane: Lane
      lanes: Lane[]
    }
  >
>('RoadRule', 'GetLanes')
const GetSelectedLaneIndex = bindValue<number[]>(
  'RoadRule',
  'GetSelectedLaneIndex',
)
const GetSelectedEdge = bindValue<Edge[]>('RoadRule', 'GetSelectedEdge')
const GetComposition = bindValue<Composition>('RoadRule', 'GetComposition')
export function useGetLanesCmd(): Record<
  number,
  {
    masterLane: Lane
    lanes: Lane[]
  }
> {
  return useValue(GetLanes)
}
export function useGetSelectedLaneIndexCmd(): number[] {
  return useValue(GetSelectedLaneIndex)
}
export function useGetSelectedEdgeEntityCmd(): Edge[] {
  return useValue(GetSelectedEdge)
}
export function useGetCompositionCmd(): Composition {
  return useValue(GetComposition)
}

const GetCamera = bindValue<Camera | null>('RoadRule', 'GetCamera')
export function useGetCameraCmd(): Camera | null {
  return useValue(GetCamera)
}

export async function setToolStateCmd(inputValue: ToolState): Promise<void> {
  return await call('RoadRule', 'SetToolState', inputValue)
}
export async function updateLane(inputValue: UpdateLaneInput): Promise<void> {
  return await call('RoadRule', 'UpdateLane', inputValue)
}
export async function lookAt(
  position: Position,
  distance: number,
): Promise<void> {
  return await call('RoadRule', 'LookAt', { position, distance })
}
export async function addSelectedLaneIndex(laneIndex: number): Promise<void> {
  return await call('RoadRule', 'AddSelectedLaneIndex', laneIndex)
}
export async function removeSelectedLaneIndex(
  laneIndex: number,
): Promise<void> {
  return await call('RoadRule', 'RemoveSelectedLaneIndex', laneIndex)
}
export async function clearSelectedLaneIndex(): Promise<void> {
  return await call('RoadRule', 'ClearSelectedLaneIndex', '')
}
