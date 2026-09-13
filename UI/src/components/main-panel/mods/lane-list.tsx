import { UIToolModeContext } from 'context'
import { Button } from 'cs2/ui'
import {
  addSelectedLaneIndex,
  removeSelectedLaneIndex,
  useGetLanesCmd,
  useGetSelectedLaneIndexCmd,
} from 'hooks/cmd'
import { useContext, useMemo } from 'react'
import { UIToolMode, type Lane } from 'types'

export default function LaneList() {
  const masterMap = useGetLanesCmd()
  const [mode] = useContext(UIToolModeContext)

  const lanes = useMemo(
    () =>
      mode === UIToolMode.Lane
        ? Object.values(masterMap)
          .map((item) => item.lanes.length === 0 ? [item.masterLane] : item.lanes)
          .flat()
          .sort((a, b) => a.laneIndex - b.laneIndex)
        : Object.values(masterMap)
          .map((item) => item.masterLane)
          .sort((a, b) => a.laneIndex - b.laneIndex),
    [mode, JSON.stringify(masterMap)],
  )

  return (
    <div
      className='row'
      style={{
        width: 'var(--right-panel-width)',
      }}
    >
      {lanes.map((lane, index) => (
        <LaneItem lane={lane} index={index + 1} />
      ))}
    </div>
  )
}

function LaneItem(props: { lane: Lane, index: number }) {
  const selectedLaneIndex = useGetSelectedLaneIndexCmd()

  return (
    <div
      style={{
        margin: '0.1em 0.2em',
        flex: '1',
      }}
    >
      <Button
        variant='flat'
        style={{
          color: selectedLaneIndex.includes(props.lane.laneIndex)
            ? 'lightblue'
            : 'gray',
          borderColor: selectedLaneIndex.includes(props.lane.laneIndex)
            ? 'lightblue'
            : 'gray',
          borderWidth: '1rem',
          borderStyle: 'solid',
        }}
        onClick={() => {
          if (selectedLaneIndex.includes(props.lane.laneIndex)) {
            removeSelectedLaneIndex(props.lane.laneIndex)
          } else {
            addSelectedLaneIndex(props.lane.laneIndex)
          }
        }}
      >
        #{props.index}
      </Button>
    </div>
  )
}
