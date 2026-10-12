import { useValue, bindValue, call } from 'cs2/api'
import {
  Lane,
  ToolState,
  Edge,
  LaneRulesValue,
  CarLaneValue,
  Composition,
  Camera,
} from 'types'

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
export async function updateLane(
  inputValue: { laneIndex: number } & (
    | {
        key: 'lane-rules'
        value: LaneRulesValue
      }
    | {
        key: 'car-lane'
        value: CarLaneValue
      }
  ),
): Promise<void> {
  return await call(
    'RoadRule',
    'UpdateLane',
    JSON.stringify({
      ...inputValue,
      value: JSON.stringify(inputValue.value),
    }),
  )
}
export async function lookAt(
  x: number,
  y: number,
  z: number,
  distance: number,
): Promise<void> {
  return await call('RoadRule', 'LookAt', JSON.stringify({ x, y, z, distance }))
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
