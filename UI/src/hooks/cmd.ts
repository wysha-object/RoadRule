import { useValue, bindValue, call } from 'cs2/api'
import {
  LaneGroup,
  GetLaneResultItem,
  ToolState,
  Edge,
  UpdateLaneInput,
  LookAtInput,
  Composition,
  Camera,
  Position,
} from 'types'

export { UpdateLaneType } from 'types'
export type { UpdateLaneInput } from 'types'

const GetToolState = bindValue<ToolState>('RoadRule', 'GetToolState')
const GetAllLaneGroup = bindValue<GetLaneResultItem[]>(
  'RoadRule',
  'GetAllLaneGroup',
)
const GetAllLane = bindValue<GetLaneResultItem[]>('RoadRule', 'GetAllLane')
const GetLanes = bindValue<Record<number, LaneGroup>>('RoadRule', 'GetLanes')
const GetSelectedLaneIndex = bindValue<number[]>(
  'RoadRule',
  'GetSelectedLaneIndex',
)
const GetSelectedEdge = bindValue<Edge[]>('RoadRule', 'GetSelectedEdge')
const GetComposition = bindValue<Composition>('RoadRule', 'GetComposition')
const GetCamera = bindValue<Camera | null>('RoadRule', 'GetCamera')

export function useGetToolStateCmd(): ToolState {
  return useValue(GetToolState)
}
export function useGetAllLaneGroupCmd(): GetLaneResultItem[] {
  return useValue(GetAllLaneGroup)
}
export function useGetAllLaneCmd(): GetLaneResultItem[] {
  return useValue(GetAllLane)
}
export function useGetLanesCmd(): Record<number, LaneGroup> {
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
  const inputValue: LookAtInput = { position, distance }
  return await call('RoadRule', 'LookAt', inputValue)
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
