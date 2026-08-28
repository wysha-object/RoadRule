import { useValue, bindValue, call } from 'cs2/api'
import { Entity } from 'cs2/utils'
import { Lane, ToolState, Edge, LaneRulesValue } from 'types'

export function useGetToolStateCmd(): ToolState {
  return JSON.parse(useValue(bindValue('RoadRule', 'GetToolState')))
}

export function useGetCameraCmd(): {
  position: { x: number; y: number; z: number }
  rotation: { x: number; y: number; z: number }
} {
  return JSON.parse(useValue(bindValue('RoadRule', 'GetCamera')))
}
export function useGetLanesCmd(): Record<
  number,
  {
    masterLane: Lane
    lanes: Lane[]
  }
> {
  return JSON.parse(useValue(bindValue('RoadRule', 'GetLanes')))
}
export function useGetSelectedLaneIndexCmd(): number[] {
  return JSON.parse(useValue(bindValue('RoadRule', 'GetSelectedLaneIndex')))
}
export function useGetSelectedEdgeEntityCmd(): Edge[] {
  return JSON.parse(useValue(bindValue('RoadRule', 'GetSelectedEdge')))
}

export async function setToolStateCmd(inputValue: ToolState): Promise<void> {
  return await call('RoadRule', 'SetToolState', inputValue)
}
export async function updateLane(
  inputValue: { laneIndex: number } & {
    key: 'lane-rules'
    value: LaneRulesValue
  },
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
